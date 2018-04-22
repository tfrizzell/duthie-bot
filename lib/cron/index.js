/**
 * This module rolls up cron functional for quick import into duthie-bot.js.
 */
'use strict';

module.exports = (...args) => {
    const runner = require('./runner');
    require('./scheduler')(runner);
};
