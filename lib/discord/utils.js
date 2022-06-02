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
        channel = channel ? guild.channels.get(channel) : module.exports.getDefaultChannel(guild);
    }

    return channel;
};

module.exports.getDefaultChannel = guild => {
    if (!(guild instanceof Discord.Guild)) {
        return;
    }

    return guild.channels.filter(channel => 
        (channel.type == 'text') && module.exports.hasAccess(channel)
    ).sort((a, b) => a.calculatedPosition - b.calculatedPosition).first();
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

module.exports.hasAccess = (channel) => (channel instanceof Discord.GuildChannel) && channel.permissionsFor(client.user).has([Discord.Permissions.FLAGS.READ_MESSAGES, Discord.Permissions.FLAGS.SEND_MESSAGES]);

module.exports.isAdmin = (user, guild) => new Promise((resolve, reject) => {
    if (module.exports.isOwner(user, guild)) {
        return resolve();
    }

    if ((!(user instanceof Discord.GuildMember) && !(user instanceof Discord.User)) || !(guild instanceof Discord.Guild)) {
        return reject();
    }

    db.get(`SELECT COUNT(*) AS count FROM guild_admins WHERE guildId = ? AND memberId = ?`, [guild.id, user.id], (err, {count = 0} = {}) => {
        if (count > 0) {
            resolve();
        } else {
            reject();
        }
    });
});

module.exports.isOwner = (user, guild) => 
    ((user instanceof Discord.GuildMember) || (user instanceof Discord.User)) && (guild instanceof Discord.Guild) && (guild.owner.id === user.id);

module.exports.tagUser = (user, guild, escapeString = true) => {
    let member = user;

    if (guild instanceof Discord.Guild) {
        member = module.exports.getGuildMember(member, guild);
    }

    if ((member instanceof Discord.GuildMember) || (member instanceof Discord.User)) {
        return `<@${member.id}>`;
    }

    return escapeString ? module.exports.escape(user) : user;
};
