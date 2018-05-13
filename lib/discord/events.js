/**
 * This module binds all non-message Discord event handler to the provided client.
 * 
 * Usage: require('./events')
 */
'use strict';

const Discord = require('discord.js');
const moment = require('moment');

const utils = require('./utils');

const client = global.client;
const db = global.db || require('../db');
const logger = global.logger || require('../logger');
const prepare = global.prepareStatement || db.prepare.bind(db);

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

const stmt = {
    archiveGuild: prepare('UPDATE guilds SET archived = ? WHERE id = ? AND archived IS NULL'),
    createGuild: prepare('INSERT OR IGNORE INTO guilds (id) VALUES (?)'),
    unarchiveGuild: prepare('UPDATE guilds SET archived = NULL WHERE id = ? AND archived IS NOT NULL')
};

let reconnected = false;

// Runs the first time the client connects to Discord
client.once('ready', () => {
    logger.info('Opened connection to Discord');
    client.user.setActivity(`initializing...`);

    const ids = [-1, ...client.guilds.map(guild => guild.id)];
    const timestamp = moment().toISOString();

    db.run(`UPDATE guilds SET archived = ? WHERE id NOT IN (${ids.map(id => '?').join(',')}) AND archived IS NULL`, [timestamp, ...ids], function(err) {
        if (err) {
            logger.error('Failed to archive guilds:', err);
        } else if (this.changes > 0) {
            logger.info(`${this.changes} guilds have been archived because ${config.name} is no longer a member of them`);
        }

        for (const [id, guild] of client.guilds) {
            client.emit('guildCreate', guild);
        }
    });
});

// Runs every time the client connects to Discord
client.on('ready', () => {
    if (reconnected) {
        logger.info('Reconnected to Discord');
    }

    reconnected = true;
    client.user.setActivity(`${pkg.version} BETA`);
});

// Runs every time the bot is added to a guild
client.on('guildCreate', guild => {
    stmt.unarchiveGuild.run(guild.id, function(err) {
        if (err) {
            logger.error(err);
        }

        if (this.changes > 0) {
            return logger.info(`${config.name} has rejoined ${guild.name}`);
        }

        stmt.createGuild.run([guild.id], function(err) {
            if (err) {
                logger.error(err);
            } else if (this.changes > 0) {
                logger.info(`${config.name} has joined ${guild.name}`);
            }
        });
    });
});

// Runs every time the bot is removed from a guild
client.on('guildDelete', guild => {
    stmt.archiveGuild.run([guild.id, moment().toISOString()], function(err) {
        if (err) {
            logger.error(err);
        } else if (this.changes > 0) {
            logger.info(`${config.name} has left ${guild.name}`);
        }
    });
});

// Runs every time the client disconnects from Discord
client.on('disconnect', e => {
    logger.info('Lost connection to Discord');
});
