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

db.serialize(() => {;
    db.transaction(err => {
        if (err) {
            logger.error(err);
        }
    });

    stmt.getLeagues.each((err, row) => {
        if (err) {
            return logger.error(err);
        }

        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
        const child = `${__dirname}/../leagues/${siteId}/info.js`;

        if (!fs.existsSync(child)) {
            return;
        }

        logger.info(`Retrieving info for league ${leagueName}...`);

        child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
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
            }
        );
    });
});

require('../../node/exceptions');

require('../../node/cleanup')(() => {
    if (!db.open) {
        return;
    }

    db.commit(err => {
        if (err) {
            logger.error(err);
        }

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
});
