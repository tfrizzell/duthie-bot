/**
 * This module rolls up all Discord modules into a single export.
 */
'use strict';

module.exports = (client) => {
    require('./events')(client);
    require('./messages')(client);
};
