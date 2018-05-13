/**
 * This module provides the `watch` command.
 */
'use strict';

const utils = require('../utils');

const watch = require('./watch');

const config = global.config || require('../../../config.json');

module.exports = (message, command) => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](message, command, ...command.arguments.slice(1));
    } else if (command.subcommand === 'help') {
        return require('./help').unwatch(message, command, ...command.tokens.slice(1));
    } else {
        return watch.unregister(message, command, ...command.arguments);
    }
};
