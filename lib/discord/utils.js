/**
 * This module provides a small library of utilities for use in the Discord modules.
 */
'use strict';

const Discord = require('discord.js');

const client = require('./client');
const db = require('../db');

if (!(client instanceof Discord.Client)) {
    throw new ReferenceError('Discord client not found!');
}

const stmt = {
    isAdmin: db.prepare(`SELECT COUNT(*) AS count FROM guild_admins WHERE guildId = ? AND memberId = ?`)
};

module.exports.escape = message => {
    if (typeof message !== 'string') {
        return message;
    }

    return message.replace(/[\*\_\~\`]/g, a => `\\${a}`);
};

module.exports.getChannel = (guild, channel) => {
    if (!(guild instanceof Discord.Guild)) {
        return;
    }

    if (!(channel instanceof Discord.GuildChannel)) {
        channel = guild.channels.get(channel);
    }

    if (!(channel instanceof Discord.GuildChannel) || !channel.permissionsFor(client.user).has([Discord.Permissions.FLAGS.READ_MESSAGES, Discord.Permissions.FLAGS.SEND_MESSAGES])) {
        channel = module.exports.getDefaultChannel(guild);
    }

    return channel;
};

module.exports.getDefaultChannel = guild => {
    if (!(guild instanceof Discord.Guild)) {
        return;
    }

    return guild.channels.filter(channel => 
        (channel.type == 'text') && 
        channel.permissionsFor(client.user).has([Discord.Permissions.FLAGS.READ_MESSAGES, Discord.Permissions.FLAGS.SEND_MESSAGES])
    ).sort((a, b) => a.calculatedPosition - b.calculatedPosition).first() || {id: undefined};
};

module.exports.getGuildMember = (user, guild) => {
    if (!(guild instanceof Discord.Guild)) {
        return;
    }

    let member;

    if (typeof user === 'string') {
        member = guild.members.get(user) || guild.members.get(user.replace(/^<\@(.*?)>$/, '$1'));
    }

    if (!member) {
        if ((user instanceof Discord.GuildMember) || (user instanceof Discord.User)) {
            member = guild.members.get(user.id);
        } else {
            const regex = new RegExp(user, 'i');
            member = guild.members.find(member => regex.test(member.nickname) || regex.test(member.user.username));
        }
    }

    return member;
};

module.exports.getUserNickname = (user, guild) => {
    if (guild instanceof Discord.Guild) {
        user = module.exports.getGuildMember(user, guild);
    }

    if (user instanceof Discord.GuildMember) {
        return module.exports.escape(user.nickname || user.user.username);
    } else if (user instanceof Discord.User) {
        return module.exports.escape(user.username);
    }

    return module.exports.escape(user);
};

module.exports.isAdmin = (user, guild) => new Promise((resolve, reject) => {
    if (module.exports.isOwner(user, guild)) {
        return resolve();
    }

    if ((!(user instanceof Discord.GuildMember) && !(user instanceof Discord.User)) || !(guild instanceof Discord.Guild)) {
        return reject();
    }

    stmt.isAdmin.get([guild.id, user.id], (err, {count = 0} = {}) => {
        if (count > 0) {
            resolve();
        } else {
            reject();
        }
    });
});

module.exports.isOwner = (user, guild) => 
    ((user instanceof Discord.GuildMember) || (user instanceof Discord.User)) && (guild instanceof Discord.Guild) && (guild.owner.id === user.id);

module.exports.tagUser = (user, guild) => {
    let member = user;

    if (guild instanceof Discord.Guild) {
        member = module.exports.getGuildMember(member, guild);
    }

    if ((member instanceof Discord.GuildMember) || (member instanceof Discord.User)) {
        return `<@${member.id}>`;
    }

    return module.exports.escape(user);
};
