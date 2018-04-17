/**
 * This module provides the `list` command.
 */
'use strict';

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger');

const config = global.config || require('../../../config.json');

module.exports.help = (username = 'user') => Promise.resolve(`Hello ${username}! Here is what you need to know about \`${config.prefix} list\`:

# SYNTAX
  \`${config.prefix} list <mode> <type> <league> <team> <channel>\`

# MODE (required)
  There are currently three supported list modes:
      * leagues       - list all leagues supported by ${config.name}
      * teams         - list all teams in actively supported leagues
      * watchers      - list all watchers on your server

# TYPE (optional, mode=watchers)
  When listing the watchers on your server, you can provide any valid watcher type to filter the output on. The valid watcher types are:
      * all (default) - an alias for all other types: bidding, contract, draft, games, news, trades, waivers
      * all-news      - an alias for all other news types: bidding, contract, draft, news, trades, waivers
      * bids          - announces any winning bids that match your league and/or team filters
      * contracts     - announces any new contracts that match your league and/or team filters
      * daily-stars   - announces any of the previous day's daily stars that match your league and/or team filters
      * draft         - announces any new draft picks that match your league and/or team filters
      * games         - announces any updated game scores that match your league and/or team filters
      * news          - announces any news items that don't fit any other type, passes through the ${config.name} news filter, and that matches your league and/or team filters
      * trades        - announces any trades that match your league and/or team filters
      * waivers       - announces any players placed on or claimed off waivers that match your league and/or team filters

# LEAGUE (optional, mode=teams,watchers)
  When listing teams or watchers, you can provide any league to filter the output on. See \`${config.prefix} list leagues\` for a list of valid leagues.

  If specifying a league by name, be sure to wrap it in quotes (ex: "LGHL PSN") or remove any spaces (ex: LGHLPSN).

# TEAM (optional, mode=watchers)
  When listing watchers, you can provide any team to filter the output on. See \`${config.prefix} list teams\` for a list of valid teams.

  If specifying a team by name, be sure to wrap it in quotes (ex: "Columbus Blue Jackets") or remove any spaces (ex: ColumbusBlueJackets).

# CHANNEL (optional, mode=watchers)
  When listing watchers, you can provide any channel to filter the output on.`
);

module.exports.leagues = ({siteId = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['ID', 'Name', 'Code', 'Site'];

    db.all(`SELECT leagues.leagueId, leagues.name, leagues.codename, sites.name AS siteName FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE ? IN (sites.id, '') AND leagues.enabled = 1 AND sites.enabled = 1 ORDER BY leagues.name, sites.name`, [siteId], 
        (err, rows) => {
            if (err) {
                return reject(err);
            }
    
            const widths = rows.reduce((widths, row) => {
                return [
                    Math.max(widths[0], row.leagueId.length + 4),
                    Math.max(widths[1], row.name.length + 4),
                    Math.max(widths[2], row.codename.length + 4),
                    Math.max(widths[3], row.siteName.length + 4)
                ];
            }, headers.map(h => h.length));
            
            resolve([
                headers.map((header, index) => 
                    header.padStart(2, ' ').padEnd(widths[index], ' ').substr(0, widths[index]).toUpperCase()
                ).join('   '),
    
                headers.map((header, index) => 
                    '-'.repeat(widths[index])
                ).join('   '),
    
                rows.map(row => 
                    Object.values(row).map((value, index) => 
                        value.padStart(2, ' ').padEnd(widths[index]).substr(0, widths[index])
                    ).join('   ')
                ).join('\n')
            ].join('\n'))
        }
    );
});

module.exports.teams = ({leagueId = '', siteId = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['ID', 'Name', 'Shortname', 'League(s)', 'Site'];

    db.all(`SELECT teams.teamId, teams.name, teams.shortname, GROUP_CONCAT(DISTINCT leagues.name) AS leagueNames, GROUP_CONCAT(DISTINCT sites.name) AS siteNames FROM teams JOIN league_teams ON league_teams.teamId = teams.teamId JOIN leagues ON leagues.id = league_teams.leagueId JOIN sites ON sites.id = teams.siteId WHERE ? IN (leagues.id, '') AND ? IN (sites.id, '') AND leagues.enabled = 1 AND sites.enabled = 1 GROUP BY teams.siteId, teams.teamId ORDER BY teams.name, teams.teamId, teams.siteId`, [leagueId, siteId], 
        (err, rows) => {
            if (err) {
                return reject(err);
            }
    
            const widths = rows.reduce((widths, row) => {
                return [
                    Math.max(widths[0], row.teamId.length + 4),
                    Math.max(widths[1], row.name.length + 4),
                    Math.max(widths[2], row.shortname.length + 4),
                    Math.max(widths[3], row.leagueNames.length + 4),
                    Math.max(widths[4], row.siteNames.length + 4)
                ];
            }, headers.map(h => h.length));
    
            resolve([
                headers.map((header, index) => 
                    header.padStart(2, ' ').padEnd(widths[index], ' ').substr(0, widths[index]).toUpperCase()
                ).join('   '),
    
                headers.map((header, index) => 
                    '-'.repeat(widths[index])
                ).join('   '),
    
                rows.map(row => 
                    Object.values(row).map((value, index) => 
                        value.padStart(2, ' ').padEnd(widths[index]).substr(0, widths[index])
                    ).join('   ')
                ).join('\n')
            ].join('\n'))
        }
    );
});

module.exports.watchers = (guild, {channelId = '', leagueId = '', siteId = '', teamId = '', typeId = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['Type', 'League', 'Team', 'Channel', 'Site'];
/*
    db.all(`
        SELECT watchers.typeId, leagues.name AS leagueName, teams.name AS teamName, watchers.channelId, sites.name AS siteName
        FROM watchers 

        LEFT JOIN leagues ON leagues.leagueId = watchers.leagueId
        LEFT JOIN teams ON teams.teamId = watchers.teamId
        LEFT JOIN sites ON sites.siteId = watchers.siteId
        WHERE watcher.guildId = ? 
        AND ? IN (watcher.leagueId, '')
        AND ? IN (watcher.teamId, ''),
        AND ? IN (watcher.siteId, '')
    `, [guild, leagueId, ]
    `${('TYPE' + ' '.repeat(15)).substr(0, 15)}   ${('LEAGUE' + ' '.repeat(15)).substr(0, 15)}   ${('TEAM' + ' '.repeat(30)).substr(0, 30)}   ${('CHANNEL' + ' '.repeat(25)).substr(0, 25)}\n${'-'.repeat(15)}   ${'-'.repeat(15)}   ${'-'.repeat(30)}   ${'-'.repeat(15)}`;
    
    type = (tokens[3] || '').trim().toLowerCase();

    if (tokens[5] == 'PSN') {
        tokens[4] += tokens[5];
        [].splice.apply(tokens, [5, 1].concat(tokens.slice(6)));
    }
    
    league = getLeague(tokens[4]);

    if (tokens[4] && !league) {
        message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[4])}" is not a valid league. See ${config.prefix} ${command} help for more information.`);
        break;
    }

    team = getTeam(tokens[5], league);

    if ((type == 'games' || (tokens[5] && !tokens[5].match(/^<\#(\d+)>$/))) && !team) {
        message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[5])}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
        break;
    }

    channel = null;

    if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
        channel = message.guild.channels.get(channel[1]);

    if (channel === undefined) {
        message.channel.send(`The channel you requested could not be round in your server.`);
        break;
    }

    _message = `${('TYPE' + ' '.repeat(15)).substr(0, 15)}   ${('LEAGUE' + ' '.repeat(15)).substr(0, 15)}   ${('TEAM' + ' '.repeat(30)).substr(0, 30)}   ${('CHANNEL' + ' '.repeat(25)).substr(0, 25)}\n${'-'.repeat(15)}   ${'-'.repeat(15)}   ${'-'.repeat(30)}   ${'-'.repeat(15)}`;
    let types = [];
    
    if (type == 'all' || !type)
        types.push('bids', 'contracts', 'daily-stars', 'draft', 'games', 'news', 'trades', 'waivers');
    else if (/^all.?news$/.test(type))
        types.push('bids', 'contracts', 'draft', 'news', 'trades', 'waivers');
    else
        types.push(type);

    data.watchers.forEach(watcher => {
        if (watcher.guild != message.guild.id || (types.indexOf(watcher.type) == -1) || (league && (watcher.league != league.id)) || (team && (watcher.team != team.id)) || (channel && (watcher.channel != channel.id)))
            return;
        
        let _league = getLeague(watcher.league);
        let _team = getTeam(watcher.team, _league);
        let _channel = message.guild.channels.get(watcher.channel);
        _message += `\n${(watcher.type + ' '.repeat(15)).substr(0, 15)}   ${((_league ? _league.name : 'All') + ' '.repeat(15)).substr(0, 15)}   ${((_team ? _team.name : 'All') + ' '.repeat(30)).substr(0, 30)}   ${(_channel.name + ' '.repeat(15)).substr(0, 15)}`;
        none = false;
    });

    if (none)
        _message += `\nThere are no watchers matching your criteria`;

    message.channel.send(escapeMarkdown(_message), {code: config.help.code, split: config.help.split});*/
});
