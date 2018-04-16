'use strict';
require('../../global');

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require(`${__libdir}/db`);
const logger = global.logger || require(`${__libdir}/logger`);

const stmt = {
    deleteLeagueTeams: db.prepare('DELETE FROM league_teams WHERE leagueId = ?'),
    insertLeagueTeam: db.prepare('INSERT OR IGNORE INTO league_teams (leagueId, teamId) VALUES (?, ?)'),
    insertTeam: db.prepare('INSERT OR IGNORE INTO teams (name, shortname, siteId, teamId) VALUES (?, ?, ?, ?)'),
    updateTeam: db.prepare('UPDATE teams SET name = ?, shortname = ? WHERE siteId = ? AND teamId = ?')
};

const children = [];
let transactionStarted = false;

db.serialize();

// TODO: Run query through watchers table
db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
    (err, row) => {
        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;

        if (err) {
            logger.error(err);
            return children.push(Promise.resolve());
        }

        const child = `${__libdir}/scripts/leagues/${siteId}/teams.js`;

        if (!fs.existsSync(child)) {
            return children.push(Promise.resolve());
        }

        if (!transactionStarted) {
            transactionStarted = true;
            db.run('BEGIN TRANSACTION');
            stmt.deleteLeagueTeams.run([leagueId]);
        }

        logger.log(`Retrieving teams for league ${leagueName}...`);

        children.push(new Promise(resolve => {
            let done = false;

            const finish = () => {
                if (!done) {
                    done = true;
                } else {
                    if (process.env.CHILD) {
                        process.send(leagueId, () => resolve());
                    } else {
                        resolve();
                    }
                }
            };

            child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
                .on('error', err => {
                    logger.error(err);
                    finish();
                })
                .on('message', teams => {
                    teams = Object.values(teams);
                    logger.log(`Processing ${teams.length} teams for league ${leagueName}...`);

                    if (teams.length > 0) {
                        for (const team of teams) {
                            const args = [team.name, team.shortname, siteUid, team.id];

                            stmt.updateTeam.run(args, function(err) {
                                if (err) {
                                    logger.error(err);
                                }

                                if (this.changes === 0) {
                                    stmt.insertTeam.run(args, err => {
                                        if (err) {
                                            logger.error(err);
                                        }

                                        stmt.insertLeagueTeam.run([leagueUid, team.id], err => {
                                            if (err) {
                                                logger.error(err);
                                            }

                                            finish();
                                        });
                                    });
                                } else {
                                    stmt.insertLeagueTeam.run([leagueUid, team.id], err => {
                                        if (err) {
                                            logger.error(err);
                                        }

                                        finish();
                                    });
                                }
                            });
                        }
                    } else {
                        finish();
                    }
                })
                .on('exit', finish);
        }));
    }, () => {
        Promise.all(children).then(() => {
            db.run('COMMIT', err => {
                if (err) {
                    logger.error(err);
                }

                stmt.deleteLeagueTeams.finalize();
                stmt.insertLeagueTeam.finalize();
                stmt.insertTeam.finalize();
                stmt.updateTeam.finalize();
            });
        });
    }
);

if (db !== global.db) {
    process.on('unhandledRejection', ex => {
        logger.error(ex);
    });

    process.on('beforeExit', () => {
        if (!db.open) {
            return;
        }

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.log('Closed connection to database');
            }
        });
    });
}
