/**
 * This module binds all non-message Discord event handler to the provided client.
 * 
 * Usage: require('events.js')(Discord.Client)
 */
'use strict';

const db = global.db || require('../db');

module.exports = (client) => {
    let reconnected = false;

    client.once('ready', () => {
        // This is run the first time the client connects to Discord
    });

    client.on('ready', () => {
        if (reconnected) {
            logger.debug('Reconnected to Discord');
        }

        // This is run every time the client connects to Discord

        reconnected = true;
    });

    client.on('guildCreate', guild => {
        // This is run when the bot is added to a new server
    });

    client.on('guildDelete', guild => {
        // This is run when the bot is removed from a server
    });

    client.on('disconnect', () => {
        logger.debug('Lost connection to Discord');
    })
};
