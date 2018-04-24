/**
 * This module binds all non-message Discord event handler to the provided client.
 * 
 * Usage: require('./events')(Discord.Client)
 */
'use strict';

const moment = require('moment');

const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const utils = require('./utils');

const Discord = global.Discord || require('discord.js');
const prepare = global.prepareStatement || db.prepare.bind(db);

const stmt = {
    archiveGuild: prepare('UPDATE guilds SET archived = ? WHERE id = ?'),
    createGuild: prepare('INSERT INTO guilds (id, defaultChannelId) VALUES (?, ?)'),
    unarchiveGuild: prepare('UPDATE guilds SET archived = NULL WHERE id = ?')
};

module.exports = (client) => {
    let reconnected = false;

    // Runs the first time the client connects to Discord
    client.once('ready', () => {
        
    });

    // Runs every time the client connects to Discord
    client.on('ready', () => {
        if (reconnected) {
            logger.info('Reconnected to Discord');
        }

        reconnected = true;
    });

    // Runs every time the bot is added to a guild
    client.on('guildCreate', guild => {
        const channel = utils.getDefaultChannel(guild);

        stmt.unarchiveGuild.run(guild.id, function(err) {
            if (err) {
                logger.error(err);
            }

            if (this.changes > 0) {
                return;
            }

            stmt.createGuild.run([guild.id, channel ? channel.id : undefined], err => {
                if (err) {
                    logger.error(err);
                }
            });
        });
    });

    // Runs every time the bot is removed from a guild
    client.on('guildDelete', guild => {
        stmt.archiveGuild.run([guild.id, moment().toISOString], function(err) {
            if (err) {
                logger.error(err);
            }
        });
    });

    // Runs every time the client disconnects from Discord
    client.on('disconnect', e => {
        logger.info('Lost connection to Discord', e);
    });
};
