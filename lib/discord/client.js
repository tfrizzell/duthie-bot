/**
 * This module provides a single point of reference for constructing a Discord Client.
 */
'use strict';

const Discord = require('discord.js');

module.exports.create = () => {
    module.exports = new Discord.Client();
    module.exports.new = () => new Discord.Client();
    return module.exports;
};

module.exports.new = () => new Discord.Client();
