/**
 * This script calls the games.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');
const moment = require('moment');

const db = require('../../db');
const logger = require('../../logger');

const stmt = {
    deleteLeagueGames: db.prepare(`DELETE FROM data_games WHERE leagueId = ?`),
    getLeagues: db.prepare(`SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0`),
    insertGame: db.prepare(`INSERT INTO data_games (timestamp, visitorTeam, visitorScore, homeTeam, homeScore, leagueId, gameId) VALUES (?, ?, ?, ?, ?, ?, ?)`),
    updateGame: db.prepare(`UPDATE data_games SET timestamp = ?, visitorTeam = ?, visitorScore = ?, homeTeam = ?, homeScore = ? WHERE leagueId = ? AND gameId = ?`)
};

const MAX_CHILDREN = {maxChildren: 'Infinity', ...require('../../../config.json').scripts}.maxChildren;
let leagues = [];

const fetchGames = (league = {}) => {
    const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = league;
    const script = `${__dirname}/../leagues/${siteId}/games.js`;

    if (!siteId || !fs.existsSync(script)) {
        if (leagues.length > 0) {
            fetchGames(leagues.shift());
        }

        return;
    }

    logger.info(`Retrieving games for league ${leagueName}...`);

    child_process.fork(script, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
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
        })
        .on('exit', () => {
            fetchGames(leagues.shift());
        });
};

stmt.getLeagues.all((err, rows = []) => {
    if (err) {
        return logger.error(err);
    }

    db.transaction(err => {
        if (err) {
            return logger.error(err);
        }

        db.serialize(() => {
            leagues = rows;

            for (const league of leagues.splice(0, MAX_CHILDREN)) {
                fetchGames(league);
            }
        });
    });
});

require('../../node/exceptions');

require('../../node/cleanup')(() => {
    if (!db.open) {
        return;
    }

    db.commit(() => {
        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
});
