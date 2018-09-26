/**
 * This script retrieves team data from thespnhl.com for the current
 * season.
 */
'use strict';

const API = require('../../../api/spnhl').new();
const logger = require('../../../logger');

API.getTeams().then(teams => {
    if (!process.env.CHILD) {
        console.log(teams);
        process.exit();
    } else {
        process.send(teams, () => process.exit());
    }
}).catch(err => {
    logger.error((err instanceof Error) ? err.toString() : err);
    process.exit(1);
});
