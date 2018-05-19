/**
 * This script retrieves daily stars from leaguegaming.com for the given
 * leagueId.
 */
'use strict';

const [forumId, date] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.forumId, process.argv[3]];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require('../../../api/leaguegaming').new();
const logger = require('../../../logger');

API.getDailyStars({forumId, date}).then(stars => {
    if (!process.env.CHILD) {
        console.log(stars);
        process.exit();
    } else {
        process.send(stars, () => process.exit());
    }
}).catch(err => {
    logger.error(`[forumId=${forumId}, date=${date || ''}]`, (err instanceof Error) ? err.toString() : err);
    process.exit(1);
});
