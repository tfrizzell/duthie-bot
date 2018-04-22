/**
 * This module provides a wrapper around sqlite3.Database to provide built-in transactions.
 */
'use strict';

const sqlite3 = require('sqlite3').verbose();

const logger = global.logger || require('./logger');

const dbfile = `${__dirname}/../duthie.db`;

class Database extends sqlite3.Database {
    constructor(...args) {
        super(...args);

        this.commit = this.commit.bind(this);
        this.finalize = this.finalize.bind(this);
        this.transaction = this.transaction.bind(this);
    }

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

    finalize(stmt) {
        try {
            stmt.finalize();
        } catch (err) {
            if (!/Statement is already finalized/i.test(err)) {
                throw err;
            }
        }
    }

    transaction(callback) {
        return this.run('BEGIN TRANSACTION', (...args) => {
            if (typeof callback === 'function') {
                callback(...args);
            }
        });
    }
}

module.exports = new Database(dbfile, function(err) {
    if (err) {
        logger.error(err);
    } else {
        logger.info('Opened connection to database');
    }

    this.run('PRAGMA foreign_keys=on', err => {
        if (err) {
            logger.error(err);
        }
    });
});

module.exports.new = () => new Database(dbfile, function(err) {
    if (err) {
        logger.error(err);
    } else {
        logger.info('Opened connection to database');
    }

    this.run('PRAGMA foreign_keys=on', err => {
        if (err) {
            logger.error(err);
        }
    });
});
