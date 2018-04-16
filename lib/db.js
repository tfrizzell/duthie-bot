'use strict';
require('./global');

const sqlite3 = require('sqlite3').verbose();
const logger = global.logger || require(`${__libdir}/logger`);

module.exports = new sqlite3.Database(__dbfile, err => {
    if (err) {
        logger.error(err);
    } else {
        logger.log('Opened connection to database');
    }
});

module.exports.new = () => new sqlite3.Database(__dbfile, err => {
    if (err) {
        logger.error(err);
    }
});