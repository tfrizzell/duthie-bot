/**
 * This script calls the news.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require('../../db');
const logger = global.logger || require('../../logger').new({level: process.env.CHILD ? 'LOG_WARN' : 'LOG_DEBUG'});

const stmt = {
    deleteLeagueNews: db.prepare('DELETE FROM data_news WHERE leagueId = ?'),
    insertNewsItem: db.prepare('INSERT OR IGNORE INTO data_news (teams, message, timestamp, leagueId, newsId) VALUES (?, ?, ?, ?, ?)'),
    updateNewsItem: db.prepare('UPDATE data_news SET teams = ?, message = ?, timestamp = ? WHERE leagueId = ? AND newsId = ?')
};

let transactionStarted = false;
db.serialize();

// Load the list of teams so we can transform league teamId to database teamId
db.all('SELECT sites.id AS siteId, teams.teamId, teams.name FROM teams JOIN sites ON sites.id = teams.siteId WHERE sites.enabled = 1 GROUP BY sites.id, teams.teamId',
    (err, rows = []) => {
        if (err) {
            return logger.error(err);
        }

        const teams = rows.reduce((teams, row) => ({...teams, [row.siteId]: {...teams[row.siteId], [row.teamId]: row.name}}), {});

        // TODO: Run query through watchers table
        db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
            (err, row) => {
                if (err) {
                    return logger.error(err);
                }
        
                const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;
                const child = `${__dirname}/../leagues/${siteId}/news.js`;

                if (!fs.existsSync(child)) {
                    return;
                }

                db.transactional();
                logger.debug(`Retrieving news for league ${leagueName}...`);
                teams[siteUid] = teams[siteUid] || {};

                child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
                    .on('error', err => {
                        logger.error(err);
                    })
                    .on('message', items => {
                        logger.debug(`Processing ${items.length} news items for league ${leagueName}...`);

                        if (items.length > 0) {
                            for (const item of items) {
                                const args = [
                                    item.teams.join(','),
                                    item.message.replace(/::team(\d+)=(.*?)::/g, (a, b, c) => teams[siteUid][b] || c),
                                    item.timestamp,
                                    leagueUid,
                                    item.id
                                ];

                                stmt.updateNewsItem.run(args, function(err) {
                                    if (err) {
                                        return logger.error(err);
                                    }

                                    if (this.changes > 0) {
                                        return;
                                    }

                                    stmt.insertNewsItem.run(args, err => {
                                        if (err) {
                                            logger.error(err);
                                        }
                                    });
                                });
                            }
                        } else {
                            stmt.deleteLeagueNews.run([leagueUid], err => {
                                if (err) {
                                    logger.error(err);
                                }
                            });
                        }
                    }
                );
            }
        );
    }
);

if (global.db !== db) {
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

        stmt.deleteLeagueNews.finalize();
        stmt.insertNewsItem.finalize();
        stmt.updateNewsItem.finalize();

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.info('Closed connection to database');
            }
        });
    });
}
