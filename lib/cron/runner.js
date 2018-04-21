/**
 * This module contains the cron tasks.
 */
'use strict';

const child_process = require('child_process');

const client = global.client;
const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const prepare = (global.prepareStatement || db.prepare.bind(db));

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

class CronRunner {
    processGames() {
        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = prepare('UPDATE data_games SET queued = 0 WHERE id = ?');

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.each('SELECT game.id, league.id AS leagueId, league.name AS leagueName, visitor.teamId AS visitorId, visitor.name AS visitorName, visitor.shortname AS visitorShortname, game.visitorScore, home.teamId AS homeId, home.name AS homeName, home.shortname AS homeShortname, game.homeScore FROM data_games game JOIN leagues league ON league.id = game.leagueId JOIN teams visitor ON visitor.siteId = league.siteId AND visitor.teamId = game.visitorTeam JOIN teams home ON home.siteId = league.siteId AND home.teamId = game.homeTeam WHERE game.queued = 1',
                (err, row) => {
                    if (err) {
                        return logger.error(err);
                    }

                    let winningTeam;
                    let winningScore;
                    let losingTeam;
                    let losingScore;

                    if (row.homeScore >= row.visitorScore) {
                        winningTeam = (row.homeName !== row.homeShortname) ? `The ${row.homeName} have` : `${row.homeName} has`;
                        winningScore = row.homeScore;

                        losingTeam = (row.visitorName !== row.visitorShortname) ? `the ${row.visitorName}` : `${row.visitorName}`;
                        losingScore = row.visitorScore;
                    } else {
                        winningTeam = (row.visitorName !== row.visitorShortname) ? `The ${row.visitorName} have` : `${row.visitorName} has`;
                        winningScore = row.visitorScore;

                        losingTeam = (row.homeName !== row.homeShortname) ? `the ${row.homeName}` : `${row.homeName}`;
                        losingScore = row.homeScore;
                    }

                    logger.info(`[${row.leagueName}] ${winningTeam} ${(winningScore !== losingScore) ? 'defeated' : 'tied'} ${losingTeam} ${winningScore}-${losingScore}`);

                    stmt.run([row.id], err => {
                        if (err) {
                            logger.error(err);
                        }
                    });
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                stmt.finalize();
            });
        });
    }

    processNews() {
        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = prepare('UPDATE data_news SET queued = 0 WHERE id = ?');
            let count = 0;

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.each('SELECT * FROM data_news WHERE queued = 1',
                (err, row) => {
                    count++;

                    stmt.run([row.id], err => {
                        if (err) {
                            logger.error(err);
                        }
                    });
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                logger.warn(`Updated ${count} news items`);
                stmt.finalize();
            });
        });
    }

    processStars() {
    }

    updateGames() {
        child_process.fork(`${__dirname}/../scripts/sqlite/games.js`, {env: {CHILD: true}}).on('exit', this.processGames);
    }

    updateLeagues() {
        child_process.fork(`${__dirname}/../scripts/sqlite/leagues.js`, {env: {CHILD: true}}).on('exit', next);
    }

    updateNews() {
        child_process.fork(`${__dirname}/../scripts/sqlite/news.js`, {env: {CHILD: true}}).on('exit', this.processNews);
    }

    updateStars() {
        child_process.fork(`${__dirname}/../scripts/sqlite/stars.js`, {env: {CHILD: true}}).on('exit', this.processStars)
    }

    updateTeams() {
        child_process.fork(`${__dirname}/../scripts/sqlite/teams.js`, {env: {CHILD: true}}).on('exit', next);
    }
}

module.exports = new CronRunner();
