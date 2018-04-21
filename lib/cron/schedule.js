/**
 * This module contains the cron schedule.
 */
'use strict';

const cron = require('cron');

const offsetSteps = (steps = 0, offset = 0, end = 59, start = 0) => {
    if (steps === 0) {
        return '*';
    }

    if (steps === 1) {
        return offset;
    }

    const size = (end - start + 1) / steps;
    return new Array(steps).fill(offset).map((v,i) => v + (i * size)).join(',');
};

module.exports = (runner, {timezone = 'America/New_York'} = {}) => ([
    // cron: 0 */30+2 0-19 * * *
    cron.job(`0 ${offsetSteps(2, 2)} 0-19 * * *`,     runner.updateGames,   undefined, true, timezone, runner),

    // cron: 0 */5+2 20-23 * * 0-4
    cron.job(`0 ${offsetSteps(12, 2)} 20-23 * * 0-4`, runner.updateGames,   undefined, true, timezone, runner),

    // cron: 0 */30+2 20-23 * * 5-6
    cron.job(`0 ${offsetSteps(12, 2)} 20-23 * * 5-6`, runner.updateGames,   undefined, true, timezone, runner),

    // cron: 0 17 4 * * *
    cron.job(`0 17 4 * * *`,                          runner.updateLeagues, undefined, true, timezone, runner),

    // cron: 0 */15 * * * *
    cron.job(`0 ${offsetSteps(4)} * * * *`,           runner.updateNews,    undefined, true, timezone, runner),

    // cron: 0 */5+4 14-15 * * *
    cron.job(`0 ${offsetSteps(12, 4)} 14-15 * * 1-5`, runner.updateStars,   undefined, true, timezone, runner),   

    // cron: 0 47 4 * * *
    cron.job(`0 47 4 * * *`,                          runner.updateTeams,   undefined, true, timezone, runner),
]);
