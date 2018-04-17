/**
 * This script retrieves news data from leaguegaming.com for the given
 * leagueId and seasonId.
 */
'use strict';

const [leagueId, seasonId] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId, args.seasonId];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require('../../../api/leaguegaming').new();
const logger = global.logger || require('../../../logger').new({level: process.env.CHILD ? 'LOG_WARN' : 'LOG_DEBUG'});

API.getNews({leagueId, seasonId}).then(news => {
    if (!process.env.CHILD) {
        console.log(news);
        process.exit();
    } else {
        process.send(news, () => process.exit());
    }
}).catch(err => {
    logger.error(`[leagueId=${leagueId}, seasonId=${seasonId}]`, err);
    process.exit(1);
});
