/**
 * This script retrieves team data from myvirtualgaming.com for the given
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

const API = require('../../../api/myvirtualgaming').new({subdomain});
const logger = require('../../../logger');

API.getTeams({leagueId, seasonId, scheduleId}).then(teams => {
    if (!process.env.CHILD) {
        console.log(teams);
        process.exit();
    } else {
        process.send(teams, () => process.exit());
    }
}).catch(err => {
    logger.error(`[subdomain=${API.subdomain}, leagueId=${leagueId}]`, (err instanceof Error) ? err.toString() : err);
    process.exit(1);
});
