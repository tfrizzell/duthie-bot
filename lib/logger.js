/**
 * This module provides a logging singleton with an option to spawn
 * additional loggers, and an assortment of log level constants.
 */
'use strict';

const config = global.config || require('../config.json');
const moment = require('moment');

const checkLevel = (logger, level) => logger.level >= level;

const LOG = {
    ERROR: 1,
    WARN: 2,
    INFO: 3,
    DEBUG: 4,
    VERBOSE: 5
};

class Logger {
    constructor({
        dateFormat = '\\[YYYY-MM-DD HH:mm:ss\\]',
        level = LOG.INFO
    } = {}) {
        if (isNaN(level)) {
            level = LOG[level] || LOG[level.replace(/^LOG_/, '')];
        }

        if (isNaN(level) && level < LOG.ERROR && level > LOG.VERBOSE) {
            level = LOG.INFO
        }

        this.dateFormat = dateFormat;
        this.level = level;
    }

    clear() {
        console.clear();
    }

    debug(...content) {
        if (checkLevel(this, LOG.DEBUG)) {
            if (this.dateFormat) {
                console.log(moment().format(this.dateFormat), '[DEBUG]', ...content);
            } else {
                console.log('[DEBUG]', ...content);
            }
        }

        return this;
    }

    error(...content) {
        if (checkLevel(this, LOG.ERROR)) {
            if (this.dateFormat) {
                console.error(moment().format(this.dateFormat), '[ERROR]', ...content);
            } else {
                console.error('[ERROR]', ...content);
            }
        }

        return this;
    }

    info(...content) {
        if (checkLevel(this, LOG.INFO)) {
            if (this.dateFormat) {
                console.info(moment().format(this.dateFormat), '[INFO]', ...content);
            } else {
                console.info('[INFO]', ...content);
            }
        }

        return this;
    }

    log(...content) {
        return this.debug(...content);
    }

    new(options = {}) {
        return new Logger(options);
    }

    trace() {
        console.trace();
        return this;
    }

    warn(...content) {
        if (checkLevel(this, LOG.WARN)) {
            if (this.dateFormat) {
                console.warn(moment().format(this.dateFormat), '[WARN]', ...content);
            } else {
                console.warn('[WARN]', ...content);
            }
        }

        return this;
    }

    verbose(...content) {
        if (checkLevel(this, LOG.VERBOSE)) {
            if (this.dateFormat) {
                console.log(moment().format(this.dateFormat), '[VERBOSE]', ...content);
            } else {
                console.log('[VERBOSE]', ...content);
            }
        }

        return this;
    }
};

module.exports = new Logger(config.logger);
module.exports.new = (options = config.logger) => new Logger(options);
