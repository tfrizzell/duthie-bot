/**
 * This module binds uncaughtException and unhandleRejection listeners to the node.js process.
 */
'use strict';

const logger = global.logger || require('../logger');

process.on('uncaughtException', err => {
    logger.error('UncaughtException:', err);
});

process.on('unhandledRejection', err => {
    logger.error('UnhanledRejection:', err);
});