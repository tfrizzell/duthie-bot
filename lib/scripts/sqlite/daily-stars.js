/**
 * This script calls the info.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');
const moment = require('moment');

const db = require('../../db');
const logger = require('../../logger');

const stmt = {
    clearDailyStars: db.prepare(`DELETE FROM data_daily_stars WHERE leagueId = ?`),
    getLeagues: db.prepare(`SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM watchers JOIN leagues ON leagues.id = watchers.leagueId AND leagues.disabled = 0 JOIN sites ON sites.id = leagues.siteId LEFT JOIN data_daily_stars ON data_daily_stars.leagueId = leagues.id WHERE watchers.typeId IN (3) AND (data_daily_stars.timestamp IS NULL OR data_daily_stars.timestamp <= ?) GROUP BY sites.id, leagues.id`),
    insertDailyStar: db.prepare(`REPLACE INTO data_daily_stars (leagueId, posGroup, rank, team, name, position, metadata, timestamp) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`)
};

const MAX_CHILDREN = {maxChildren: 'Infinity', ...require('../../../config.json').scripts}.maxChildren;
let leagues = [];

const fetchDailyStars = (league = {}) => {
    const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = league;
    const script = `${__dirname}/../leagues/${siteId}/daily-stars.js`;

    if (!siteId || !fs.existsSync(script)) {
        if (leagues.length > 0) {
            fetchDailyStars(leagues.shift());
        }

        return;
    }
    
    logger.info(`Retrieving daily stars for league ${leagueName}...`);

    child_process.fork(script, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
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
        })
        .on('exit', () => {
            fetchDailyStars(leagues.shift());
        });
};

stmt.getLeagues.all([moment().startOf('day').toISOString()], (err, rows = []) => {
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
                fetchDailyStars(league);
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
