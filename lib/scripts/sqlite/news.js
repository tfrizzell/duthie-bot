/**
 * This script calls the news.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');
const moment = require('moment');

const db = require('../../db');
const logger = require('../../logger');

const stmt = {
    deleteLeagueNews: db.prepare(`DELETE FROM data_news WHERE leagueId = ?`),
    getLastNewsItem: db.prepare(`SELECT newsId FROM data_news WHERE leagueId = ? ORDER BY timestamp DESC, id DESC LIMIT 1`),
    getLeagues: db.prepare(`SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0`),
    insertNewsItem: db.prepare(`INSERT INTO data_news (leagueId, newsId, message, type, timestamp) VALUES (?, ?, ?, ?, ?)`),
    insertNewsTeamMap: db.prepare(`INSERT INTO data_news_team_map (newsId, siteId, mappedTeamId) VALUES (?, ?, ?)`)
};

const MAX_CHILDREN = {maxChildren: 'Infinity', ...require('../../../config.json').scripts}.maxChildren;
let leagues = [];

const fetchNews = (league = {}) => {
    const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = league;
    const script = `${__dirname}/../leagues/${siteId}/news.js`;

    if (!siteId || !fs.existsSync(script)) {
        if (leagues.length > 0) {
            fetchNews(leagues.shift());
        }

        return;
    }

    logger.info(`Retrieving news for league ${leagueName}...`);

    child_process.fork(script, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
        .on('error', err => {
            logger.error(err);
        })
        .on('message', items => {
            logger.info(`Processing ${items.length} news items for league ${leagueName}...`);

            if (items.length > 0) {
                stmt.getLastNewsItem.get([leagueUid], (err, {newsId: lastNewsId} = {}) => {
                    let skip = false;

                    Promise.all(items.map(item => {
                        if (item.id === lastNewsId) {
                            skip = true;
                        }

                        if (skip) {
                            return Promise.resolve([]);
                        }

                        return new Promise(resolve => {
                            stmt.insertNewsItem.run([
                                leagueUid,
                                item.id,
                                item.message,
                                item.type || 'news',
                                moment(item.timestamp).toISOString()
                            ], function(err) {
                                if (err) {
                                    if (!/UNIQUE constraint failed: data_news\.leagueId, data_news\.newsId/i.test(err)) {
                                        logger.error(err);
                                    }

                                    return resolve([]);
                                }

                                const newsId = this.lastID;
                                resolve(item.teams.map(team => [newsId, team]));
                            });
                        });
                    })).then(teamMap => {
                        teamMap = teamMap.reduce((teamMap, itemMap) => [...teamMap, ...itemMap], []);

                        for (const [newsId, teamId] of teamMap) {
                            stmt.insertNewsTeamMap.run([newsId, siteUid, teamId], err => {
                                if (err && !/UNIQUE constraint failed: data_news_team_map\.newsId, data_news_team_map\.siteId, data_news_team_map\.mappedTeamId/i.test(err)) {
                                    logger.error(err);
                                }
                            });
                        }
                    });
                });
            } else {
                stmt.deleteLeagueNews.run([leagueUid], err => {
                    if (err) {
                        logger.error(err);
                    }
                });
            }
        })
        .on('exit', () => {
            fetchNews(leagues.shift());
        });
};

db.serialize(() => {
    db.transaction(err => {
        if (err) {
            return logger.error(err);
        }

        stmt.getLeagues.all((err, rows = []) => {
            if (err) {
                return logger.error(err);
            }

            leagues = rows;

            for (const league of leagues.splice(0, MAX_CHILDREN)) {
                fetchNews(league);
            }
        });
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
