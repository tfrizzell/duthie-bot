/**
 * This module provides the `list` command.
 */
'use strict';

const utils = require('../utils');

const config = global.config || require('../../../config.json');
const db = global.db || require('../../db');
const logger = global.logger || require('../../logger');

const outputList = (headers, objs) => {
    const rows = objs.map(obj => Object.values(obj));

    const width = rows.reduce((width, row) => {
        for (let i = 0; i < headers.length; i++) {
            if (row[i] === undefined) {
                row[i] = '';
            } else {
                row[i] = String(row[i]);
            }

            width[i] = Math.max(width[i], row[i].length);
        }

        return width;
    }, headers.map(header => header.length));

    headers = headers.map((header, index) => String(header).padEnd(width[index]));
    const separator = width.map(width => Array(width).fill('-').join('')).join('---');

    return  [
        `--${separator}--`,
        `| ${headers.join(' | ')} |`,
        `--${separator}--`,
        ...rows.map(row => `| ${row.map((value, index) => !isNaN(value) ? value.padStart(width[index]) : value.padEnd(width[index])).join(' | ')} |`),
        `--${separator}--`
    ].join('\n');
};

module.exports = (message, command) => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](message, command, ...command.arguments.slice(1));
    } else {
        return require('./help').list(message, command, ...command.tokens.slice(1));
    }
};

module.exports.admins = module.exports.administrators = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested an administrator list for guild ${message.guild.name} (${message.guild.id})`);
    const headers = ['Nickname', 'Tag', 'Level'];
    const ownerId = message.guild.ownerID;

    db.all(`SELECT memberId FROM guild_admins WHERE guildId = ? AND memberId != ?`, [message.guild.id, ownerId],
        (err, admins = []) => {
            if (err) {
                throw err;
            }

            admins = [{memberId: ownerId}, ...admins].reduce((admins, admin) => {
                const member = message.guild.members.get(admin.memberId);
                return member ? [...admins, {nickname: utils.getUserNickname(member), tag: member.user.tag, level: (member.id === ownerId ? 'Owner' : 'Admin')}] : admins;
            }, []).sort((a, b) => {
                if (a.level === 'Owner' && b.level !== 'Owner') {
                    return -1;
                } else if (a.level !== 'Owner' && b.level === 'Owner') {
                    return 1;
                } else {
                    return a.nickname.toLowerCase().localeCompare(b.nickname.toLowerCase());
                }
            });

            message.channel.send(outputList(headers, admins), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.leagues = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested a league list: ${JSON.stringify(args)}`);
    const headers = ['ID', 'Name', 'Code', 'Site'];
    let {site = ''} = command.params;

    db.all(`SELECT leagues.id, leagues.name, leagues.codename, sites.name AS siteName FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0 AND UPPER(?) IN (UPPER(sites.id), UPPER(sites.siteId), UPPER(sites.name), '') ORDER BY leagues.name, sites.name`, [site],
        (err, leagues = []) => {
            if (err) {
                throw err;
            }

            message.channel.send(outputList(headers, leagues), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.sites = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested a site list`);
    const headers = ['ID', 'Name'];

    db.all(`SELECT id, name FROM sites ORDER BY id, name`, 
        (err, sites = []) => {
            if (err) {
                throw err;
            }

            message.channel.send(outputList(headers, sites), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.teams = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested a team list: ${JSON.stringify(args)}`);
    const headers = ['ID', 'Name', 'Shortname', 'League(s)', 'Site'];
    const {site = '', league = ''} = command.params;

    db.all(`SELECT teams.id, teams.name, teams.shortname, REPLACE(GROUP_CONCAT(DISTINCT leagues.name), ',', ', ') AS leagueNames, REPLACE(GROUP_CONCAT(DISTINCT sites.name), ',', ', ') AS siteNames FROM teams JOIN league_team_map ON league_team_map.teamId = teams.id JOIN leagues ON leagues.id = league_team_map.leagueId AND leagues.siteId = sites.id JOIN team_map ON team_map.teamId = teams.id JOIN sites ON sites.id = team_map.siteId WHERE UPPER(?) IN (UPPER(sites.id), UPPER(sites.siteId), UPPER(sites.name), '') AND UPPER(?) IN (UPPER(leagues.id), UPPER(leagues.name), leagues.codename, '') AND leagues.disabled = 0 GROUP BY teams.id ORDER BY teams.name, leagues.name, sites.name`, [site, league],
        (err, teams = []) => {
            if (err) {
                throw err;
            }

            teams = teams.map(team => ({...team, leagueNames: `${team.leagueNames.substring(0, 30)}${team.leagueNames.length > 30 ? '...' : ''}`}));
            message.channel.send(outputList(headers, teams), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.watchers = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested a watcher list for guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(args)}`);
    const headers = ['League', 'Team', 'Type', 'Channel', 'Site'];
    const {type = '', site = '', league = '', team = '', channel = ''} = command.params;

    if (channel && isNaN(channel)) {
        logger.error(`${message.author.tag} requested unknown channel ${channel} on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(command.arguments)}`);
        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to find the channel ${channel} in your server.`);
    }

    db.all(`SELECT leagues.name AS leagueName, IFNULL(teams.name, 'All Teams') AS teamName, watcher_types.name AS type, watchers.channelId AS channel, sites.name AS siteName FROM watchers JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL JOIN watcher_types ON watcher_types.id = watchers.typeId JOIN leagues ON leagues.id = watchers.leagueId LEFT JOIN teams ON teams.id = watchers.teamId JOIN sites ON sites.id = leagues.siteId WHERE watchers.archived IS NULL AND watchers.guildId = ? AND UPPER(?) IN (UPPER(watcher_types.id), UPPER(watcher_types.name), '') AND UPPER(?) IN (UPPER(sites.id), UPPER(sites.siteId), UPPER(sites.name), '') AND UPPER(?) IN (UPPER(leagues.id), UPPER(leagues.name), leagues.codename, '') AND UPPER(?) IN (UPPER(teams.id), UPPER(teams.name), UPPER(teams.shortname), teams.codename, teams.codeshortname, '') AND ? IN (watchers.channelId, '') GROUP BY watchers.id ORDER BY leagues.id, leagues.name, teams.id, teams.name, watcher_types.name, channelId, sites.id, sites.name`, [message.guild.id, type, site, league, team, channel],
        (err, watchers = []) => {
            if (err) {
                throw err;
            }

            const defaults = {};

            message.channel.send(outputList(headers, watchers.map(watcher => {
                const channel = message.guild.channels.get(watcher.channel) || defaults[message.guild.id] || (defaults[message.guild.id] = utils.getDefaultChannel(message.guild));
                return ({...watcher, channel: {name: 'unknown-channel', ...channel}.name});
            })), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.watcherTypes = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested a watcher type list`);
    const headers = ['ID', 'Name', 'Description'];

    db.all(`SELECT id, name, description FROM watcher_types`, 
        (err, types = []) => {
            if (err) {
                throw err;
            }

            types = [
                {id: '', name: 'all', description: 'an alias for all types'},
                {id: '', name: 'all-news', description: 'an alias for all news related types'},
                ...types
            ].sort((a,b) => a.name.localeCompare(b.name));

            message.channel.send(outputList(headers, types), {code: 'mysql', split: 'true'});
        }
    );
};
