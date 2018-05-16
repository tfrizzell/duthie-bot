/**
 * This module provides the `watch` command.
 */
'use strict';

const moment = require('moment');

const db = require('../../db');
const logger = require('../../logger');
const utils = require('../utils');

const config = require('../../../config.json');

const registerWatcher = ([guildId, typeId, leagueId, teamId, channelId], callback) => 
    db.run(`UPDATE watchers SET archived = NULL WHERE guildId = ? AND typeId = ? AND leagueId = ? AND IFNULL(teamId, '') = ? AND IFNULL(channelId, '') = ? AND archived IS NOT NULL`, [guildId, typeId, leagueId, teamId || '', channelId || ''], function(err, ...args) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => registerWatcher([guildId, typeId, leagueId, teamId, channelId], callback), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                throw err;
            }
        }

        if (this.changes > 0) {
            if (typeof callback === 'function') {
                callback.call(this, err, ...args);
            }

            return;
        }

        db.run(`INSERT INTO watchers (guildId, typeId, leagueId, teamId, channelId) VALUES (?, ?, ?, ?, ?)`, [guildId, typeId, leagueId, teamId, channelId], function(err, ...args) {
            if (err) {
                if (err.code === 'SQLITE_BUSY') {
                    return setTimeout(() => registerWatcher([guildId, typeId, leagueId, teamId, channelId], callback), 100);
                } else if (err.code !== 'SQLITE_CONSTRAINT') {
                    throw err;
                }
            }

            if (typeof callback === 'function') {
                callback.call(this, err, ...args);
            }
        });
    });

const unregisterWatcher = ([timestamp, guildId, typeId, leagueId, teamId, channelId], callback) => 
    db.run(`UPDATE watchers SET archived = ? WHERE guildId = ? AND typeId = ? AND ? IN (leagueId, '') AND ? IN (IFNULL(teamId, ''), '*') AND ? IN (IFNULL(channelId, ''), '') AND archived IS NULL`, [timestamp, guildId, typeId, leagueId, teamId, channelId], function(err, ...args) {
        if (err) {
            if (err.code === 'SQLITE_BUSY') {
                return setTimeout(() => unregisterWatcher([timestamp, guildId, typeId, leagueId, teamId, channelId], callback), 100);
            } else if (err.code !== 'SQLITE_CONSTRAINT') {
                throw err;
            }
        }

        if (typeof callback === 'function') {
            callback.call(this, err, ...args);
        }
    });

module.exports = (message, command) => {
    if (module.exports[command.subcommand]) {
        return module.exports[command.subcommand](message, command, ...command.arguments.slice(1));
    } else if (command.subcommand === 'help') {
        return require('./help').watch(message, command, ...command.tokens.slice(1));
    } else if (command.subcommand === 'list') {
        return require('./list').watchers(message, command, ...command.arguments.slice(1));
    } else {
        return module.exports.register(message, command, ...command.arguments);
    }
};

module.exports.register = module.exports.watch = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} is attempting to register watcher(s) on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(args)}`);
    const {type, league, team, channel} = command.params;

    utils.isAdmin(message.author, message.guild)
        .then(() => {
            if (channel && isNaN(channel)) {
                logger.error(`${message.author.tag} requested unknown channel ${channel} on guild ${message.guild.name} (${message.guild.id})`);
                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to find the channel ${channel} in your server.`);
            }

            db.all(`SELECT id FROM watcher_types WHERE UPPER(?) IN (UPPER(id), UPPER(name)) OR (UPPER(?) = 'ALL-NEWS' AND id NOT IN (3, 5)) OR (UPPER(?) = 'ALL')`, [type, type, type], (err, types = []) => {
                if (err) {
                    throw err;
                }

                if (types.length === 0) {
                    return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what type of watcher(s) you're attempting to register. May I suggest checking \`${config.prefix} list watcher-types\`?`);
                }

                db.get(`SELECT id FROM leagues WHERE UPPER(?) IN (UPPER(id), UPPER(name), codename) LIMIT 1`, [league], (err, {id: leagueId} = {}) => {
                    if (err) {
                        throw err;
                    }

                    if (!leagueId) {
                        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what league you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list leagues\`?`);
                    }

                    db.get(`SELECT id FROM watcher_types WHERE UPPER(?) IN (UPPER(id), UPPER(name)) OR (UPPER(?) = 'ALL-NEWS' AND id NOT IN (3, 5)) OR (UPPER(?) = 'ALL')`, [team, leagueId], (err, {id: teamId} = {}) => {
                        if (err) {
                            throw err;
                        }

                        if (!teamId && team) {
                            return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what team you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list teams\`?`);
                        }

                        db.serialize(() => {
                            db.transaction(err => {
                                if (err) {
                                    throw err;
                                }
                            });

                            Promise.all(types.map(({id: typeId}) => new Promise((resolve, reject) => {
                                registerWatcher([message.guild.id, typeId, leagueId, teamId, channel], function(err) {
                                    resolve(this.changes);
                                });
                            }))).then(args => {
                                db.commit(err => {
                                    if (err) {
                                        throw err;
                                    }

                                    const count = args.reduce((count, changes) => count + changes, 0);

                                    if (count > 0) {
                                        logger.debug(`${message.author.tag} has registered ${count} watcher(s) on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(command.arguments)}`);
                                    }

                                    if (types.length > 1) {
                                        message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watchers have been registered!`);
                                    } else {
                                        message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watcher has been registered!`);
                                    }
                                });
                            }).catch(err => {
                                logger.error('Failed to register watcher(s):', err);

                                db.rollback(() => {
                                    if (typeof err === 'object' && err.code === 'SQLITE_CONSTRAINT') {
                                        message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but the watcher you attempted to register already exists.`);
                                    } else {
                                        message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but something seems to have gone wrong and I'm unable to finish your request.`);
                                    }
                                });
                            });
                        });
                    });
                });
            });
        }
    ).catch(() => {
            logger.error(`${message.author.tag} attempted to register watcher(s) on guild ${message.guild.name} (${message.guild.id}) but is not an administrator`);
            message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you aren't allow to do that!`);
        }
    );
};

module.exports.unregister = module.exports.unwatch = (message, command, ...args) => {
    logger.verbose(`${message.author.tag} is attempting to unregister watcher(s) on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(args)}`);
    const {type = 'all', league = '', team = '', channel = ''} = command.params;
    const timestamp = moment().toISOString();

    utils.isAdmin(message.author, message.guild)
        .then(() => {
            if (channel && isNaN(channel)) {
                logger.error(`${message.author.tag} requested unknown channel ${channel} on guild ${message.guild.name} (${message.guild.id})`);
                return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to find the channel ${channel} in your server.`);
            }

            db.all(`SELECT id FROM watcher_types WHERE UPPER(?) IN (UPPER(id), UPPER(name)) OR (UPPER(?) = 'ALL-NEWS' AND id NOT IN (3, 5)) OR (UPPER(?) = 'ALL')`, [type, type, type], (err, types = []) => {
                if (err) {
                    throw err;
                }

                if (types.length === 0) {
                    return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what type of watcher(s) you're attempting to unregister. May I suggest checking \`${config.prefix} list watcher-types\`?`);
                }

                db.get(`SELECT id FROM leagues WHERE UPPER(?) IN (UPPER(id), UPPER(name), codename) LIMIT 1`, [league], (err, {id: leagueId = ''} = {}) => {
                    if (err) {
                        throw err;
                    }

                    if (!leagueId && league) {
                        return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what league you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list leagues\`?`);
                    }

                    db.get(`SELECT id FROM watcher_types WHERE UPPER(?) IN (UPPER(id), UPPER(name)) OR (UPPER(?) = 'ALL-NEWS' AND id NOT IN (3, 5)) OR (UPPER(?) = 'ALL')`, [team, leagueId], (err, {id: teamId = ''} = {}) => {
                        if (err) {
                            throw err;
                        }

                        if (!teamId && team && team !== '*') {
                            return message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but I wasn't able to tell what team you're attempting to register a watcher for. May I suggest checking \`${config.prefix} list teams\`?`);
                        }

                        db.serialize(() => {
                            db.transaction(err => {
                                if (err) {
                                    throw err;
                                }
                            });

                            Promise.all(types.map(({id: typeId}) => new Promise((resolve, reject) => {
                                unregisterWatcher([timestamp, message.guild.id, typeId, leagueId, teamId || team, channel], function(err) {
                                    resolve(this.changes);
                                });
                            }))).then(args => {
                                db.commit(err => {
                                    if (err) {
                                        throw err;
                                    }

                                    const count = args.reduce((count, changes) => count + changes, 0);

                                    if (count > 0) {
                                        logger.debug(`${message.author.tag} has unregistered ${count} watcher(s) on guild ${message.guild.name} (${message.guild.id}): ${JSON.stringify(command.arguments)}`);
                                    }

                                    if (count > 1) {
                                        message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watchers have been unregistered!`);
                                    } else {
                                        message.channel.send(`Okay, ${utils.tagUser(message.author, message.guild)}! Your watcher has been unregistered!`);
                                    }
                                });
                            }).catch(err => {
                                logger.error('Failed to unregister watcher(s):', err);

                                db.rollback(() => {
                                    message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but something seems to have gone wrong and I'm unable to finish your request.`);
                                });
                            });
                        });
                    });
                });
            });
        }
    ).catch(() => {
            logger.error(`${message.author.tag} attempted to unregister watcher(s) on guild ${message.guild.name} (${message.guild.id}) but is not an administrator`);
            message.channel.send(`I'm sorry, ${utils.tagUser(message.author, message.guild)}, but you aren't allow to do that!`);
        }
    );
};
