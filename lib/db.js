/**
 * This module provides a wrapper around sqlite3.Database to provide built-in transactions.
 */
'use strict';

const dbfile = `${__dirname}/../duthie-bot.db`;
const logger = global.logger || require('./logger');
const sqlite3 = require('sqlite3').verbose();

class Database extends sqlite3.Database {
    commit(callback) {
        return this.run('COMMIT', (err, ...args) => {
            if (err && /cannot commit - no transaction is active/i.test(err)) {
                err = undefined;
            }

            if (typeof callback === 'function') {
                callback(err, ...args);
            }
        });
    }

    transaction(callback) {
        return this.run('BEGIN TRANSACTION', (...args) => {
            if (typeof callback === 'function') {
                callback(...args);
            }
        });
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
