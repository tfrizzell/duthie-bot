/**
 * This script retrieves game data from thespnhl.com for the current
 * season.
 */
'use strict';

const API = require('../../../api/spnhl').new();
const logger = require('../../../logger');

API.getGames().then(games => {
    if (!process.env.CHILD) {
        console.log(games);
        process.exit();
    } else {
        process.send(games, () => process.exit());
    }
}).catch(err => {
    logger.error((err instanceof Error) ? err.toString() : err);
    process.exit(1);
});
