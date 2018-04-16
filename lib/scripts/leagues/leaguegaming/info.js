'use strict';
require('../../../global');

const [leagueId] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require(`${__libdir}/api/leaguegaming`).new();
const logger = global.logger || require(`${__libdir}/logger`);

API.getInfo({leagueId}).then(info => {
    if (!process.env.CHILD) {
        console.log(info);
        process.exit();
    } else {
        process.send(info, () => process.exit());
    }
}).catch(ex => {
    logger.error(`[leagueId=${leagueId}]`, ex);
    process.exit(1);
});
