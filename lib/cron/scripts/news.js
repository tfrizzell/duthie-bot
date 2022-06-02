/**
 * This script processes news data and sends updates to subscribers.
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
    logger.debug('Processing news items');

    if (client === null || client.status !== 0) {
        return;
    }

    db.serialize(() => {
        db.transaction(err => {
            if (err) {
                return logger.error(err);
            }

            db.all(`SELECT * FROM news WHERE queued = 1 ORDER BY timestamp, id`, (err, items = []) => {
                if (err) {
                    return logger.error(err);
                }

                Promise.all(items.map(item =>
                    new Promise(_resolve => {
                        const resolve = (...args) => {
                            clearTimeout(timeout);
                            _resolve(...args);
                        };

                        const timeout = setTimeout(resolve, 50000);

                        const teams = (item.teams || '').split(',');
                        const type = `${item.type}s`.replace(/ss$/, 's');

                        db.all(`SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL JOIN watcher_types ON watcher_types.id = watchers.typeId WHERE watchers.archived IS NULL AND watcher_types.name = ? AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (${teams.map(t => '?').join(',')})) GROUP BY watchers.guildId, watchers.channelId`, [type, item.leagueId, ...teams],
                            (err, rows = []) => {
                                if (err) {
                                    logger.error(err);
                                }

                                Promise.all((item.type !== 'news' || /have (been eliminated|claimed|clinched|drafted|placed|traded)/i.test(item.message))
                                    ? rows.map(row =>
                                        new Promise(resolve => {
                                            const guild = client.guilds.get(row.guildId);

                                            if (!guild) {
                                                return resolve();
                                            }

                                            const channel = utils.getChannel(guild, row.channelId);

                                            if (!utils.hasAccess(channel)) {
                                                return resolve();
                                            }

                                            logger.verbose(`Sending ${item.leagueName} news item #${item.id} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);
                                            let message = item.message;

                                            if (item.type === 'bid') {
                                                message = message.replace(/(.*?) have earned the players rights for (.*?) with a bid amount of (\S+).*/i, (match, team, player, amount) => `${!item.customTeams ? 'The ' : ''}${team} have won the rights to ${utils.tagUser(player, guild, false)} with a bid of ${bid}!`);
                                            } else if (item.type === 'contract') {
                                                message = message.replace(/(.*?) and the (.*?) have agreed to a (\d+) season deal at (\S+) per season/i, (match, player, team, length, amount) => `${!item.customTeams ? 'The ' : ''}${team} have signed ${utils.tagUser(player, guild, false)} to a ${length} season contract worth ${amount} per season!`);
                                            } else if (item.type === 'draft') {
                                                message = message.replace(/The (.*?) have drafted (.*?) (\S+) overall in season (\d+) of the (.*)/i, (match, team, player, position, season, league) => `${!item.customTeams ? 'The ' : ''}${team} have drafted ${utils.tagUser(player, guild, false)} ${position} overall in the ${league} season ${season} draft!`);
                                            }

                                            channel.send(utils.escape(message), {split: true})
                                                .then(() => resolve())
                                                .catch(err => {
                                                    logger.error(`Failed to news item ${item.id} to ${guild.name}:`, err);
                                                    resolve();
                                                });
                                        })
                                    )
                                    : []
                                ).then(() => {
                                    db.run(`UPDATE data_news SET queued = 0 WHERE id = ?`, [item.id], err => {
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
                    });
                });
            });
        });
    });
});

client
	.login(config.token)
    .catch(err => {client.emit('error', err)});
