/**
 * This module provides the `watch` command.
 */
'use strict';

const moment = require('moment');

const utils = require('../utils');

const config = global.config || require('../../../config.json');
const db = global.db || require('../../db');

module.exports = command => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](command, ...command.arguments.slice(1));
    } else {
        return module.exports.register(command, ...command.arguments);
    }
};

module.exports.help = (command, ...args) => {
    const message = command.message;
    const [target = ''] = args;

    const response = 
`Hello, ${utils.getUserNickname(message.author, message.guild)}! Here is what you need to know about \`${config.prefix} watch\`:

# SYNTAX
    ${config.prefix} watch type=<type> league=<league>[ team=<team>][ channel=<channel>]

    It is important to remember to include the parameter name when sending a request, otherwise the paramter will be ignored. If your parameter value contains spaces, be sure to enclose it in quotations (ex: league="LGHL PSN") or remove the spaces (ex: league=LGHLPSN).

# TYPE (required)
    When registering a watcher, you must specify the type of watcher being registered. See \`${config.prefix} list watcher-types\` for a list of valid types.

# LEAGUE (required)
    When registering a watcher, you must specify the league you wish to watch. See \`${config.prefix} list leagues\` for a list of valid leagues.

# TEAM (optional)
    When registering a watcher, you may specify the team you wish to watch. See \`${config.prefix} list teams\` for a list of valid teams.

# CHANNEL (optional)
    When registering a watcher, you may specify the channel you wish to have messages sent to. If the channel doesn't exist or ${config.name} can't access it, a default channel will be used.`;

    if (target.match(/\bme\b/i)) {
        message.author.send(response, {code: 'vb', split: 'true'});
    } else {
        message.channel.send(response, {code: 'vb', split: 'true'});
    }
};

module.exports.register = (command, ...args) => {
    const message = command.message;
    const guild = message.guild;
    const {type, league, team, channel} = command.params;

    db.get(`SELECT id FROM watcher_types WHERE UPPER(?) IN (UPPER(id), UPPER(name)) OR (UPPER(?) = 'ALL-NEWS' AND id NOT IN (3, 5)) OR (UPPER(?) = 'ALL')`, [type, type, type], 
        (err, types = []) => {
            if (err) {
                throw err;
            }

            if (!(types instanceof Array)) {
                types = [types];
            }

            if (types.length === 0) {
                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what type of watcher you're attempting to register. May I suggest checking \`${config.prefix} list watcher-types\`?`);
            }

            db.get(`SELECT id FROM leagues WHERE UPPER(?) IN (UPPER(id), UPPER(name), UPPER(codename)) LIMIT 1`, [league], 
                (err, {id: leagueId} = {}) => {
                    if (err) {
                        throw err;
                    }

                    if (!leagueId) {
                        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what league you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list leagues\`?`);
                    }

                    db.get(`SELECT teams.id FROM teams JOIN league_team_map ON league_team_map.teamId = teams.id WHERE UPPER(?) IN (UPPER(teams.id), UPPER(teams.name), UPPER(teams.shortname), UPPER(teams.codename)) AND league_team_map.leagueId = ? LIMIT 1`, [team, leagueId],
                        (err, {id: teamId} = {}) => {
                            if (err) {
                                throw err;
                            }

                            if (!teamId && team) {
                                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what team you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list teams\`?`);
                            }

                            db.serialize(() => {
                                db.run('BEGIN TRANSACTION');
                
                                Promise.all(types.map(({id: typeId}) => new Promise((resolve, reject) => {
                                    db.run(`UPDATE watchers SET archived = NULL WHERE guildId = ? AND typeId = ? AND leagueId = ? AND IFNULL(teamId, '') = ? AND IFNULL(channelId, '') = ?`, [guild.id, typeId, leagueId, teamId || '', channel || ''],
                                        function(err) {
                                            if (err) {
                                                return reject(err);
                                            }
                            
                                            if (this.changes > 0) {
                                                return resolve(this.changes);
                                            }
                            
                                            db.run(`INSERT INTO watchers (guildId, typeId, leagueId, teamId, channelId) VALUES (?, ?, ?, ?, ?)`, [guild.id, typeId, leagueId, teamId, channel],
                                                err => {
                                                    if (err) {
                                                        return reject(err);
                                                    }
                            
                                                    resolve(1);
                                                }
                                            );
                                        }
                                    )
                                }))).then((...args) => {
                                    db.run('COMMIT', err => {
                                        if (err) {
                                            throw err;
                                        }

                                        const count = args.reduce((count, changes) => count + changes[0], 0);

                                        if (count === 1) {
                                            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watcher has been registered!`);
                                        } else {
                                            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watchers have been registered!`);
                                        }
                                    });
                                }).catch(err => {
                                    db.run('ROLLBACK', () => {
                                        throw err;
                                    });
                                });
                            });
                        }
                    );
                }
            );
        }
    );
};

module.exports.unregister = (command, ...args) => {
    const message = command.message;
    const guild = message.guild;
    const {type = '', league = '', team = '', channel = ''} = command.params;
    const timestamp = moment().toISOString();

    db.get(`SELECT id FROM watcher_types WHERE ? IN (id, name, '') OR (? = 'all-news' AND id NOT IN (3, 5)) OR (? = 'all')`, [type, type, type], 
        (err, types = []) => {
            if (err) {
                throw err;
            }

            if (!(types instanceof Array)) {
                types = [types];
            }

            if (types.length === 0) {
                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what type of watcher you're attempting to register. May I suggest checking \`${config.prefix} list watcher-types\`?`);
            }

            db.get(`SELECT id FROM leagues WHERE UPPER(?) IN (UPPER(id), UPPER(name), UPPER(codename)) LIMIT 1`, [league], 
                (err, {id: leagueId = ''} = {}) => {
                    if (err) {
                        throw err;
                    }

                    if (!leagueId && league) {
                        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what league you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list leagues\`?`);
                    }

                    db.get(`SELECT teams.id FROM teams JOIN league_team_map ON league_team_map.teamId = teams.id WHERE UPPER(?) IN (UPPER(teams.id), UPPER(teams.name), UPPER(teams.shortname), UPPER(teams.codename)) AND league_team_map.leagueId = ? LIMIT 1`, [team, leagueId],
                        (err, {id: teamId = ''} = {}) => {
                            if (err) {
                                throw err;
                            }

                            if (!teamId && team) {
                                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what team you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list teams\`?`);
                            }

                            db.serialize(() => {
                                db.run('BEGIN TRANSACTION');

                                Promise.all(types.map(({id: typeId}) => new Promise((resolve, reject) => {
                                    db.run(`UPDATE watchers SET archived = ? WHERE guildId = ? AND typeId = ? AND ? IN (leagueId, '') AND ? IN (IFNULL(teamId, ''), '') AND ? IN (IFNULL(channelId, ''), '')`, [timestamp, guild.id, typeId, leagueId, teamId, channel],
                                        function(err) {
                                            if (err) {
                                                return reject(err);
                                            }

                                            resolve(this.changes);
                                        }
                                    )
                                }))).then((...args) => {
                                    db.run('COMMIT', err => {
                                        if (err) {
                                            throw err;
                                        }

                                        const count = args.reduce((count, changes) => count + changes[0], 0);

                                        if (count === 1) {
                                            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watcher has been unregistered!`);
                                        } else {
                                            message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watchers have been unregistered!`);
                                        }
                                    });
                                }).catch(err => {
                                    db.run('ROLLBACK', () => {
                                        throw err;
                                    });
                                });
                            });
                        }
                    );
                }
            );
        }
    );
};
