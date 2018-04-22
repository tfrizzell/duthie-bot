/**
 * This script calls the games.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');
const moment = require('moment');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger');

const prepare = global.prepareStatement || db.prepare.bind(db);

const stmt = {
    deleteLeagueGames: prepare('DELETE FROM data_games WHERE leagueId = ?'),
    insertGame: prepare('INSERT INTO data_games (timestamp, visitorTeam, visitorScore, homeTeam, homeScore, leagueId, gameId) VALUES (?, ?, ?, ?, ?, ?, ?)'),
    updateGame: prepare('UPDATE data_games SET timestamp = ?, visitorTeam = ?, visitorScore = ?, homeTeam = ?, homeScore = ? WHERE leagueId = ? AND gameId = ?')
};

db.serialize();

db.transaction(err => {
    if (err) {
        logger.error(err);
    }
});

db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN sites ON sites.id = leagues.siteId WHERE watchers.typeId IN (5) GROUP BY sites.id, leagues.id',
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
                        const args = [
                            moment(game.date).toISOString(),
                            game.visitor.id,
                            game.visitor.score,
                            game.home.id,
                            game.home.score,
                            leagueUid,
                            game.id
                        ];

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
                    stmt.deleteLeagueGames.run([leagueUid], err => {
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

    db.finalize(stmt.deleteLeagueGames);
    db.finalize(stmt.insertGame);
    db.finalize(stmt.updateGame);

    db.close(err => {
        if (err) {
            logger.error(err);
        } else {
            logger.info('Closed connection to database');
        }
    });
});
