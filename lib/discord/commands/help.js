/**
 * This module provides the `help` command.
 */
'use strict';

const utils = require('../utils');

const config = global.config || require('../../../config.json');

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
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about ${config.name}:

# LIST
  For more information, type: \`${config.prefix} list help\`

# PING
  Sends a ping command to make sure ${config.name} is alive and well

# UNWATCH
  For more information, type: \`${config.prefix} unwatch help\`

# WATCH
  For more information, type: \`${config.prefix} watch help\``;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, {code: 'vb', split: 'true'});
    } else {
        message.channel.send(response, {code: 'vb', split: 'true'});
    }
};
