'use strict';
require('../../../global');

const [leagueId, subdomain] = (() => {
    if (process.env.CHILD) {
        const args = JSON.parse(process.argv[2]);
        return [args.leagueId, args.subdomain];
    } else {
        return process.argv.slice(2);
    }
})();

const API = require(`${__libdir}/api/myvirtualgaming`).new({subdomain});
const logger = global.logger || require(`${__libdir}/logger`);

API.getSchedule({leagueId}).then(games => {
    if (!process.env.CHILD) {
        console.log(games);
        process.exit();
    } else {
        process.send(games, () => process.exit());
    }
}).catch(ex => {
    logger.error(`[subdomain=${API.subdomain}, leagueId=${leagueId}]`, ex);
    process.exit(1);
});
