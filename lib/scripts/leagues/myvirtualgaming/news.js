/**
 * This script retrieves news data from myvirtualgaming.com for the given
 * leagueId and optional subdomain.
 */
'use strict';

const [leagueId, seasonId, scheduleId, subdomain] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId, args.seasonId, args.scheduleId, args.subdomain];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require('../../../api/myvirtualgaming').new();
const logger = require('../../../logger');

API.getNews({leagueId, seasonId, scheduleId}).then(news => {
    if (!process.env.CHILD) {
        console.log(news);
        process.exit();
    } else {
        process.send(news, () => process.exit());
    }
}).catch(err => {
    logger.error(`[leagueId=${leagueId}, seasonId=${seasonId}]`, (err instanceof Error) ? err.toString() : err);
    process.exit(1);
});