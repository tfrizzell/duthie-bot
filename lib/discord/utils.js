/**
 * This module provides a small library of utilities for use in the Discord modules.
 */
'use strict';

const client = global.client;
const Discord = global.Discord || require('discord.js');

if (!client) {
    throw new ReferenceError('Discord client not found!');
}

module.exports = {
    escape: (message) => {
        if (typeof message !== 'string') {
            return message;
        }

        return message.replace(/[\*\_\~\`]/g, a => `\\${a}`);
    },
    getDefaultChannel: (guild) => {
        if (!(guild instanceof Discord.Guild)) {
            return undefined;
        }

        return guild.channels.filter(channel => 
            (channel.type == 'text') && 
            channel.permissionsFor(client.user).has(Discord.Permissions.FLAGS.READ_MESSAGES)
        ).sort((a, b) => a.calculatedPosition - b.calculatedPosition).first();
    },
    tagUser: (guild, user) => {
        if (user instanceof Discord.GuildMember) {
            return `<@${user.id}>`;
        }

        if (guild instanceof Discord.Guild) {
            const regex = new RegExp(user, 'i');
            const member = guild.members.find(member => regex.test(member.nickname) || regex.test(member.user.username));

            if (member) {
                return `<@${member.id}>`;
            }
        }

        return user;
    }
};
