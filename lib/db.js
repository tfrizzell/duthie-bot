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
        return this.run('COMMIT', function (err, ...args) {
            if (err && /no transaction is active/i.test(err)) {
                err = undefined;
            }

            if (typeof callback === 'function') {
                callback.call(this, err, ...args);
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

    rollback(callback) {
        return this.run('ROLLBACK', function (err, ...args) {
            if (err && /no transaction is active/i.test(err)) {
                err = undefined;
            }

            if (typeof callback === 'function') {
                callback.call(this, err, ...args);
            }
        });
    }

    transaction(callback) {
        return this.run('BEGIN TRANSACTION', function (err, ...args) {
            if (typeof callback === 'function') {
                callback.call(this, err, ...args);
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
