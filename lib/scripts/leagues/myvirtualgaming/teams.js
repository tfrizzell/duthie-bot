/**
 * This script retrieves team data from myvirtualgaming.com for the given
 * leagueId and optional subdomain.
 */
'use strict';

const [leagueId, subdomain] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId, args.subdomain];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require('../../../api/myvirtualgaming').new({subdomain});
const logger = global.logger || require('../../../logger').new({level: process.env.CHILD ? 'LOG_WARN' : 'LOG_DEBUG'});

API.getTeams({leagueId}).then(teams => {
    if (!process.env.CHILD) {
        console.log(teams);
        process.exit();
    } else {
        process.send(teams, () => process.exit());
    }
}).catch(err => {
    logger.error(`[subdomain=${API.subdomain}, leagueId=${leagueId}]`, err);
    process.exit(1);
});
