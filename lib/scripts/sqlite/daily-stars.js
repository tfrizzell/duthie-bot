/**
 * This script calls the info.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');
const moment = require('moment');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger');
const prepare = global.prepareStatement || db.prepare.bind(db);

const stmt = {
    clearDailyStars: prepare('DELETE FROM data_daily_stars WHERE leagueId = ?'),
    insertDailyStar: prepare('REPLACE INTO data_daily_stars (leagueId, posGroup, rank, team, name, position, metadata, timestamp) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
};

db.serialize();

db.transaction(err => {
    if (err) {
        logger.error(err);
    }
});

db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN sites ON sites.id = leagues.siteId LEFT JOIN data_daily_stars ON data_daily_stars.leagueId = leagues.id WHERE watchers.typeId IN (3) AND (data_daily_stars.timestamp IS NULL OR data_daily_stars.timestamp <= ?) GROUP BY sites.id, leagues.id', [moment().startOf('day').toISOString()],
    (err, row) => {
        if (err) {
            return logger.error(err);
        }

        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
        const child = `${__dirname}/../leagues/${siteId}/daily-stars.js`;

        if (!fs.existsSync(child)) {
            return;
        }

        logger.info(`Retrieving daily stars for league ${leagueName}...`);

        child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
            .on('error', err => {
                logger.error(err);
            })
            .on('message', stars => {
                logger.info(`Processing daily stars for league ${leagueName}...`);

                stmt.clearDailyStars.run([leagueUid], err => {
                    if (err) {
                        logger.error(err);
                    }

                    for (const [group, list] of Object.entries(stars)) {
                        for (const star of list) {
                            stmt.insertDailyStar.run([
                                leagueUid,
                                group,
                                star.rank,
                                star.team,
                                star.name,
                                star.position,
                                JSON.stringify(star.metadata || {}),
                                moment().toISOString()
                            ], err => {
                                if (err) {
                                    logger.error(err);
                                }
                            });
                        }
                    }
                });
            }
        );
    }
);

require('../../node/exceptions');

require('../../node/cleanup')(() => {
    if (!db.open) {
        return;
    }

    db.commit(err => {
        if (err) {
            logger.error(err);
        }
    });

    db.finalize(stmt.insertDailyStar);
    db.finalize(stmt.clearDailyStars);

    db.close(err => {
        if (err) {
            logger.error(err);
        } else {
            logger.info('Closed connection to database');
        }
    });
});
