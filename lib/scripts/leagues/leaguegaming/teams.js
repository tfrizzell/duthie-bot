/**
 * This script retrieves team data from leaguegaming.com for the given
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

API.getTeams({leagueId, seasonId}).then(teams => {
    if (!process.env.CHILD) {
        console.log(teams);
        process.exit();
    } else {
        process.send(teams, () => process.exit());
    }
}).catch(err => {
    logger.error(`[leagueId=${leagueId}, seasonId=${seasonId}]`, err);
    process.exit(1);
});
