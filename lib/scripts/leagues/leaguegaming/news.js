'use strict';
require('../../../global');

const [leagueId, seasonId] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId, args.seasonId];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require(`${__libdir}/api/leaguegaming`).new();
const logger = global.logger || require(`${__libdir}/logger`);

API.getNews({leagueId, seasonId}).then(news => {
    if (!process.env.CHILD) {
        console.log(news);
        process.exit();
    } else {
        process.send(news, () => process.exit());
    }
}).catch(ex => {
    logger.error(`[leagueId=${leagueId}, seasonId=${seasonId}]`, ex);
    process.exit(1);
});
