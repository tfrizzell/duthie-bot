'use strict';

const moment = require('moment');

class Logger {
    constructor({dateFormat = '\\[YYYY-MM-DD HH:mm:ss\\]'} = {}) {
        this.dateFormat = dateFormat;
    }

    clear() {
        console.clear();
    }

    debug(...content) {
        return this.log(...content);
    }

    error(...content) {
        if (this.dateFormat) {
            console.error(moment().format(this.dateFormat), ...content);
        } else {
            console.error(...content);
        }

        return this;
    }

    info(...content) {
        if (this.dateFormat) {
            console.info(moment().format(this.dateFormat), ...content);
        } else {
            console.info(...content);
        }

        return this;
    }

    log(...content) {
        if (this.dateFormat) {
            console.log(moment().format(this.dateFormat), ...content);
        } else {
            console.log(...content);
        }

        return this;
    }

    trace() {
        console.trace();
        return this;
    }

    warn(...content) {
        if (this.dateFormat) {
            console.warn(moment().format(this.dateFormat), ...content);
        } else {
            console.warn(...content);
        }

        return this;
    }
};

module.exports = new Logger();
module.exports.new = (options = {}) => new Logger(options);
