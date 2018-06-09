/**
 * This script calls the info.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');

const db = require('../../db');
const logger = require('../../logger');

const stmt = {
    getLeagues: db.prepare(`SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0`),
    updateLeague: db.prepare(`UPDATE leagues SET name = ?, codename = ?, extraData = ? WHERE id = ?`)
};

const MAX_CHILDREN = {maxChildren: 'Infinity', ...require('../../../config.json').scripts}.maxChildren;
let leagues = [];

const fetchInfo = (league = {}) => {
    const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = league;
    const script = `${__dirname}/../leagues/${siteId}/info.js`;

    if (!siteId || !fs.existsSync(script)) {
        if (leagues.length > 0) {
            fetchInfo(leagues.shift());
        }

        return;
    }

    logger.info(`Retrieving info for league ${leagueName}...`);

    child_process.fork(script, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
        .on('error', err => {
            logger.error(err);
        })
        .on('message', info => {
            logger.info(`Processing info for league ${leagueName}...`);

            if (!info.id || !info.name || !info.codename) {
                return;
            }

            stmt.updateLeague.run([
                info.name || leagueName, 
                info.codename, 
                JSON.stringify({...info, codename: undefined, id: undefined, name: undefined}), 
                leagueUid
            ], err => {
                if (err) {
                    logger.error(err);
                }
            });
        })
        .on('exit', () => {
            fetchInfo(leagues.shift());
        });
};

stmt.getLeagues.all((err, rows = []) => {
    if (err) {
        return logger.error(err);
    }

    db.transaction(err => {
        if (err) {
            return logger.error(err);
        }

        db.serialize(() => {
            leagues = rows;

            for (const league of leagues.splice(0, MAX_CHILDREN)) {
                fetchInfo(league);
            }
        });
    });
});

require('../../node/exceptions');

require('../../node/cleanup')(() => {
    if (!db.open) {
        return;
    }

    db.commit(() => {
        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
});
