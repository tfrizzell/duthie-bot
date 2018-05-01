/**
 * This module binds all Discord message event handlers to the provided client.
 * 
 * Usage: require('./messages')
 */
'use strict';

const db = global.db || require('../db');

const commands = require('./commands');
const CommandResponse = require('./response');

const client = global.client;

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

client.on('message', message => {
    // This is run any time a message is received by the bot
    if (message.author.bot || !new RegExp(`^(\`${config.prefix} .*?\`$|${config.prefix} )`).test(message.content.trim())) {
        return;
    }

    if (message.channel.type === 'voice') {
        return messsage.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you can't do that here.`)
            .catch(err => logger.error(err));
    }

    const msg = message.content.trim().replace(/^`(.*?)`$/, '$1');
    const cmd = msg.split(/\s+/)[1];

    if (!commands[cmd]) {
        return messsage.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I don't know what you're asking me for.`)
            .catch(err => logger.error(err));
    }

    commands[cmd](message)
        .then(response => {
            if (!(response instanceof CommandResponse)) {
                return message.channel.send(response)
                    .catch(err => logger.error(err));
            }

            response.channel.send(response.content, response.options)
                .catch(err => logger.error(err));
        }).catch(err => logger.error(err));
});
