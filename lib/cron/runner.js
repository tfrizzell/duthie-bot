/**
 * This module contains the cron tasks.
 */
'use strict';

const child_process = require('child_process');

const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const utils = require('../discord/utils');

const client = global.client;
const prepare = (global.prepareStatement || db.prepare.bind(db));

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

class CronRunner {
    constructor() {
        this.processDailyStars = this.processDailyStars.bind(this);
        this.processGames = this.processGames.bind(this);
        this.processNews = this.processNews.bind(this);
        this.removeArchivedGuilds = this.removeArchivedGuilds.bind(this);
        this.updateDailyStars = this.updateDailyStars.bind(this);
        this.updateGames = this.updateGames.bind(this);
        this.updateLeagues = this.updateLeagues.bind(this);
        this.updateNews = this.updateNews.bind(this);
        this.updateTeams = this.updateTeams.bind(this);
    }

    processDailyStars() {
        db.run('UPDATE data_daily_stars SET queued = 0 WHERE queued = 1');
        return;

        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = {
                getWatchers: prepare('SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.leagueId = ? AND watchers.typeId IN (3) AND (watchers.teamId IS NULL OR watchers.teamId = ?) GROUP BY watchers.guildId, watchers.channelId'),
                updateStar: prepare('UPDATE data_daily_stars SET queued = 0 WHERE leagueId = ? AND posGroup = ? AND rank = ?')
            }

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.each('SELECT * FROM daily_stars WHERE queued = 1',
                (err, star) => {
                    if (err) {
                        return logger.error(err);
                    }

                    stmt.getWatchers.all([star.leagueId, star.teamId], (err, rows = []) => {
                            if (err) {
                                logger.error(err);
                            }

                            Promise.all(rows.map(row => new Promise(resolve => {
                                const guild = client.guilds.get(row.guildId);

                                /*if (!guild) {
                                    return resolve();
                                }

                                const channel = guild.channels.get(row.channelId) || utils.getDefaultChannel(guild);

                                if (!channel) {
                                    return resolve();
                                }*/

                                const us = (row.teamId === visitor.id) ? visitor : home;
                                const usText = (us.name !== us.shortname) ? `The **${utils.escape(us.name)}** have` : `**${utils.escape(us.name)}** has`;
                                const them = (row.teamId === visitor.id) ? home : visitor;
                                const themText = (them.name !== them.shortname) ? `the **${utils.escape(them.name)}**` : `**${utils.escape(them.name)}**`;
                                let message;

                                if (us.score > them.score) {
                                    message = `${usText} defeated ${themText} by the score of **${us.score} to ${them.score}**!`;
                                } else if (us.score === them.score) {
                                    message = `${usText.replace(/\*\*/g, '')} tied ${themText.replace(/\*\*/g, '')} by the score of ${us.score} to ${them.score}.`;
                                } else {
                                    message = `${usText.replace(/\*\*/g, '__')} been defeated by ${themText.replace(/\*\*/g, '__')} by the score of __${them.score} to ${us.score}__.`;
                                }

                                /*channel.send(message)
                                    .then(() => resolve())
                                    .catch(err => {
                                        logger.error(`Failed to send game ${game.id} message to ${guild.name}:`, err);
                                        resolve();
                                    });*/

                                if (row === rows[0]) {
                                    logger.info(`[${game.leagueName}] ${message}`);
                                }

                                resolve();
                            }))).then(() => {
                                stmt.updateStar.run([game.id], err => {
                                    if (err) {
                                        logger.error(`Failed to dequeue game ${game.id}:`, err);
                                    }
                                });
                            });
                        }
                    );
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                db.finalize(stmt.updateGame);
                db.finalize(stmt.getWatchers);
            });
        });
    }

    processGames() {
        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = {
                getWatchers: prepare('SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.leagueId = ? AND watchers.typeId IN (5) AND (watchers.teamId IS NULL OR watchers.teamId IN (?, ?)) GROUP BY watchers.guildId, watchers.channelId'),
                updateGame: prepare('UPDATE data_games SET queued = 0 WHERE id = ?')
            }

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.each('SELECT * FROM games WHERE queued = 1',
                (err, game) => {
                    if (err) {
                        return logger.error(err);
                    }

                    const visitor = {
                        id: game.visitorTeam,
                        name: game.visitorName,
                        shortname: game.visitorShortname,
                        score: game.visitorScore
                    };

                    const home = {
                        id: game.homeTeam,
                        name: game.homeName,
                        shortname: game.homeShortname,
                        score: game.homeScore
                    };

                    stmt.getWatchers.all([game.leagueId, visitor.id, home.id], (err, rows = []) => {
                            if (err) {
                                logger.error(err);
                            }

                            Promise.all(rows.map(row => new Promise(resolve => {
                                const guild = client.guilds.get(row.guildId);

                                /*if (!guild) {
                                    return resolve();
                                }

                                const channel = guild.channels.get(row.channelId) || utils.getDefaultChannel(guild);

                                if (!channel) {
                                    return resolve();
                                }*/

                                const us = (row.teamId === visitor.id) ? visitor : home;
                                const usText = (us.name !== us.shortname) ? `The **${utils.escape(us.name)}** have` : `**${utils.escape(us.name)}** has`;
                                const them = (row.teamId === visitor.id) ? home : visitor;
                                const themText = (them.name !== them.shortname) ? `the **${utils.escape(them.name)}**` : `**${utils.escape(them.name)}**`;
                                let message;

                                if (us.score > them.score) {
                                    message = `${usText} defeated ${themText} by the score of **${us.score} to ${them.score}**!`;
                                } else if (us.score === them.score) {
                                    message = `${usText.replace(/\*\*/g, '')} tied ${themText.replace(/\*\*/g, '')} by the score of ${us.score} to ${them.score}.`;
                                } else {
                                    message = `${usText.replace(/\*\*/g, '__')} been defeated by ${themText.replace(/\*\*/g, '__')} by the score of __${them.score} to ${us.score}__.`;
                                }

                                /*channel.send(message)
                                    .then(() => resolve())
                                    .catch(err => {
                                        logger.error(`Failed to send game ${game.id} message to ${guild.name}:`, err);
                                        resolve();
                                    });*/

                                if (row === rows[0]) {
                                    logger.info(`[${game.leagueName}] ${message}`);
                                }

                                resolve();
                            }))).then(() => {
                                stmt.updateGame.run([game.id], err => {
                                    if (err) {
                                        logger.error(`Failed to dequeue game ${game.id}:`, err);
                                    }
                                });
                            });
                        }
                    );
                }
            );

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }

                db.finalize(stmt.updateGame);
                db.finalize(stmt.getWatchers);
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

    updateDailyStars() {
        child_process.fork(`${__dirname}/../scripts/sqlite/daily-stars.js`, {env: {CHILD: true}}).on('exit', this.processDailyStars)
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

    updateTeams() {
        child_process.fork(`${__dirname}/../scripts/sqlite/teams.js`, {env: {CHILD: true}}).on('exit', next);
    }
}

module.exports = new CronRunner();
