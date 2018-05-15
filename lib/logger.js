/**
 * This module provides a logging singleton with an option to spawn
 * additional loggers, and an assortment of log level constants.
 */
'use strict';

const moment = require('moment');

const config = require('../config.json');

const LOG = {
    NONE: 'Infinity',
    ERROR: 5,
    WARN: 4,
    INFO: 3,
    DEBUG: 2,
    VERBOSE: 1
};

const DEFAULT_LOG_LEVEL = process.env.CHILD ? LOG.WARN : LOG.INFO;

const checkLogLevel = (logger, level) => logger.options.level <= level;

const getLogLevel = level => LOG[level.toUpperCase().replace(/^LOG[_.]/, '')] || DEFAULT_LOG_LEVEL;

class Logger {
    constructor({
        dateFormat = '\\[YYYY-MM-DD HH:mm:ss\\]',
        level = DEFAULT_LOG_LEVEL
    } = {}) {
        if (isNaN(level) || level < LOG.VERBOSE || (level > LOG.ERROR && level !== LOG.NONE)) {
            level = getLogLevel(level);
        }

        this.options = {
            dateFormat: dateFormat,
            level: level
        };

        this.clear = this.clear.bind(this);
        this.debug = this.debug.bind(this);
        this.error = this.error.bind(this);
        this.info = this.info.bind(this);
        this.log = this.log.bind(this);
        this.new = this.new.bind(this);
        this.trace = this.trace.bind(this);
        this.warn = this.warn.bind(this);
        this.verbose = this.verbose.bind(this);
    }

    clear() {
        console.clear();
    }

    debug(...content) {
        if (checkLogLevel(this, LOG.DEBUG)) {
            if (this.options.dateFormat) {
                console.log(moment().format(this.options.dateFormat), '[DEBUG]', ...content);
            } else {
                console.log('[DEBUG]', ...content);
            }
        }

        return this;
    }

    error(...content) {
        if (checkLogLevel(this, LOG.ERROR)) {
            if (this.options.dateFormat) {
                console.error(moment().format(this.options.dateFormat), '[ERROR]', ...content);
            } else {
                console.error('[ERROR]', ...content);
            }
        }

        return this;
    }

    info(...content) {
        if (checkLogLevel(this, LOG.INFO)) {
            if (this.options.dateFormat) {
                console.info(moment().format(this.options.dateFormat), '[INFO]', ...content);
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
        if (checkLogLevel(this, LOG.WARN)) {
            if (this.options.dateFormat) {
                console.warn(moment().format(this.options.dateFormat), '[WARN]', ...content);
            } else {
                console.warn('[WARN]', ...content);
            }
        }

        return this;
    }

    verbose(...content) {
        if (checkLogLevel(this, LOG.VERBOSE)) {
            if (this.options.dateFormat) {
                console.log(moment().format(this.options.dateFormat), '[VERBOSE]', ...content);
            } else {
                console.log('[VERBOSE]', ...content);
            }
        }

        return this;
    }
}

for (const [key, value] of Object.entries(LOG)) {
    Logger.prototype[`LOG_${key}`] = value;
}

module.exports = new Logger(process.env.CHILD ? {...config.scripts}.logger : config.logger);
module.exports.new = (options = config.logger) => new Logger(options);
