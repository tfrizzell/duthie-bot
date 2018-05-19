/**
 * This script retrieves league data from leaguegaming.com for the given
 * leagueId.
 */
'use strict';

const [leagueId] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require('../../../api/leaguegaming').new();
const logger = require('../../../logger');

API.getInfo({leagueId}).then(info => {
    if (!process.env.CHILD) {
        console.log(info);
        process.exit();
    } else {
        process.send(info, () => process.exit());
    }
}).catch(err => {
    logger.error(`[leagueId=${leagueId}]`, (err instanceof Error) ? err.toString() : err);
    process.exit(1);
});
