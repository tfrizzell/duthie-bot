/**
 * This script calls the schedule.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger').new({level: process.env.CHILD ? 'LOG_WARN' : 'LOG_DEBUG'});

const stmt = {
    deleteGames: db.prepare('DELETE FROM data_games WHERE leagueId = ?'),
    insertGame: db.prepare('INSERT OR IGNORE INTO data_games (timestamp, visitorTeam, visitorScore, homeTeam, homeScore, leagueId, gameId) VALUES (?, ?, ?, ?, ?, ?, ?)'),
    updateGame: db.prepare('UPDATE data_games SET timestamp = ?, visitorTeam = ?, visitorScore = ?, homeTeam = ?, homeScore = ? WHERE leagueId = ? AND gameId = ?')
};

let transactionStarted = false;
db.serialize();

// TODO: Run query through watchers table
db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
    (err, row) => {
        if (err) {
            return logger.error(err);
        }

        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
        const child = `${__dirname}/../leagues/${siteId}/schedule.js`;

        if (!fs.existsSync(child)) {
            return;
        }

        db.transactional();
        logger.log(`Retrieving schedule for league ${leagueName}...`);

        child_process.fork(child, [JSON.stringify({...JSON.parse(row.extraData), leagueId: leagueId})], {env: {CHILD: true}})
            .on('error', err => {
                logger.error(err);
            })
            .on('message', games => {
                logger.log(`Processing ${games.length} games for league ${leagueName}...`);

                if (games.length > 0) {
                    for (const game of games) {
                        const args = [game.date, game.visitor.id, game.visitor.score, game.home.id, game.home.score, leagueUid, game.id];

                        stmt.updateGame.run(args, function(err) {
                            if (err) {
                                return logger.error(err);
                            }

                            if (this.changes > 0) {
                                return;
                            }

                            stmt.insertGame.run(args, err => {
                                if (err) {
                                    logger.error(err);
                                }
                            });
                        });
                    }
                } else {
                    stmt.deleteGames.run([leagueUid], err => {
                        if (err) {
                            logger.error(err);
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

        stmt.deleteGames.finalize();
        stmt.insertGame.finalize();
        stmt.updateGame.finalize();

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
}
