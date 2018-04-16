'use strict';
require('../../global');

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require(`${__libdir}/db`);
const logger = global.logger || require(`${__libdir}/logger`);

const stmt = {
    deleteLeagueNews: db.prepare('DELETE FROM data_news WHERE leagueId = ?'),
    insertNewsItem: db.prepare('INSERT OR IGNORE INTO data_news (teams, message, timestamp, leagueId, newsId) VALUES (?, ?, ?, ?, ?)'),
    updateNewsItem: db.prepare('UPDATE data_news SET teams = ?, message = ?, timestamp = ? WHERE leagueId = ? AND newsId = ?')
};

const children = [];
let transactionStarted = false;

db.serialize();

// Load the list of teams so we can transform league teamId to database teamId
db.all('SELECT sites.id AS siteId, teams.teamId, teams.name FROM teams JOIN sites ON sites.id = teams.siteId WHERE sites.enabled = 1 GROUP BY sites.id, teams.teamId',
    (err, rows = []) => {
        const teams = rows.reduce((teams, row) => ({...teams, [row.siteId]: {...teams[row.siteId], [row.teamId]: row.name}}), {});

        // TODO: Run query through watchers table
        db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
            (err, row) => {
                const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;

                if (err) {
                    logger.error(err);
                    return children.push(Promise.resolve());
                }

                const child = `${__libdir}/scripts/leagues/${siteId}/news.js`;

                if (!fs.existsSync(child)) {
                    return children.push(Promise.resolve());
                }

                if (!transactionStarted) {
                    transactionStarted = true;
                    db.run('BEGIN TRANSACTION');
                }

                logger.log(`Retrieving news for league ${leagueName}...`);
                teams[siteUid] = teams[siteUid] || {};

                children.push(new Promise(resolve => {
                    let done = false;

                    const finish = () => {
                        if (!done) {
                            done = true;
                        } else {
                            if (process.env.CHILD) {
                                process.send(leagueId, () => resolve());
                            } else {
                                resolve();
                            }
                        }
                    };

                    child_process.fork(child, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
                        .on('error', err => {
                            logger.error(err);
                            finish();
                        })
                        .on('message', items => {
                            logger.log(`Processing ${items.length} news items for league ${leagueName}...`);

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
                                            logger.error(err);
                                        }

                                        if (this.changes === 0) {
                                            stmt.insertNewsItem.run(args, err => {
                                                if (err) {
                                                    logger.error(err);
                                                }

                                                finish();
                                            });
                                        } else {
                                            finish();
                                        }
                                    });
                                }
                            } else {
                                stmt.deleteLeagueNews.run([leagueUid], err => {
                                    if (err) {
                                        logger.error(err);
                                    }

                                    finish();
                                });
                            }
                        })
                        .on('exit', finish);
                }));
            }, () => {
                Promise.all(children).then(() => {
                    db.run('COMMIT', err => {
                        if (err) {
                            logger.error(err);
                        }

                        stmt.deleteLeagueNews.finalize();
                        stmt.insertNewsItem.finalize();
                        stmt.updateNewsItem.finalize();
                    });
                });
            }
        );
    }
);

if (db !== global.db) {
    process.on('unhandledRejection', ex => {
        logger.error(ex);
    });

    process.on('beforeExit', () => {
        if (!db.open) {
            return;
        }

        db.close(err => {
            if (err) {
                logger.error(err);
            } else {
                logger.log('Closed connection to database');
            }
        });
    });
}
