/**
 * This script calls the teams.js script for each enabled league and stores the results
 * in the sqlite3 database.
 */
'use strict';

const child_process = require('child_process');
const fs = require('fs');

const db = require('../../db');
const logger = require('../../logger');

const stmt = {
    getLeagues: db.prepare(`SELECT sites.id AS siteUid, sites.siteId, leagues.id AS leagueUid, leagues.leagueId, leagues.name AS leagueName, leagues.extraData FROM leagues JOIN sites ON sites.id = leagues.siteId WHERE leagues.disabled = 0`),
    getTeamId: db.prepare(`SELECT id, 0 AS mapped FROM teams WHERE codename = ? UNION SELECT teamId AS id, 1 AS mapped FROM team_map WHERE siteId = ? AND mappedTeamId = ? ORDER BY mapped LIMIT 1`),
    insertLeagueTeamMap: db.prepare(`INSERT INTO league_team_map (leagueId, teamId) VALUES (?, ?)`),
    insertTeam: db.prepare(`INSERT INTO teams (name, shortname) VALUES (?, ?)`),
    insertTeamMap: db.prepare(`INSERT INTO team_map (siteId, mappedTeamId, teamId) VALUES (?, ?, ?)`),
    updateTeam: db.prepare(`UPDATE teams SET name = ?, shortname = ? WHERE id = ?`)
};

const MAX_CHILDREN = {maxChildren: 'Infinity', ...require('../../../config.json').scripts}.maxChildren;
let leagues = [];

const fetchTeams = (league = {}) => {
    const {siteUid, siteId, leagueUid, leagueId, leagueName, extraData} = league;
    const script = `${__dirname}/../leagues/${siteId}/teams.js`;

    if (!siteId || !fs.existsSync(script)) {
        if (leagues.length > 0) {
            fetchTeams(leagues.shift());
        }

        return;
    }
    
    logger.info(`Retrieving teams for league ${leagueName}...`);

    child_process.fork(script, [JSON.stringify({...JSON.parse(extraData), leagueId: leagueId})], {env: {CHILD: true}})
        .on('error', err => {
            logger.error(err);
        })
        .on('message', teams => {
            teams = Object.values(teams);
            logger.info(`Processing ${teams.length} teams for league ${leagueName}...`);

            if (teams.length === 0) {
                return;
            }

            Promise.all(teams.map(team => 
                new Promise(resolve => {
                    stmt.getTeamId.get([
                        team.name.replace(/[\/\-'\. ]+/g, '').toUpperCase(),
                        siteUid,
                        team.id
                    ], (err, {id: teamId, mapped = false} = {}) => {
                        if (err) {
                            logger.error(err);
                            return resolve();
                        }

                        if (!teamId) {
                            stmt.insertTeam.run([team.name, team.shortname], function(err) {
                                if (err) {
                                    logger.error(err);
                                    return resolve();
                                }

                                resolve([team.id, this.lastID]);
                            });
                        } else {
                            if (mapped) {
                                stmt.updateTeam.run([team.name, team.shortname, teamId], err => {
                                    if (err) {
                                        logger.error(err);
                                    }

                                    resolve([team.id, teamId]);
                                });
                            } else {
                                resolve([team.id, teamId]);
                            }
                        }
                    });
                })
            )).then(teamMap => {
                teamMap = teamMap.reduce((teamMap, [mappedTeamId, teamId]) => ({...teamMap, [mappedTeamId]: teamId}), {});

                for (const [mappedTeamId, teamId] of Object.entries(teamMap)) {
                    stmt.insertTeamMap.run([siteUid, mappedTeamId, teamId], err => {
                        if (err && !/UNIQUE constraint failed: team_map\.siteId, team_map\.mappedTeamId/.test(err)) {
                            logger.error(err);
                        }
                    });

                    stmt.insertLeagueTeamMap.run([leagueUid, teamId], err => {
                        if (err && !/NIQUE constraint failed: league_team_map\.leagueId, league_team_map\.teamId/.test(err)) {
                            logger.error(err);
                        }
                    });
                }
            });
        })
        .on('exit', () => {
            fetchTeams(leagues.shift());
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
                fetchTeams(league);
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
