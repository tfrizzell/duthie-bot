/**
 * This module provides the `admin` command.
 */
'use strict';

const db = require('../../db');
const logger = require('../../logger');
const utils = require('../utils');

const addAdmin = ([guildId, memberId], callback) => 
    db.run(`INSERT INTO guild_admins (guildId, memberId) VALUES (?, ?)`, [guildId, memberId], function(err, ...args) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => addAdmin([guildId, memberId], callback), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                throw err;
            }
        }

        if (typeof callback === 'function') {
            callback.call(this, err, ...args);
        }
    });

const removeAdmin = ([guildId, memberId], callback) => 
    db.run(`DELETE FROM guild_admins WHERE guildId = ? AND memberId = ?`, [guildId, memberId], function(err, ...args) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => removeAdmin([guildId, memberId], callback), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                throw err;
            }
        }

        if (typeof callback === 'function') {
            callback.call(this, err, ...args);
        }
    });

module.exports = (message, command) => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](message, command, ...command.arguments.slice(1));
    } else if (command.subcommand === 'list') {
        return require('./list').admins(message, command, ...command.arguments.slice(1));
    } else {
        return require('./help').admin(message, command, ...command.tokens.slice(1));
    }
};

module.exports.add = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} is attempting to add an administrator on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify([args.join(' ')])}`);
    const user = args.join(' ');

    if (!utils.isOwner(message.author, message.guild)) {
        logger.error(`${message.author.tag} attempted to add an administrator on guild ${message.guild.name} (${message.guild.id}) but is not the guild owner`);
        message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you aren't allow to do that!`);
    }

    let member = utils.getGuildMember(user, message.guild);

    if (!member) {
        logger.error(`${message.author.tag} requested unknown user ${user} on guild ${message.guild.name} (${message.guild.id})`);
        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to a find user ${user} on your server.`);
    }

    addAdmin([message.guild.id, member.id], function(err) {
        if (this.changes > 0) {
            logger.debug(`${message.author.tag} has added ${member.user.tag} as an administrator on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(command.arguments)}`);
            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! You have added ${utils.getUserNickname(member, message.guild)} as an administrator on your server!`);
        } else {
            message.channel.send(`${utils.tagUser(message.author, message.guild)}, it appears that ${utils.getUserNickname(member, message.guild)} is already an administrator on your server!`);
        }
    });
};

module.exports.remove = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} is attempting to remove an administrator on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify([args.join(' ')])}`);
    const user = args.join(' ');

    if (!utils.isOwner(message.author, message.guild)) {
        logger.error(`${message.author.tag} attempted to remove an administrator on guild ${message.guild.name} (${message.guild.id}) but is not the guild owner`);
        message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you aren't allow to do that!`);
    }

    let member = utils.getGuildMember(user, message.guild);

    if (!member) {
        logger.error(`${message.author.tag} requested unknown user ${user} on guild ${message.guild.name} (${message.guild.id})`);
        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to a find user ${user} on your server.`);
    }

    removeAdmin([message.guild.id, member.id], function(err) {
        if (this.changes > 0) {
            logger.debug(`${message.author.tag} has removed ${member.user.tag} as an administrator on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(command.arguments)}`);
            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! You have removed ${utils.getUserNickname(member, message.guild)} as an administrator on your server!`);
        } else {
            message.channel.send(`${utils.tagUser(message.author, message.guild)}, it appears that ${utils.getUserNickname(member, message.guild)} is not an administrator on your server!`);
        }
    });
};
