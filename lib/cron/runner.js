/**
 * This module contains the cron tasks.
 */
'use strict';

const child_process = require('child_process');

const db = global.db || require('../db');
const logger = global.logger || require('../logger');

const client = global.client;
const prepare = (global.prepareStatement || db.prepare.bind(db));

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

class CronRunner {
    constructor() {
        this.processGames = this.processGames.bind(this);
        this.processNews = this.processNews.bind(this);
        this.processStars = this.processStars.bind(this);
        this.removeArchivedGuilds = this.removeArchivedGuilds.bind(this);
        this.updateGames = this.updateGames.bind(this);
        this.updateLeagues = this.updateLeagues.bind(this);
        this.updateNews = this.updateNews.bind(this);
        this.updateStars = this.updateStars.bind(this);
        this.updateTeams = this.updateTeams.bind(this);
    }

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

            db.each('SELECT * FROM games WHERE queued = 1',
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
                            logger.error(`Failed to dequeue game ${row.id}:`, err);
                        }
                    });
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                db.finalize(stmt);
            });
        });
    }

    processNews() {
        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = prepare('UPDATE data_news SET queued = 0 WHERE id = ?');

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.each('SELECT * FROM news WHERE queued = 1',
                (err, row) => {
                    if (err) {
                        return logger.error(err);
                    }

                    logger.info(`[${row.leagueName}] ${row.message}`);

                    stmt.run([row.id], err => {
                        if (err) {
                            logger.error(`Failed to dequeue news item ${row.id}:`, err);
                        }
                    });
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                db.finalize(stmt);
            });
        });
    }

    processStars() {
    }

    removeArchivedGuilds() {
        db.serialize(() => {
            const stmt = prepare('DELETE FROM guilds WHERE archived <= ?');

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            stmt.run([moment().subtract(7, 'days').toISOString()], err => {
                if (err) {
                    logger.error('Failed to remove archived guilds:', err);
                }
            });

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                db.finalize(stmt);
            });
        });
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
