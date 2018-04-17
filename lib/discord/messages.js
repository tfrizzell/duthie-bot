/**
 * This module binds all Discord message event handlers to the provided client.
 * 
 * Usage: require('messages.js')(Discord.Client)
 */
'use strict';

const cmd = require('./commands');
const db = global.db || require('../db');

module.exports = (client) => {
    client.on('message', message => {
        // This is run any time a message is received by the bot
    });
};
