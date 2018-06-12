/**
 * This script processes game result data and sends updates to subscribers.
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
    logger.debug('Processing game results');

    if (client === null || client.status !== 0) {
        return;
    }

    db.serialize(() => {
        db.transaction(err => {
            if (err) {
                return logger.error(err);
            }

            db.all(`SELECT * FROM games WHERE queued = 1 ORDER BY timestamp, gameId, id`, (err, games = []) => {
                if (err) {
                    return logger.error(err);
                }

                Promise.all(games.map(game =>
                    new Promise(_resolve => {
                        const resolve = (...args) => {
                            clearTimeout(timeout);
                            _resolve(...args);
                        };

                        const timeout = setTimeout(resolve, 50000);

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

                        db.all(`SELECT watchers.* FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL WHERE watchers.archived IS NULL AND watchers.typeId IN (5) AND watchers.leagueId = ? AND (watchers.teamId IS NULL OR watchers.teamId IN (?, ?)) GROUP BY watchers.guildId, watchers.channelId`, [game.leagueId, visitor.id, home.id],
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

                                        const us = (row.teamId === home.id || (!row.teamId && home.score >= visitor.score)) ? home : visitor;
                                        const usText = !game.customTeams ? `The **${utils.escape(us.name)}** have` : `**${utils.escape(us.name)}** has`;
                                        const them = (us === home) ? visitor : home;
                                        const themText = !game.customTeams ? `the **${utils.escape(them.name)}**` : `**${utils.escape(them.name)}**`;
                                        let message;

                                        if (us.score > them.score) {
                                            message = `${usText} defeated ${themText} by the score of **${us.score} to ${them.score}**!`;
                                        } else if (us.score === them.score) {
                                            message = `${usText.replace(/\*\*/g, '')} tied ${themText.replace(/\*\*/g, '')} by the score of ${us.score} to ${them.score}.`;
                                        } else {
                                            message = `${usText.replace(/\*\*/g, '_')} been defeated by ${themText.replace(/\*\*/g, '_')} by the score of _${them.score} to ${us.score}_.`;
                                        }

                                        logger.verbose(`Sending results for ${game.leagueName} game #${game.id} to channel ${channel.name} on guild ${guild.name} (${guild.id})`);

                                        channel.send(message, {split: true})
                                            .then(() => resolve())
                                            .catch(err => {
                                                logger.error(`Failed to send game ${game.id} to ${guild.name}:`, err);
                                                resolve();
                                            });
                                    })
                                )).then(() => {
                                    db.run(`UPDATE data_games SET queued = 0 WHERE id = ?`, [game.id], err => {
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
                    });
                });
            });
        });
    });
});

client
	.login(config.token)
    .catch(err => {client.emit('error', err)});
