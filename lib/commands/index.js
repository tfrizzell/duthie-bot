'use strict';
require('../../global');

module.exports.help = require('./help');
module.exports.list = require('./list');
module.exports.ping = () => 'PONG';
