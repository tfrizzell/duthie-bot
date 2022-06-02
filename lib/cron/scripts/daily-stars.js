/**
 * This script processes daily star data and sends updates to subscribers.
 */
'use strict';

const Discord = require('discord.js');
const moment = require('moment');

const config = require('../../../config.json');
const db = require('../../db');
const logger = require('../../logger');

const client = require('../../discord/client').create();
const utils = require('../../discord/utils');

require('../../discord/events');
client.removeAllListeners('ready');
client.removeAllListeners('guildCreate');
client.removeAllListeners('guildDelete');
client.removeAllListeners('disconnect');

client.once('ready', () => {
    logger.debug('Processing daily stars');

    if (client === null || client.status !== 0) {
        return;
    }

    db.serialize(() => {
        db.transaction(err => {
            if (err) {
                return logger.error(err);
            }

            db.all(`SELECT * FROM daily_stars WHERE queued = 1`, (err, rows) => {
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

                Promise.all(Object.entries(stars).map(([leagueId, data]) =>
                    new Promise(_resolve => {
                        const resolve = (...args) => {
                            clearTimeout(timeout);
                            _resolve(...args);
                        };

                        const timeout = setTimeout(resolve, 50000);

                        db.all(`SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.typeId IN (3) AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (${data.teams.map(t => '?').join(',')})) GROUP BY watchers.guildId, watchers.channelId`, [leagueId, ...data.teams],
                            (err, rows = []) => {
                                if (err) {
                                    logger.error(err);
                                }

                                Promise.all(rows.map(row =>
                                    new Promise(resolve => {
                                        const guild = client.guilds.get(row.guildId);

                                        if (!guild) {
                                            return resolve();
                                        }

                                        const channel = utils.getChannel(guild, row.channelId);

                                        if (!utils.hasAccess(channel)) {
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
                                                    buf.push(`    * ${utils.tagUser(star.playerName, guild)} - ${moment({d: star.rank}).format('Do')} Star ${group.replace(/^./, a => a.toUpperCase()).replace(/s$/, '')} _(${Object.entries(JSON.parse(star.metadata)).map(([key, val]) => `${val} ${key !== '+/-' ? key : ''}`.trim()).join(', ')})_`);
                                                }
                                            }
                                        }

                                        if (teamName) {
                                            buf.unshift(`__**Congratulations to the ${`${utils.escape(teamName)}'s`.replace(/s's$/, `s'`)} ${utils.escape(leagueName)} Daily Stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')}:**__`);
                                        } else {
                                            buf.unshift(`__**Congratulations to the ${utils.escape(leagueName)} Daily Stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')}:**__`);
                                        }

                                        logger.verbose(`Sending ${leagueName} Daily Stars for ${moment(timestamp).subtract(1, 'day').format('dddd, MMMM Do, YYYY')} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);

                                        channel.send(buf.join('\n'), {split: true})
                                            .then(() => resolve())
                                            .catch(err => {
                                                logger.error(`Failed to send daily stars to ${guild.name}:`, err);
                                                resolve();
                                            });
                                    })
                                )).then(() => {
                                    db.run(`UPDATE data_daily_stars SET queued = 0 WHERE leagueId = ?`, [leagueId], err => {
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
                    });
                });
            });
        });
    });
});

client
	.login(config.token)
    .catch(err => {client.emit('error', err)});
