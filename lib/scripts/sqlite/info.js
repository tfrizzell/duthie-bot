'use strict';
require('../../global');

const child_process = require('child_process');
const fs = require('fs');

const db = global.db || require(`${__libdir}/db`);
const logger = global.logger || require(`${__libdir}/logger`);

const stmt = {
    updateLeague: db.prepare('UPDATE leagues SET name = ?, codename = ?, extraData = ? WHERE id = ?')
};

const children = [];
let transactionStarted = false;

db.serialize();

// TODO: Run query through watchers table
db.each('SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.enabled = 1 AND sites.enabled = 1',
    (err, row) => {
        const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = row;

        if (err) {
            logger.error(err);
            return children.push(Promise.resolve());
        }

        const child = `${__libdir}/scripts/leagues/${siteId}/info.js`;

        if (!fs.existsSync(child)) {
            return children.push(Promise.resolve());
        }

        if (!transactionStarted) {
            transactionStarted = true;
            db.run('BEGIN TRANSACTION');
        }

        logger.log(`Retrieving info for league ${leagueName}...`);

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
                .on('message', info => {
                    logger.log(`Processing info for league ${leagueName}...`);

                    if (info.id && info.name && info.codename) {
                        stmt.updateLeague.run([info.name || leagueName, info.codename, JSON.stringify({...info, codename: undefined, id: undefined, name: undefined}), leagueUid], err => {
                            if (err) {
                                logger.error(err);
                            }

                            finish();
                        });
                    } else {
                        finish();
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

                stmt.updateLeague.finalize();
            });
        });
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
