/**
 * This module provides the `help` command.
 */
'use strict';

const logger = require('../../logger');
const utils = require('../utils');

const config = require('../../../config.json');

const options = {code: 'vb', split: true};

module.exports = (message, command) => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](message, command, ...command.arguments);
    } else {
        return module.exports.help(message, command, ...command.arguments);
    }
};

module.exports.admin = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested help: ${JSON.stringify(args)}`);
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} admin\`:

# SYNTAX
    ${config.prefix} admin [add | remove] [user]`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, options);
    } else {
        message.channel.send(response, options);
    }
};

module.exports.help = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested help: ${JSON.stringify(args)}`);
    const [target = ''] = command.arguments;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about ${config.name}:

# ADMIN
  For more information, type: \`${config.prefix} help admin\`

# LIST
  For more information, type: \`${config.prefix} help list\`

# PING
  Sends a ping command to make sure ${config.name} is alive and well

# UNWATCH
  For more information, type: \`${config.prefix} help unwatch\`

# WATCH
  For more information, type: \`${config.prefix} help watch\``;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, options);
    } else {
        message.channel.send(response, options);
    }
};

module.exports.list = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested help: ${JSON.stringify(args)}`);
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} list\`:

# SYNTAX
    ${config.prefix} list <dataset>[ <parameters>]
    ${config.prefix} list admins
    ${config.prefix} list leagues[ site=<site>]
    ${config.prefix} list sites
    ${config.prefix} list teams[ site=<site>][ league=<league>]
    ${config.prefix} list watcher-types
    ${config.prefix} list watchers[ type=<type>][ site=<site>][ league=<league>][ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# DATASET (required)
    Below are the datasets currently supported by \`${config.prefix} list\`:
        * admins        - list all ${config.name} administrators for your server
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
        message.author.send(response, options);
    } else {
        message.channel.send(response, options);
    }
};

module.exports.unwatch = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested help: ${JSON.stringify(args)}`);
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} unwatch\`:

# SYNTAX
    ${config.prefix} unwatch[ type=<type>][ league=<league>][ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# TYPE (optional)
    When unregistering a watcher, you may specify the type of watcher you wish to filter your watcher removal on. If excluded, watchers of all types will be removed. See \`${config.prefix} list watcher-types\` for a list of valid types.

# LEAGUE (optional)
    When unregistering a watcher, you may specify the league you wish to filter your watcher removal on. If excluded, watchers for all leagues will be removed. See \`${config.prefix} list leagues\` for a list of valid leagues.

# TEAM (optional)
    When unregistering a watcher, you may specify the team you wish to filter your watcher removal on. If excluded, only watchers with no teams associated will be removed. See \`${config.prefix} list teams\` for a list of valid teams.

    To remove watchers for all teams, use \`team=*\`.

# CHANNEL (optional)
    When unregistering a watcher, you may specify the channel you wish to filter your watcher removal on. If excluded, watchers for all channels will be removed.`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, options);
    } else {
        message.channel.send(response, options);
    }
};

module.exports.watch = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} has requested help: ${JSON.stringify(args)}`);
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} watch\`:

# SYNTAX
    ${config.prefix} watch type=<type> league=<league>[ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# TYPE (required)
    When registering a watcher, you must specify the type of watcher being registered. See \`${config.prefix} list watcher-types\` for a list of valid types.

# LEAGUE (required)
    When registering a watcher, you must specify the league you wish to watch. See \`${config.prefix} list leagues\` for a list of valid leagues.

# TEAM (optional)
    When registering a watcher, you may specify the team you wish to watch. See \`${config.prefix} list teams\` for a list of valid teams.

# CHANNEL (optional)
    When registering a watcher, you may specify the channel you wish to have messages sent to. If the channel doesn't exist or ${config.name} can't access it, a default channel will be used.`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, options);
    } else {
        message.channel.send(response, options);
    }
};
