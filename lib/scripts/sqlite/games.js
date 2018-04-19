/**
 * This script calls the games.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const db = global.db || require('../../db');
const fs = require('fs');
const logger = global.logger || require('../../logger');
const moment = require('moment-timezone');
const prepare = global.prepareStatement || db.prepare.bind(db);

const stmt = {
    deleteGames: prepare('DELETE FROM data_games WHERE leagueId = ?'),
    insertGame: prepare('INSERT OR IGNORE INTO data_games (timestamp, visitorTeam, visitorScore, homeTeam, homeScore, leagueId, gameId) VALUES (?, ?, ?, ?, ?, ?, ?)'),
    updateGame: prepare('UPDATE data_games SET timestamp = ?, visitorTeam = ?, visitorScore = ?, homeTeam = ?, homeScore = ? WHERE leagueId = ? AND gameId = ?')
};

moment.tz.setDefault('America/New_York');
db.serialize();

db.transaction(err => {
    if (err) {
        logger.error(err);
    }
});

// TODO: Run query through watchers table
db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
    (err, row) => {
        if (err) {
            return logger.error(err);
        }

        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
        const child = `${__dirname}/../leagues/${siteId}/games.js`;

        if (!fs.existsSync(child)) {
            return;
        }

        logger.info(`Retrieving games for league ${leagueName}...`);

        child_process.fork(child, [JSON.stringify({...JSON.parse(row.extraData), leagueId: leagueId})], {env: {CHILD: true}})
            .on('error', err => {
                logger.error(err);
            })
            .on('message', games => {
                logger.info(`Processing ${games.length} games for league ${leagueName}...`);

                if (games.length > 0) {
                    for (const game of games) {
                        const args = [moment(game.date).toISOString(), game.visitor.id, game.visitor.score, game.home.id, game.home.score, leagueUid, game.id];

                        stmt.updateGame.run(args, function(err) {
                            if (err) {
                                logger.error(err);
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
