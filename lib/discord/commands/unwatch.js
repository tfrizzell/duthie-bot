/**
 * This module provides the `watch` command.
 */
'use strict';

const utils = require('../utils');
const watch = require('./watch');

const config = global.config || require('../../../config.json');

module.exports = command => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](command, ...command.arguments.slice(1));
    } else {
        return watch.unregister(command, ...command.arguments);
    }
};

module.exports.help = (command, ...args) => {
    const message = command.message;
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} unwatch\`:

# SYNTAX
    ${config.prefix} unwatch[ type=<type>][ league=<league>][ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# TYPE (optional)
    When unregistering a watcher, you may specify the type of watcher you wish to filter your watcher removal on. If excluded, watchers of all types may be removed. See \`${config.prefix} list watcher-types\` for a list of valid types.

# LEAGUE (optional)
    When unregistering a watcher, you may specify the league you wish to filter your watcher removal on. If excluded, watchers for all leagues may be removed. See \`${config.prefix} list leagues\` for a list of valid leagues.

# TEAM (optional)
    When unregistering a watcher, you may specify the team you wish to filter your watcher removal on. If excluded, watchers for all teams may be removed. See \`${config.prefix} list teams\` for a list of valid teams.

# CHANNEL (optional)
    When unregistering a watcher, you may specify the channel you wish to filter your watcher removal on. If excluded, watchers for all channels may be removed.`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, {code: 'vb', split: 'true'});
    } else {
        message.channel.send(response, {code: 'vb', split: 'true'});
    }
};
