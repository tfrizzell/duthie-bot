/**
 * This script calls the teams.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger').new({level: process.env.CHILD ? 'LOG_WARN' : 'LOG_DEBUG'});

const stmt = {
    deleteLeagueTeams: db.prepare('DELETE FROM league_teams WHERE leagueId = ?'),
    insertLeagueTeam: db.prepare('INSERT OR IGNORE INTO league_teams (leagueId, teamId) VALUES (?, ?)'),
    insertTeam: db.prepare('INSERT OR IGNORE INTO teams (name, shortname, siteId, teamId) VALUES (?, ?, ?, ?)'),
    updateTeam: db.prepare('UPDATE teams SET name = ?, shortname = ? WHERE siteId = ? AND teamId = ?')
};

db.serialize();

// TODO: Run query through watchers table
db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
    (err, row) => {
        if (err) {
            return logger.error(err);
        }

        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
        const child = `${__dirname}/../leagues/${siteId}/teams.js`;

        if (!fs.existsSync(child)) {
            return;
        }

        db.transactional();
        logger.debug(`Retrieving teams for league ${leagueName}...`);

        child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
            .on('error', err => {
                logger.error(err);
            })
            .on('message', teams => {
                teams = Object.values(teams);
                logger.debug(`Processing ${teams.length} teams for league ${leagueName}...`);

                if (teams.length === 0) {
                    return;
                }

                for (const team of teams) {
                    const args = [team.name, team.shortname, siteUid, team.id];

                    stmt.updateTeam.run(args, function(err) {
                        if (err) {
                            return logger.error(err);
                        }

                        if (this.changes === 0) {
                            stmt.insertTeam.run(args, err => {
                                if (err) {
                                    return logger.error(err);
                                }

                                stmt.insertLeagueTeam.run([leagueUid, team.id], err => {
                                    if (err) {
                                        logger.error(err);
                                    }
                                });
                            });
                        } else {
                            stmt.insertLeagueTeam.run([leagueUid, team.id], err => {
                                if (err) {
                                    logger.error(err);
                                }
                            });
                        }
                    });
                }
            }
        );
    }
);

if (global.db !== db) {
    require('../../node/exceptions');

    require('../../node/cleanup')(() => {
        if (!db.open) {
            return;
        }

        db.commit(err => {
            if (err) {
                logger.error(err);
            }
        });

        stmt.deleteLeagueTeams.finalize();
        stmt.insertLeagueTeam.finalize();
        stmt.insertTeam.finalize();
        stmt.updateTeam.finalize();

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
}
