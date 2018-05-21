/**
 * This module provides a set of commands for use by the developer.
 */
'use strict';

const Discord = require('discord.js');

const client = require('../client');
const logger = require('../../logger');
const runner = require('../../cron/runner')
const utils = require('../utils');

const config = require('../../../config.json');

if (!(client instanceof Discord.Client)) {
    throw new ReferenceError('Discord client not found!');
}

module.exports = (message, command) => {
    if (!config.devId || message.author.id !== config.devId) {
        logger.error(`Developer command issued by non-developer ${message.author.tag}: ${message.content}`);

        return messsage.channel
            .send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I don't know what ${command.name} is.`)
            .catch(err => logger.error(err));
    }

    return module.exports[command.subcommand](message, command, ...command.arguments.slice(1));
};

module.exports.announce = (message, command, ...args) => {
    logger.debug(`${message.author.tag} has broadcasted an announcement: ${args.join(' ')}`);
    const msg = utils.escape(args.join(' '));

    for (const [id, guild] of client.guilds) {
        const channel = utils.getDefaultChannel(guild);

        if (channel) {
            channel.send(`**ANNOUNCEMENT:** ${msg}`);
        }
    }

    message.author.send(`I have finished broadcasting your announcement:\n\`\`\`\n${msg}\`\`\``);
};

module.exports.broadcast = (message, command, ...args) => {
    logger.debug(`${message.author.tag} has broadcasted a message: ${args.join(' ')}`);
    const msg = utils.escape(args.join(' '));

    for (const [id, guild] of client.guilds) {
        const channel = utils.getDefaultChannel(guild);

        if (channel) {
            channel.send(msg);
        }
    }

    message.author.send(`I have finished broadcasting your message:\n\`\`\`\n${msg}\`\`\``);
};

module.exports.echo = (message, command, ...args) => {
    const msg = utils.escape(args.join(' '));
    message.author.send(msg);
    logger.debug(`${message.author.tag} has echoed a message: ${msg}`);
};

module.exports.message = (message, command, ...args) => {
    const [type, id, ...buf] = args;
    const msg = utils.escape(buf.join(' '));

    switch (type) {
        case 'guild':
        case 'server':
            const guild = client.guilds.get(id);

            if (!guild) {
                return message.author.send(`I was unable to find guild ${id} to send your message`);
            }

            const channel = utils.getDefaultChannel(guild);
    
            if (channel) {
                return;
            }

            logger.debug(`${message.author.tag} has sent a message to guild ${guild.name} (${guild.id}): ${msg}`);
           channel.send(msg);
            message.author.send(`I have finished sending your message to guild ${guild.name} (${guild.id}):\n\`\`\`\n${msg}\`\`\``);
        break;

        case 'user':
            client.fetchUser(id)
                .then(user => {
                    logger.debug(`${message.author.tag} has sent a message to user ${user.tag}: ${msg}`);
                    user.send(msg);
                    message.author.send(`I have finished sending your message to ${utils.tagUser(user)}:\n\`\`\`\n${msg}\`\`\``);
                })
                .catch(() => {
                    message.author.send(`I was unable to find user ${id} to send your message`);
                });
        break;
    }
};

module.exports.run = (message, command, ...args) => {
    for (const cmd of args) {
        if (!runner[cmd]) {
            continue;
        }

        logger.debug(`${message.author.tag} has executed cron job ${cmd}`);
        runner[cmd]();
    }
};

module.exports.shutdown = (message, command, ...args) => {
    logger.debug(`${message.author.tag} has issued a shutdown command`);
    process.emit('dev.shutdown');
};
