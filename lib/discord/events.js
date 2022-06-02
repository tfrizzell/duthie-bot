/**
 * This module binds all non-message Discord event handler to the provided client.
 * 
 * Usage: require('./events')
 */
'use strict';

const Discord = require('discord.js');
const moment = require('moment');

const client = require('./client');
const db = require('../db');
const logger = require('../logger');
const utils = require('./utils');

const config = require('../../config.json');
const pkg = require('../../package.json');

if (!(client instanceof Discord.Client)) {
    throw new ReferenceError('Discord client not found!');
}

let reconnected = false;

// Runs the first time the client connects to Discord
client.once('ready', () => {
    logger.info(`Connected to Discord! ${config.name} is active in ${client.guilds.size} ${client.guilds.size !== 1 ? 'guilds' : 'guild'}`);
    client.user.setActivity(`initializing...`);

    const ids = [-1, ...client.guilds.map(guild => guild.id)];
    const timestamp = moment().toISOString();

    db.run(`UPDATE guilds SET archived = ? WHERE id NOT IN (${ids.map(id => '?').join(',')}) AND archived IS NULL`, [timestamp, ...ids], function(err) {
        if (err) {
            logger.error('Failed to archive guilds:', err);
        } else if (this.changes > 0) {
            logger.info(`${this.changes} ${this.changes !== 1 ? 'guilds have' : 'guild has'} been archived because ${config.name} is no longer a member of ${this.changes !== 1 ? 'them' : 'it'}`);
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

// Runs every time an error occurs
client.on('error', err => {
    if (Object(err) === err) {
        if (/getaddrinfo ENOTFOUND/i.test(err.message)) {
            return logger.error(`Failed to connect to Discord: could not resolve address ${err.message.replace(/.*? ENOTFOUND (\S+).*/, '$1')}`);
        } else if (err.message) {
            return logger.error(err.message);
        }
    }

    logger.error(err);
});

// Runs every time the bot is added to a guild
client.on('guildCreate', guild => {
    db.run(`UPDATE guilds SET archived = NULL WHERE id = ? AND archived IS NOT NULL`, [guild.id], function(err) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => client.emit('guildCreate', guild), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                return logger.error(err);
            }
        }

        if (this.changes > 0) {
            return logger.info(`${config.name} has rejoined ${guild.name} (${guild.id})`);
        }

        db.run(`INSERT INTO guilds (id) VALUES (?)`, [guild.id], function(err) {
            if (err) {
                if (err.code !== 'SQLITE_CONSTRAINT') {
                    logger.error(err);
                }
            } else if (this.changes > 0) {
                logger.info(`${config.name} has joined ${guild.name} (${guild.id})`);
            }
        });
    });
});

// Runs every time the bot is removed from a guild
client.on('guildDelete', guild => {
    db.run(`UPDATE guilds SET archived = ? WHERE id = ? AND archived IS NULL`, [guild.id, moment().toISOString()], function(err) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => client.emit('guildDelete', guild), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                logger.error(err);
            }
        } else if (this.changes > 0) {
            logger.info(`${config.name} has left ${guild.name} (${guild.id})`);
        }
    });
});

// Runs every time the client disconnects from Discord
client.on('disconnect', () => {
    logger.info('Lost connection to Discord');
});
