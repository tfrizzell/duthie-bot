/**
 * This module provides the `help` command.
 */
'use strict';

const config = global.config || require('../../../config.json');

const CommandResponse = require('./response');
const utils = require('../utils');

module.exports = (message) => new Promise(resolve => {
  const msg = message.content.trim().replace(/^`(.*?)`$/, '$1');
  const cmd = msg.split(/\s+/);

  resolve(new CommandResponse({
    channel: (cmd[2] === 'me') ? message.author : message.channel,
    options: {
      code: 'vb',
      split: 'true',
    },
    content: `Hello ${utils.tagUser(message.author, message.guild)}! Here is what you need to know about ${config.name}:

# LIST
  For more information, type: \`${config.prefix} list help\`

# PING
  Sends a ping command to make sure ${config.name} is alive and well

# UNWATCH
  For more information, type: \`${config.prefix} unwatch help\`

# WATCH
  For more information, type: \`${config.prefix} watch help\``
  }));
});
