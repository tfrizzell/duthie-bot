/**
 * This module binds all Discord message event handlers to the provided client.
 * 
 * Usage: require('./messages')
 */
'use strict';

const commands = require('./commands');
const DuthieCommand = require('./command');
const utils = require('./utils');

const client = global.client;
const db = global.db || require('../db');

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

// Runs every time a message is received from Discord
client.on('message', message => {
    if (message.author.bot) {
        return;
    }

    const command = new DuthieCommand(message);

    if (command.prefix !== config.prefix) {
        return;
    }

    if (message.channel.type === 'voice') {
        logger.debug(`Discarding message ${message.content}: received over voice channel`);

        return messsage.channel
            .send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you can't do that here.`)
            .catch(err => logger.error(err));
    }

    if (typeof commands[command.name] !== 'function') {
        logger.debug(`Discarding message ${message.content}: command '${command.name}' not found`);

        return messsage.channel
            .send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I don't know ${command.name} is.`)
            .catch(err => logger.error(err));
    }

    try {
        commands[command.name](message, command);
    } catch (err) {
        logger.error(err);
        message.channel.send(`Oh no! Something went wrong and I was unable to complete your request, ${utils.tagUser(message.author, message.guild)}!`);
    }
});
