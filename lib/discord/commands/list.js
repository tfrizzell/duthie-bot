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

module.exports = command => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](command, ...command.arguments.slice(1));
    } else {
        return module.exports.help(command, ...command.arguments);
    }
};

module.exports.help = (command, ...args) => {
    const message = command.message;
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} list\`:

# SYNTAX
    ${config.prefix} list <dataset>[ <parameters>]
    ${config.prefix} list leagues[ site=<site>]
    ${config.prefix} list sites
    ${config.prefix} list teams[ site=<site>][ league=<league>]
    ${config.prefix} list watcher-types
    ${config.prefix} list watchers[ type=<type>][ site=<site>][ league=<league>][ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# DATASET (required)
    Below are the datasets currently supported by \`${config.prefix} list\`:
        * leagues       - list all leagues supported by ${config.name}
        * sites         - list all sites supported by ${config.name}
        * teams         - list all teams in the supported sites and leagues
        * watcher-types - list all watcher types supported by ${config.name}
        * watchers      - list all watchers registered for your server

# TYPE (optional, mode=watchers)
    When listing the watchers on your server, you may provide any valid watcher type to filter the output on. See \`${config.prefix} list watcher-types\` for a list of valid types.

# SITE (optional)
    When listing in any mode except sites, you may provide any site to filter the output on. See \`${config.prefix} list sites\` for a list of valid sites.

# LEAGUE (optional, mode=teams,watchers)
    When listing teams or watchers, you may provide any league to filter the output on. See \`${config.prefix} list leagues\` for a list of valid leagues.

# TEAM (optional, mode=watchers)
    When listing watchers, you may provide any team to filter the output on. See \`${config.prefix} list teams\` for a list of valid teams.

# CHANNEL (optional, mode=watchers)
    When listing watchers, you may provide any channel to filter the output on.`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, {code: 'vb', split: 'true'});
    } else {
        message.channel.send(response, {code: 'vb', split: 'true'});
    }
};

module.exports.leagues = (command, ...args) => {
    const headers = ['ID', 'Name', 'Code', 'Site'];
    const message = command.message;
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

module.exports.sites = (command, ...args) => {
    const headers = ['ID', 'Name'];
    const message = command.message;

    db.all(`SELECT id, name FROM sites ORDER BY id, name`, 
        (err, sites = []) => {
            if (err) {
                throw err;
            }

            message.channel.send(outputList(headers, sites), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.teams = (command, ...args) => {
    const headers = ['ID', 'Name', 'Shortname', 'League(s)', 'Site'];
    const message = command.message;
    const {site = '', league = ''} = command.params;

    db.all(`SELECT teams.id, teams.name, teams.shortname, REPLACE(GROUP_CONCAT(DISTINCT leagues.name), ',', ', ') AS leagueNames, REPLACE(GROUP_CONCAT(DISTINCT sites.name), ',', ', ') AS siteNames FROM teams JOIN league_team_map ON league_team_map.teamId = teams.id JOIN leagues ON leagues.id = league_team_map.leagueId AND leagues.siteId = sites.id JOIN team_map ON team_map.teamId = teams.id JOIN sites ON sites.id = team_map.siteId WHERE UPPER(?) IN (UPPER(sites.id), UPPER(sites.siteId), UPPER(sites.name), '') AND UPPER(?) IN (UPPER(leagues.id), UPPER(leagues.name), UPPER(leagues.codename), '') AND leagues.disabled = 0 GROUP BY teams.id ORDER BY teams.name, leagues.name, sites.name`, [site, league],
        (err, teams = []) => {
            if (err) {
                throw err;
            }

            teams = teams.map(team => ({...team, leagueNames: `${team.leagueNames.substring(0, 30)}${team.leagueNames.length > 30 ? '...' : ''}`}));
            message.channel.send(outputList(headers, teams), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.watchers = (command, ...args) => {
    const headers = ['League', 'Team', 'Type', 'Channel', 'Site'];
    const message = command.message;
    const guild = message.guild;
    const {type = '', site = '', league = '', team = '', channel = ''} = command.params;

    db.all(`SELECT leagues.name AS leagueName, IFNULL(teams.name, 'All Teams') AS teamName, watcher_types.name AS type, watchers.channelId AS channel, sites.name AS siteName FROM watchers JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL JOIN watcher_types ON watcher_types.id = watchers.typeId JOIN leagues ON leagues.id = watchers.leagueId LEFT JOIN teams ON teams.id = watchers.teamId JOIN sites ON sites.id = leagues.siteId WHERE watchers.archived IS NULL AND watchers.guildId = ? AND UPPER(?) IN (UPPER(watcher_types.id), UPPER(watcher_types.name), '') AND UPPER(?) IN (UPPER(sites.id), UPPER(sites.siteId), UPPER(sites.name), '') AND UPPER(?) IN (UPPER(leagues.id), UPPER(leagues.name), UPPER(leagues.codename), '') AND UPPER(?) IN (UPPER(teams.id), UPPER(teams.name), UPPER(teams.shortname), UPPER(teams.codename), '') AND ? IN (watchers.channelId, '') GROUP BY watchers.id ORDER BY leagues.id, leagues.name, teams.id, teams.name, watcher_types.name, channelId, sites.id, sites.name`, [guild.id, type, site, league, team, channel],
        (err, watchers = []) => {
            if (err) {
                throw err;
            }

            const defaults = {};

            message.channel.send(outputList(headers, watchers.map(watcher => {
                const channel = guild.channels.get(watcher.channel) || defaults[guild.id] || (defaults[guild.id] = utils.getDefaultChannel(guild));
                return ({...watcher, channel: {name: 'unknown-channel', ...channel}.name});
            })), {code: 'mysql', split: 'true'});
        }
    );
};

module.exports.watcherTypes = (command, ...args) => {
    const headers = ['ID', 'Name', 'Description'];
    const message = command.message;

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
