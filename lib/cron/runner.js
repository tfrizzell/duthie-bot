/**
 * This module contains the cron tasks.
 */
'use strict';

const child_process = require('child_process');
const moment = require('moment');
const utils = require('../discord/utils');

const client = global.client;
const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const prepare = global.prepareStatement || db.prepare.bind(db);

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

class CronRunner {
    constructor() {
        this.processDailyStars = this.processDailyStars.bind(this);
        this.processGames = this.processGames.bind(this);
        this.processNews = this.processNews.bind(this);
        this.removeArchivedGuilds = this.removeArchivedData.bind(this);
        this.updateDailyStars = this.updateDailyStars.bind(this);
        this.updateGames = this.updateGames.bind(this);
        this.updateLeagues = this.updateLeagues.bind(this);
        this.updateNews = this.updateNews.bind(this);
        this.updateTeams = this.updateTeams.bind(this);
    }

    processDailyStars() {
        logger.debug('Processing daily stars');

        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = {
                updateDailyStars: prepare('UPDATE data_daily_stars SET queued = 0 WHERE leagueId = ?')
            };

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.all('SELECT * FROM daily_stars WHERE queued = 1',
                (err, rows) => {
                    if (err) {
                        return logger.error(err);
                    }

                    const stars = rows.reduce((stars, star) => {
                        stars[star.leagueId] = stars[star.leagueId] || {stars: {}, teams: []};

                        stars[star.leagueId].stars = {
                            ...stars[star.leagueId].stars,
                            [star.group]: [...(stars[star.leagueId].stars[star.group] || []), star]
                        };

                        if (!stars[star.leagueId].teams.includes(star.teamId)) {
                            stars[star.leagueId].teams.push(star.teamId);
                        }

                        return stars;
                    }, {});

                    Promise.all(Object.entries(stars).map(([leagueId, date]) => 
                        new Promise(resolve => {
                            db.all(`SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.typeId IN (3) AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (${data.teams.map(t => '?').join(',')}) GROUP BY watchers.guildId, watchers.channelId`, [leagueId, ...data.teams], 
                                (err, rows = []) => {
                                    if (err) {
                                        logger.error(err);
                                    }
        
                                    Promise.all(rows.map(row => 
                                        new Promise(resolve => {
                                            //const guild = client.guilds.get(row.guildId);
                                            const guild = client.guilds.get('279432958203396096')

                                            if (!guild) {
                                                return resolve();
                                            }

                                            //const channel = guild.channels.get(row.channelId) || utils.getDefaultChannel(guild);
                                            const channel = guild.channels.get('444708110830010378');

                                            if (!channel) {
                                                return resolve();
                                            }

                                            const buf = [];

                                            let leagueName = '';
                                            let teamName = '';
                                            let timestamp = '';

                                            for (const [group, stars] of Object.entries(data.stars)) {
                                                if (buf.length > 0) {
                                                    buf.push('');
                                                }

                                                for (const star of stars) {
                                                    if (!leagueName) {
                                                        leagueName = star.leagueName;
                                                    }

                                                    if (!timestamp) {
                                                        timestamp = star.timestamp;
                                                    }

                                                    if (row.teamId && row.teamId === star.teamId) {
                                                        teamName = star.teamName;
                                                    }

                                                    if (!row.teamId || row.teamId === star.teamId) {
                                                        buf.push(`    * ${utils.tagUser(star.playerName, guild)} - ${moment({d: star.rank}).format('Do')} Star ${group.replace(/^\./, a => a.toUpperCase())} __(${Object.entries(JSON.parse(star.metadata)).map(([key, val]) => `${val} ${key !== '+/-' ? key : ''}`.trim()).join(', ')})__`);
                                                    }
                                                }
                                            }

                                            if (teamName) {
                                                buf.unshift(`**Congratulations to the ${`${teamName}'s`.replace(/s's$/, `s'`)} ${leagueName} Daily Stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')}:**`);
                                            } else {
                                                buf.unshift(`**Congratulations to the ${leagueName} Daily Stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')}:**`);
                                            }

                                            if (row === rows[0]) {
                                                logger.verbose(`Sending ${leagueName} daily stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);

                                                channel.send(`[${leagueName}] ${buf.join('\n')}`)
                                                    .then(() => resolve())
                                                    .catch(err => {
                                                        logger.error(`Failed to send daily stars to ${guild.name}:`, err);
                                                        resolve();
                                                    });
                                            } else {
                                                resolve();
                                            }
                                        })
                                    )).then(() => {
                                        stmt.updateDailyStars.run([leagueId], err => {
                                            if (err) {
                                                logger.error(`Failed to dequeue daily stars:`, err);
                                            }

                                            resolve();
                                        });
                                    });
                                }
                            )
                        })
                    )).then(() => {
                        db.commit(err => {
                            if (err) {
                                logger.error(err);
                            }
            
                            db.finalize(stmt.updateDailyStars);
                        });
                    });
                }
            );
        });
    }

    processGames() {
        logger.debug('Processing game results');

        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = {
                getWatchers: prepare('SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.typeId IN (5) AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (?, ?)) GROUP BY watchers.guildId, watchers.channelId'),
                updateGame: prepare('UPDATE data_games SET queued = 0 WHERE id = ?')
            };

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.all('SELECT * FROM games WHERE queued = 1',
                (err, games = []) => {
                    if (err) {
                        return logger.error(err);
                    }

                    Promise.all(games.map(game => 
                        new Promise(resolve => {
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

                                    Promise.all(rows.map(row => 
                                        new Promise(resolve => {
                                            //const guild = client.guilds.get(row.guildId);
                                            const guild = client.guilds.get('279432958203396096')

                                            if (!guild) {
                                                return resolve();
                                            }

                                            //const channel = guild.channels.get(row.channelId) || utils.getDefaultChannel(guild);
                                            const channel = guild.channels.get('444708110830010378');

                                            if (!channel) {
                                                return resolve();
                                            }

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

                                            if (row === rows[0]) {
                                                logger.verbose(`Sending results for ${game.leagueName} game #${game.id} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);
                                                logger.info(`[${game.leagueName}] ${message}`);

                                                channel.send(`[${game.leagueName}] ${message}`)
                                                    .then(() => resolve())
                                                    .catch(err => {
                                                        logger.error(`Failed to send game ${game.id} to ${guild.name}:`, err);
                                                        resolve();
                                                    });
                                            } else {
                                                resolve();
                                            }
                                        })
                                    )).then(() => {
                                        stmt.updateGame.run([game.id], err => {
                                            if (err) {
                                                logger.error(`Failed to dequeue game ${game.id}:`, err);
                                            }

                                            resolve();
                                        });
                                    });
                                }
                            );
                        })
                    )).then(() => {
                        db.commit(err => {
                            if (err) {
                                logger.error(err);
                            }
            
                            db.finalize(stmt.updateGame);
                            db.finalize(stmt.getWatchers);
                        });
                    });
                }
            );
        });
    }

    processNews() {
        logger.debug('Processing news items');

        if (client.status !== 0) {
            return;
        }

        db.serialize(() => {
            const stmt = {
                updateNews: prepare('UPDATE data_news SET queued = 0 WHERE id = ?')
            };

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.all('SELECT * FROM news WHERE queued = 1',
                (err, items = []) => {
                    if (err) {
                        return logger.error(err);
                    }

                    Promise.all(items.map(item => 
                        new Promise(resolve => {
                            const teams = (item.teams || '').split(',');

                            db.all(`SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL LEFT JOIN watcher_types ON watcher_types.name = ? WHERE watchers.archived IS NULL AND watchers.typeId = IFNULL(watcher_types.id, 6) AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (${teams.map(t => '?').join(',')})) GROUP BY watchers.guildId, watchers.channelId`, [item.type, item.leagueId, ...teams], 
                                (err, rows = []) => {
                                    if (err) {
                                        logger.error(err);
                                    }

                                    Promise.all(rows.map(row => 
                                        new Promise(resolve => {
                                            //const guild = client.guilds.get(row.guildId);
                                            const guild = client.guilds.get('279432958203396096')

                                            if (!guild) {
                                                return resolve();
                                            }

                                            //const channel = guild.channels.get(row.channelId) || utils.getDefaultChannel(guild);
                                            const channel = guild.channels.get('444708110830010378');

                                            if (!channel) {
                                                return resolve();
                                            }

                                            if (row === rows[0]) {
                                                logger.verbose(`Sending ${item.leagueName} news item #${item.id} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);
                                                logger.info(`[${item.leagueName}] ${item.message}`);

                                                channel.send(`[${item.leagueName}] ${item.message}`)
                                                    .then(() => resolve())
                                                    .catch(err => {
                                                        logger.error(`Failed to news item ${item.id} to ${guild.name}:`, err);
                                                        resolve();
                                                    });
                                            } else {
                                                resolve();
                                            }
                                        })
                                    )).then(() => {
                                        stmt.updateNews.run([item.id], err => {
                                            if (err) {
                                                logger.error(`Failed to dequeue news item ${item.id}:`, err);
                                            }

                                            resolve();
                                        });
                                    });
                                }
                            );
                        })
                    )).then(() => {
                        db.commit(err => {
                            if (err) {
                                logger.error(err);
                            }
            
                            db.finalize(stmt.updateNews);
                        });
                    });
                }
            );
        });
    }

    removeArchivedData() {
        logger.debug('Starting archive data removal');

        db.serialize(() => {
            const timestamp = moment().subtract(7, 'days').toISOString();

            db.transaction(err => {
                if (err) {
                    logger.error(err);
                }
            });

            db.run('DELETE FROM guilds WHERE archived <= ?', [timestamp], function(err) {
                if (err) {
                    logger.error('Failed to remove archived guilds:', err);
                } else if (this.changes > 0) {
                    logger.info(`Removed ${this.changes} archived guilds from database`);
                }
            });

            db.run('DELETE FROM watchers WHERE archived <= ?', [timestamp], function(err) {
                if (err) {
                    logger.error('Failed to remove archived watchers:', err);
                } else if (this.changes > 0) {
                    logger.info(`Removed ${this.changes} archived watchers from database`);
                }
            });

            db.commit(err => {
                if (err) {
                    logger.error(err);
                }
            });
        });
    }

    updateDailyStars() {
        logger.debug('Starting daily star update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/daily-stars.js`, {env: {CHILD: true}}).on('exit', this.processDailyStars)
    }

    updateGames() {
        logger.debug('Starting game update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/games.js`, {env: {CHILD: true}}).on('exit', this.processGames);
    }

    updateLeagues() {
        logger.debug('Starting league info update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/info.js`, {env: {CHILD: true}});
    }

    updateNews() {
        logger.debug('Starting news update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/news.js`, {env: {CHILD: true}}).on('exit', this.processNews);
    }

    updateTeams() {
        logger.debug('Starting team info update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/teams.js`, {env: {CHILD: true}});
    }
}

module.exports = new CronRunner();
