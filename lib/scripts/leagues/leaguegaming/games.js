/**
 * This script retrieves game data from leaguegaming.com for the given
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

const logger = global.logger || require('../../../logger');
const API = require('../../../api/leaguegaming').new();

API.getGames({leagueId, seasonId}).then(games => {
    if (!process.env.CHILD) {
        console.log(games);
        process.exit();
    } else {
        process.send(games, () => process.exit());
    }
}).catch(err => {
    logger.error(`[leagueId=${leagueId}, seasonId=${seasonId}]`, err);
    process.exit(1);
});
