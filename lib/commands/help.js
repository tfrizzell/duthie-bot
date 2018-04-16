'use strict';
require('../../global');

const Discord = require('discord.js');
const config = global.conf || require(__configfile);

module.exports = (username = 'user') => Promise.resolve(`Hello ${username}! Here is what you need to know about ${config.name}:

# LIST
  For more information, type: \`${config.prefix} list help\`

# PING
  Sends a ping command to make sure ${config.name} is alive and well

# UNWATCH
  For more information, type: \`${config.prefix} unwatch help\`

# WATCH
  For more information, type: \`${config.prefix} watch help\``
);
