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
    archiveGuild: prepare('UPDATE guilds SET archived = ? WHERE id = ?'),
    createGuild: prepare('INSERT INTO guilds (id) VALUES (?)'),
    unarchiveGuild: prepare('UPDATE guilds SET archived = NULL WHERE id = ?')
};

let reconnected = false;

// Runs the first time the client connects to Discord
client.once('ready', () => {
    logger.info('Opened connection to Discord');
    client.user.setActivity(`initializing...`);

    /*Promise.all(client.guilds.map(guild => 
        new Promise(resolve => {
            stmt.unarchiveGuild.run(guild.id, function(err) {
                if (err) {
                    logger.error(err);
                }
    
                if (this.changes > 0) {
                    logger.info(`${config.name} has rejoined ${guild.name}`);
                    return resolve(guild.id);
                }
    
                const channel = utils.getDefaultChannel(guild);

                stmt.createGuild.run([guild.id, channel ? channel.id : undefined], err => {
                    if (err) {
                        logger.error(err);
                    } else {
                        logger.info(`${config.name} has joined ${guild.name}`);
                    }

                    resolve(guild.id);
                });
            });
        })
    )).then(ids => {
        db.run(`UPDATE guilds SET archived = ? WHERE id NOT IN (${ids.map(id => '?').join(',')}) AND archived IS NULL`, [ids]);
    });*/
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
            logger.info(`${config.name} has rejoined ${guild.name}`);
            return;
        }

        stmt.createGuild.run([guild.id], err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info(`${config.name} has joined ${guild.name}`);
            }
        });
    });
});

// Runs every time the bot is removed from a guild
client.on('guildDelete', guild => {
    stmt.archiveGuild.run([guild.id, moment().toISOString], function(err) {
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
