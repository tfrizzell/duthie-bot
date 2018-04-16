'use strict';
require('../../global');

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require(`${__libdir}/db`);
const logger = global.logger || require(`${__libdir}/logger`);

const stmt = {
    deleteGames: db.prepare('DELETE FROM data_games WHERE leagueId = ?'),
    insertGame: db.prepare('INSERT OR IGNORE INTO data_games (timestamp, visitorTeam, visitorScore, homeTeam, homeScore, leagueId, gameId) VALUES (?, ?, ?, ?, ?, ?, ?)'),
    updateGame: db.prepare('UPDATE data_games SET timestamp = ?, visitorTeam = ?, visitorScore = ?, homeTeam = ?, homeScore = ? WHERE leagueId = ? AND gameId = ?')
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

        const child = `${__libdir}/scripts/leagues/${siteId}/schedule.js`;

        if (!fs.existsSync(child)) {
            return children.push(Promise.resolve());
        }

        if (!transactionStarted) {
            transactionStarted = true;
            db.run('BEGIN TRANSACTION');
        }

        logger.log(`Retrieving schedule for league ${leagueName}...`);

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

            child_process.fork(child, [JSON.stringify({...JSON.parse(row.extraData), leagueId: leagueId})], {env: {CHILD: true}})
                .on('error', err => {
                    logger.error(err);
                    finish();
                })
                .on('message', games => {
                    logger.log(`Processing ${games.length} games for league ${leagueName}...`);

                    if (games.length > 0) {
                        for (const game of games) {
                            const args = [game.date, game.visitor.id, game.visitor.score, game.home.id, game.home.score, leagueUid, game.id];

                            stmt.updateGame.run(args, function(err) {
                                if (err) {
                                    logger.error(err);
                                }

                                if (this.changes === 0) {
                                    stmt.insertGame.run(args, err => {
                                        if (err) {
                                            logger.error(err);
                                        }

                                        finish();
                                    });
                                } else {
                                    finish();
                                }
                            });
                        }
                    } else {
                        stmt.deleteGames.run([leagueUid], err => {
                            if (err) {
                                logger.error(err);
                            }
        
                            finish();
                        });
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

                stmt.deleteGames.finalize();
                stmt.insertGame.finalize();
                stmt.updateGame.finalize();
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
