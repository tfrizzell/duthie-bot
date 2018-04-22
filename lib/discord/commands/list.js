/**
 * This module provides the `list` command.
 */
'use strict';

const config = global.config || require('../../../config.json');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger');

module.exports.help = (username = 'user') => Promise.resolve(`Hello ${username}! Here is what you need to know about \`${config.prefix} list\`:

# SYNTAX
  \`${config.prefix} list <mode> <type> <league> <team> <channel>\`

# MODE (required)
  There are currently three supported list modes:
      * leagues       - list all leagues supported by ${config.name}
      * sites         - list all sites supported by ${config.name}
      * teams         - list all teams in the supported sites and leagues
      * watchers      - list all watchers registered for your server

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

module.exports.leagues = ({site = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['ID', 'Name', 'Code', 'Site'];

    db.all(`SELECT leagues.id, leagues.name, leagues.codename, sites.name AS siteName FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0 AND ? IN (sites.id, sites.siteId, sites.name, '') ORDER BY leagues.name, sites.name`, [site],
        (err, rows) => {
            if (err) {
                return reject(err);
            }

            const widths = rows.reduce((widths, row) => {
                return [
                    Math.max(widths[0], row.id.toString().length),
                    Math.max(widths[1], row.name.toString().length),
                    Math.max(widths[2], row.codename.toString().length),
                    Math.max(widths[3], row.siteName.toString().length)
                ];
            }, headers.map(h => h.length));

            resolve([
                headers.map((header, index) =>
                    ` ${header.padEnd(widths[index])} `.substr(0, widths[index] + 2).toUpperCase()
                ).join('   '),

                headers.map((header, index) =>
                    '-'.repeat(widths[index] + 2)
                ).join('   '),

                rows.map(row =>
                    Object.values(row).map((value = '', index) =>
                        ` ${value.toString().padEnd(widths[index])} `.substr(0, widths[index] + 2)
                    ).join('   ')
                ).join('\n')
            ].join('\n'))
        }
    );
});

module.exports.sites = () => new Promise((resolve, reject) => {
    const headers = ['ID', 'Name'];

    db.all(`SELECT id, name FROM sites ORDER BY id, name`, 
        (err, rows) => {
            if (err) {
                return reject(err);
            }

            const widths = rows.reduce((widths, row) => {
                return [
                    Math.max(widths[0], row.id.toString().length),
                    Math.max(widths[1], row.name.toString().length)
                ];
            }, headers.map(h => h.length));

            resolve([
                headers.map((header, index) =>
                    ` ${header.padEnd(widths[index])} `.substr(0, widths[index] + 2).toUpperCase()
                ).join('   '),

                headers.map((header, index) =>
                    '-'.repeat(widths[index] + 2)
                ).join('   '),

                rows.map(row =>
                    Object.values(row).map((value = '', index) =>
                        ` ${value.toString().padEnd(widths[index])} `.substr(0, widths[index] + 2)
                    ).join('   ')
                ).join('\n')
            ].join('\n'))
        }
    );
});

module.exports.teams = ({league = '', site = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['ID', 'Name', 'Shortname', 'League(s)', 'Site'];

    db.all(`SELECT teams.id, teams.name, teams.shortname, REPLACE(GROUP_CONCAT(leagues.name), ',', ', ') AS leagueNames, REPLACE(GROUP_CONCAT(DISTINCT sites.name), ',', ', ') AS siteNames FROM teams JOIN league_team_map ON league_team_map.teamId = teams.id JOIN leagues ON leagues.id = league_team_map.leagueId JOIN team_map ON team_map.teamId = teams.id JOIN sites ON sites.id = team_map.siteId WHERE ? IN (leagues.id, leagues.name, leagues.codename, '') AND ? IN (sites.id, sites.siteId, sites.name, '') AND leagues.disabled = 0 GROUP BY teams.id ORDER BY teams.name, leagues.name, sites.name`, [league, site],
        (err, rows) => {
            if (err) {
                return reject(err);
            }

            const widths = rows.reduce((widths, row) => {
                return [
                    Math.max(widths[0], row.id.toString().length + 4),
                    Math.max(widths[1], row.name.toString().length + 4),
                    Math.max(widths[2], row.shortname.toString().length + 4),
                    Math.max(widths[3], row.leagueNames.toString().length + 4),
                    Math.max(widths[4], row.siteNames.toString().length + 4)
                ];
            }, headers.map(h => h.length));

            resolve([
                headers.map((header, index) =>
                    ` ${header.padEnd(widths[index])} `.substr(0, widths[index] + 2).toUpperCase()
                ).join('   '),

                headers.map((header, index) =>
                    '-'.repeat(widths[index] + 2)
                ).join('   '),

                rows.map(row =>
                    Object.values(row).map((value, index) =>
                        ` ${value.toString().padEnd(widths[index])} `.substr(0, widths[index] + 2)
                    ).join('   ')
                ).join('\n')
            ].join('\n'))
        }
    );
});

module.exports.watchers = (guild, {channel = '', league = '', site = '', team = '', type = ''} = {}) => new Promise((resolve, reject) => {
    const headers = ['League', 'Team', 'Type', 'Channel', 'Site'];

    db.all(`SELECT leagues.name AS leagueName, IFNULL(teams.name, 'All Teams') AS teamName, watcher_types.name AS type, watchers.channelId AS channel, sites.name AS siteName FROM watchers JOIN guilds ON guilds.id = watchers.guildId AND guilds.archived IS NULL JOIN watcher_types ON watcher_types.id = watchers.typeId JOIN leagues ON leagues.id = watchers.leagueId LEFT JOIN teams ON teams.id = watchers.teamId JOIN sites ON sites.id = leagues.siteId WHERE watchers.guildId = ? AND watchers.archived IS NULL AND ? IN (watchers.channelId, '') AND  ? IN (watcher_types.id, watcher_types.name, '') AND  ? IN (leagues.id, leagues.name, leagues.codename, '') AND ? IN (teams.id, teams.name, teams.shortname, teams.codename, '') AND ? IN (sites.id, sites.siteId, sites.name, '') GROUP BY watchers.id ORDER BY leagues.id, leagues.name, teams.id, teams.name, watcher_types.name, channelId, sites.id, sites.name`, [guild.id, channel, type, league, team, site],
    (err, rows) => {
        if (err) {
            return reject(err);
        }

        const widths = rows.reduce((widths, row) => {
            row.channel = {name: 'invalid-channel', ...guild.channels.get(row.channel)}.name;

            return [
                Math.max(widths[0], row.leagueName.toString().length + 4),
                Math.max(widths[1], row.teamName.toString().length + 4),
                Math.max(widths[2], row.type.toString().length + 4),
                Math.max(widths[3], row.channel.toString().length + 4),
                Math.max(widths[4], row.siteName.toString().length + 4)
            ];
        }, headers.map(h => h.length));

        resolve([
            headers.map((header, index) =>
                ` ${header.padEnd(widths[index])} `.substr(0, widths[index] + 2).toUpperCase()
            ).join('   '),

            headers.map((header, index) =>
                '-'.repeat(widths[index] + 2)
            ).join('   '),

            rows.map(row =>
                Object.values(row).map((value, index) =>
                    ` ${value.toString().padEnd(widths[index])} `.substr(0, widths[index] + 2)
                ).join('   ')
            ).join('\n')
        ].join('\n'))
    });
});
