/**
 * This module provides a wrapper around sqlite3.Database to provide added functionality.
 */
'use strict';

const sqlite3 = require('sqlite3').verbose();

const logger = require('./logger');

const dbfile = `${__dirname}/../duthie.db`;

const data = new WeakMap();

class Database extends sqlite3.Database {
    constructor(...args) {
        super(...args);
        data.set(this, []);

        this.close = this.close.bind(this);
        this.commit = this.commit.bind(this);
        this.prepare = this.prepare.bind(this);
        this.rollback = this.rollback.bind(this);
        this.transaction = this.transaction.bind(this);
    }

    close(callback) {
        const stmts = data.get(this);

        Promise.all(stmts.map(stmt => new Promise(resolve => {
            stmt.finalize(err => {
                if (err) {
                    logger.error('Error while cleaning up prepared statements:', err);
                }

                resolve();
            });
        }))).then(() => {
            super.close(callback);
        });

        return this;
    }

    commit(callback) {
        return this.run('COMMIT', function(err) {
            if (typeof callback === 'function') {
                callback.call(this, err);
            }
        });
    }

    prepare(...args) {
        const stmt = new Statement(this, ...args);
        data.set(this, [...data.get(this), stmt]);
        return stmt;
    }

    rollback(callback) {
        return this.run('ROLLBACK', function(err) {
            if (typeof callback === 'function') {
                callback.call(this, err);
            }
        });
    }

    transaction(callback) {
        return this.run('BEGIN TRANSACTION', function(err) {
            if (typeof callback === 'function') {
                callback.call(this, err);
            }
        });
    }
}

class Statement extends sqlite3.Statement {
    constructor(db, ...args) {
        super(db, ...args);
        data.set(this, db);
    }

    finalize(callback) {
        return super.finalize(function(err, ...args) {
            if (!err) {
                const db = data.get(this);
                data.set(db, data.get(db).filter(stmt => stmt !== this));
            }

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
