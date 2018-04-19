/**
 * This module binds all non-message Discord event handler to the provided client.
 * 
 * Usage: require('./events')(Discord.Client)
 */
'use strict';

const Discord = global.Discord || require('discord.js');
const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const moment = require('moment');
const prepare = global.prepareStatement || db.prepare.bind(db);

const stmt = {
    guildCreated: prepare('INSERT OR IGNORE INTO guilds (guildId, defaultChannelId) VALUES (?, ?)'),
    guildDeleted: prepare('UPDATE OR IGNORE guilds SET deleted = ? WHERE guildId = ?'),
    guildRejoined: prepare('UPDATE guilds SET deleted = NULL WHERE guildId = ?')
};

module.exports = (client) => {
    const getDefaultChannel = (guild) => guild.channels.filter(channel => (channel.type == 'text') && channel.permissionsFor(client.user).has(Discord.Permissions.FLAGS.READ_MESSAGES)).sort((a, b) => a.calculatedPosition - b.calculatedPosition).first();
    let reconnected = false;

    client.once('ready', () => {
        // This is run the first time the client connects to Discord
    });

    client.on('ready', () => {
        if (reconnected) {
            logger.info('Reconnected to Discord');
        }

        // This is run every time the client connects to Discord

        reconnected = true;
    });

    client.on('guildCreate', guild => {
        const defaultChannel = getDefaultChannel(guild);
        const args = [guild.id, channel ? channel.id : undefined];

        stmt.guildRejoined.run(args, function(err) {
            if (err) {
                logger.error(err);
            }

            if (this.changes > 0) {
                return;
            }

            stmt.guildCreated.run(args, err => {
                if (err) {
                    logger.error(err);
                }
            });
        });
    });

    client.on('guildDelete', guild => {
        stmt.guildDeleted.run([guild.id, moment().toISOString], function(err) {
            if (err) {
                logger.error(err);
            }
        });
    });

    client.on('disconnect', e => {
        logger.info('Lost connection to Discord');
    });
};
