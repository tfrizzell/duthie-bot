/**
 * This module provides a wrapper around sqlite3.Database to provide built-in transactions.
 */
'use strict';

const dbfile = `${__dirname}/../duthie-bot.db`;
const logger = global.logger || require('./logger');
const sqlite3 = require('sqlite3').verbose();

class Database extends sqlite3.Database {
    commit(callback) {
        if (!this.transaction) {
            return this;
        }

        this.run('COMMIT', (err, ...args) => {
            if (!/cannot commit - no transaction is active/i.test(err.Error)) {
                err = undefined;
            }

            this.transaction = !!err;

            if (typeof callback === 'function') {
                callback(err, ...args);
            }
        });

        return this;
    }

    transactional(callback) {
        if (this.transaction) {
            return this;
        }

        this.run('BEGIN TRANSACTION', (err, ...args) => {
            this.transaction = !err;

            if (typeof callback === 'function') {
                callback(err, ...args);
            }
        });

        return this;
    }
}

module.exports = new Database(dbfile, err => {
    if (err) {
        logger.error(err);
    } else {
        logger.info('Opened connection to database');
    }
});

module.exports.new = () => new Database(dbfile, err => {
    if (err) {
        logger.error(err);
    } else {
        logger.debug('Opened new connection to database');
    }
});
