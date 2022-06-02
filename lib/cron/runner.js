/**
 * This module contains the cron tasks.
 */
'use strict';

const child_process = require('child_process');
const Discord = require('discord.js');
const moment = require('moment');

const client = require('../discord/client');
const db = require('../db');
const logger = require('../logger');
const utils = require('../discord/utils');

if (!(client instanceof Discord.Client)) {
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
        child_process.fork(`${__dirname}/scripts/daily-stars.js`);
    }

    processGames() {
        logger.debug('Processing game results');
        child_process.fork(`${__dirname}/scripts/games.js`);
    }

    processNews() {
        logger.debug('Processing news items');
        child_process.fork(`${__dirname}/scripts/news.js`);
    }

    removeArchivedData() {
        logger.debug('Starting archive data removal');

        db.serialize(() => {
            const timestamp = moment().subtract(7, 'days').toISOString();

            db.transaction(err => {
                if (err) {
                    return logger.error(err);
                }

                Promise.all([
                    new Promise(_resolve => {
                        const resolve = (...args) => {
                            clearTimeout(timeout);
                            _resolve(...args);
                        };

                        const timeout = setTimeout(resolve, 50000);

                        db.run(`DELETE FROM guilds WHERE archived <= ?`, [timestamp], function(err) {
                            if (err) {
                                logger.error('Failed to remove archived guilds:', err);
                            } else if (this.changes > 0) {
                                logger.info(`Removed ${this.changes} archived guilds from database`);
                            }

                            resolve();
                        });
                    }),
                    new Promise(_resolve => {
                        const resolve = (...args) => {
                            clearTimeout(timeout);
                            _resolve(...args);
                        };

                        const timeout = setTimeout(resolve, 50000);

                        db.run(`DELETE FROM watchers WHERE archived <= ?`, [timestamp], function(err) {
                            if (err) {
                                logger.error('Failed to remove archived watchers:', err);
                            } else if (this.changes > 0) {
                                logger.info(`Removed ${this.changes} archived watchers from database`);
                            }

                            resolve();
                        });
                    })
                ]).then(() => {
                    db.commit(err => {
                        if (err) {
                            logger.error(err);
                        }
                    });
                });
            });
        });
    }

    updateDailyStars() {
        logger.debug('Starting daily star update script');
        child_process.fork(`${__dirname}/../scripts/sqlite/daily-stars.js`, {env: {CHILD: true}}).on('exit', this.processDailyStars);
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
