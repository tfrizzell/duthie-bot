/*m.*
 * Load dependencies
 */
const child = require('child_process');
const Discord = require('discord.js');
const fs = require('fs');
const http = require('http');

/*
 * Load application data
 */
let data = JSON.parse(fs.readFileSync('./data/data.json', 'utf8')) || {};
let leagues = JSON.parse(fs.readFileSync('./data/leagues.json', 'utf8')) || {};
let teams = JSON.parse(fs.readFileSync('./data/teams.json', 'utf8')) || {};

const app = require('./application.json');
const client = new Discord.Client();
const pkg = require('./package.json');

/**
 * Common functionality for easy reuasbility
 */
function getDefaultChannel(guild, defaultChannel) {
	let channel;

	if (defaultChannel && (channel = guild.channels.get(defaultChannel)))
		return channel;

	if (guild.defaultChannel)
		return guild.defaultChannel;

	return guild.channels.filter(c => {
		return c.type == 'text';
	}).sort((a, b) => {
		return a.calculatedPosition - b.calculatedPosition;
	}).first();
}

function getLeague(league) {
	let lstring = String(league).toUpperCase().replace(/[^A-Z0-9]+/g, '');

	return Object.keys(leagues).filter(id => {
		var l = leagues[id];
		return l == league || l.id == league || l.code.toUpperCase().replace(/[^A-Z0-9]+/g, '') == lstring || l.name.toUpperCase().replace(/[^A-Z0-9]+/g, '') == lstring;
	}).map(id => {
		return leagues[id];
	}).shift();
}

function getTeam(team, league) {
	let _league = arguments[2];
	league = getLeague(league);

	if (_league && !league)
		return undefined;

	let tstring = String(team).toUpperCase().replace(/[^A-Z0-9]+/g, '');

	return Object.keys(teams).filter(id => {
		var t = teams[id];
		return (t == team || t.id == team || t.name.toUpperCase().replace(/[^A-Z0-9]+/g, '') == tstring || t.shortname.toUpperCase().replace(/[^A-Z0-9]+/g, '') == tstring) && (!league || t.leagues.indexOf(league.id) != -1);
	}).map(id => {
		return teams[id];
	}).shift();
}

function saveData() {
	fs.writeFile('./data/data.json', JSON.stringify(data), err => {
		if (err)
			console.error(err.message)
	});
}

function tokenize(string) {
	let tokens = [];
	return string.replace(/(["'])(.*?[^\\])\1/g, (substr, token, value) => {
		tokens.push(value);
		return '${' + (tokens.length - 1) + '}';
	}).split(/\s+/).map(token => {
		let t;
		return (t = token.match(/^\${(\d+)}$/)) ? tokens[t[1]] : token;
	});
}

function updateNewsSubscribers(league, team) {
	if (!(league = getLeague(league)) || (team && !(team = getTeam(team, league))))
		return;

	let path = `./data/news-${league.id}${team ? '-' + team.id : ''}.json`;
	let subs = data.watchers.filter(s => {return s.league==league.id && (!team || s.teams.indexOf(team.id) != -1) && /^(bids|contracts|draft|news|trades|waivers)$/.test(s.type)});

	if (!subs.length)
		return fs.unlink(path);

	try {
		let news = require(path), count = 0;

		news.filter(item => {
			return item.new;
		}).forEach(item => {
			let data, message, team = getTeam(item.teams[0], league);
			item.new = false;
			count++;

			if (item.type == 'bidding') {
				if (!(data = item.message.match(/have earned the player rights for (.*?) with a bid amount of (\S+)/i)))
					return;

				let player = {name: data[1].trim()}, bid = {amount: data[2].trim()};
				message = eval('`' + app.messages.news.bidWon + '`');
			} else if (item.type == 'contract') {
				if (!(data = item.message.match(/^(.*?) and the .*? have agreed to a (\d+) season deal at (.*?) per season$/i)))
					return;

				let player = {name: data[1].trim()}, contract = {length: data[2].trim(), salary: data[3].trim()};
				message = eval('`' + app.messages.news.signed + '`');
			} else if (/^(draft|trade|waiver)$/.test(item.type) || (item.type == 'news' && item.message.match(/have (been eliminated|claimed|clinched|drafted|placed|traded)/i)))
				message = item.message;

			if (!message)
				return;

			subs.filter(sub => {
				return (!sub.team || item.teams.indexOf(sub.team) != -1) && (sub.type == 'news' || sub.type == item.type);
			}).forEach(sub => {
				let guild = client.guilds.get(sub.guild);
				(guild.channels.get(sub.channel) || getDefaultChannel(guild)).send(message);
			});
		});

		if (count)
			fs.writeFile(path, JSON.stringify(news));
	} catch (e) {
		console.error(e);
	}
}

function updateScheduleSubscribers(league, team) {
	if (!(league = getLeague(league)) || (team && !(team = getTeam(team, league))))
		return;

	let path = `./data/schedule-${league.id}${team ? '-' + team.id : ''}.json`;
	let subs = data.watchers.filter(s => {return s.league==league.id && (!team || s.team==team.id) && s.type=='games'});

	if (!subs.length)
		return fs.unlink(path);

	try {
		let games = require(path), count = 0;

		Object.keys(games).filter(id => {
			return games[id].updated;
		}).forEach(id => {
			let game = games[id];
			game.updated = false;
			count++;

			if (game.home.score === null || game.visitor.score === null)
				 return;

			subs.forEach(function(s) {
				let guild = client.guilds.get(s.guild), channel = guild.channels.get(s.channel) || getDefaultChannel(guild), our = game.home.id == s.team ? game.home : game.visitor, their = our == game.home ? game.visitor : game.home;

				if (our.score > their.score)
					channel.send(eval('`' + app.messages.games.win + '`'));
				else if (our.score < their.score)
					channel.send(eval('`' + app.messages.games.loss + '`'));
				else
					channel.send(eval('`' + app.messages.games.tie + '`'));
			});
		});

		if (count)
			fs.writeFile(path, JSON.stringify(games));
	} catch (e) {
		console.log(e);
	}
}



/**
 * Set up the Discord client event handlers, and log in
 */
client.on('ready', () => {
	console.log(eval('`' + app.messages._core.start + '`'));
	client.user.setGame('in testing...');

	client.guilds.forEach(guild => {
		data.guilds[guild.id] = data.guilds[guild.id] || {};
		data.guilds[guild.id].admins = data.guilds[guild.id].admins || [guild.ownerId];
		data.guilds[guild.id].defaultChannel = data.guilds[guild.id].defaultChannel || getDefaultChannel(guild).id;
	});

	saveData();
});

client.on('guildCreate', guild => {
	console.log(eval('`' + app.messages._core.guild.joined + '`'));

	if (data.guilds[guild.id])
		return;

	data.guilds[guild.id] = {
		admins: [guild.ownerId],
		defaultChannel: getDefaultChannel(guild).id
	};

	saveData();
});

client.on('guildDelete', guild => {
	console.log(eval('`' + app.messages._core.guild.left + '`'));
	delete data.guilds[guild.id];
	data.watchers = data.watchers.filter(w => {return w.guild!=guild.id});
	saveData();
});

client.on('message', message => {
	if (message.author.bot) return;

	let tokens = tokenize(message.content.trim());
	if (tokens[0] != app.config.prefix) return;
	if (data.guilds[message.guild.id].admins.indexOf(message.author.id) == -1) return message.channel.send(eval('`' + app.messages._core.permissionDenied + '`'));
	if (message.channel.type == 'voice' || !message.guild.id) return message.channel.send('You can\'t do that here!');

	switch (tokens[1].toLowerCase()) {
	  case 'panic!': {
		console.log(`${message.author.username} issued panic command from channel ${message.channel.name} on server ${message.guild.name}`);
		process.exit();
	  }
	  case 'help': {
		message.channel.send(eval('`' + app.messages.help._help + '`'), {code: app.config.help.format, split: app.config.help.split});
		break;
	  }
	  case 'ping': {
		var time = Date.now() - message.createdAt.valueOf();
		message.channel.send(`PONG! Your ping time is ${time}ms`);
		break;
	  }
	  case 'unwatch': {
		let channel, err, league = {}, team = {}, type = tokens[2].trim().toLowerCase();

		if (tokens[3]) {
			if (tmp = tokens[3].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(tmp[1]) || null;
			else
				league = getLeague(tokens[3]) || {id: null, name: ''};
		}

		if (tokens[4]) {
			if (tmp = tokens[4].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(tmp[1]) || null;
			else
				team = getTeam(tokens[4], league) || {id: null, name: ''};
		}

		if (tokens[5] && (tmp = tokens[5].match(/^<\#(\d+)>$/)))
			channel = message.guild.channels.get(tmp[1]) || null;

		if (type == 'help')
			err = new Error(eval('`' + app.messages.help.unwatch + '`'));
		else if (!/^(all|all.?news|bids|contracts|draft|games|news|trades|waivers)$/.test(type))
			err = new Error(`${type} is not a valid watcher type. See ${app.config.prefix} ${tokens[1].toLowerCase()} help for more information`);
		else if (tokens[3] && !league)
			err = new Error(`${tokens[3]} is not a valid league`);
		else if (type == 'games' && !team)
			err = new Error(`${tokens[4]} is not a valid team in ${league.name}`);
		else if (channel === null)
			err = new Error(`The channel you requested could not be found in your server`);

		if (err) {
			message.channel.send(err.message, {code: app.config.help.format, split: app.config.help.split});
			break;
		}

		let strType = type.replace(/([^w])s$/, '$1'), len = data.watchers.length, cmessage, lmessage;

		if (type == 'all') {
			if (team.id !== undefined) {
				if (league.id !== undefined) {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id || w.team!=team.id});
					cmessage = `Ok! You are no longer watching for any updates from the ${league.name} ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching all events from the ${league.name} ${team.name} on server ${message.guild.name}`;
				} else {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.team!=team.id});
					cmessage = `Ok! You are no longer watching for any updates from the ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching all events from the ${team.name} on server ${message.guild.name}`;
				}
			} else if (league.id !== undefined) {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id});
				cmessage = `Ok! You are no longer watching for any updates from ${league.name}.`;
				lmessage = `${message.author.username} has stopped watching all events from ${league.name} on server ${message.guild.name}`;
			} else {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id});
				cmessage = `Ok! You are no longer watching for any updates.`;
				lmessage = `${message.author.username} has stopped watching all events on server ${message.guild.name}`;
			}
		} else if (/^all.?news$/.test(type)) {
			if (team.id !== undefined) {
				if (league.id !== undefined) {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id || w.team!=team.id || w.type=='games'});
					cmessage = `Ok! You are no longer watching for news updates from the ${league.name} ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching all news events from the ${league.name} ${team.name} on server ${message.guild.name}`;
				} else {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.team!=team.id || w.type=='games'});
					cmessage = `Ok! You are no longer watching for news updates from the ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching all news events from the ${team.name} on server ${message.guild.name}`;
				}
			} else if (league.id !== undefiend) {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id || w.type=='games'});
				cmessage = `Ok! You are no longer watching for news updates from ${league.name}.`;
				lmessage = `${message.author.username} has stopped watching all news events from ${league.name} on server ${message.guild.name}`;
			} else {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.type=='games'});
				cmessage = `Ok! You are no longer watching for news updates.`;
				lmessage = `${message.author.username} has stopped watching all news events on server ${message.guild.name}`;
			}
		} else {
			if (team.id !== undefined) {
				if (league.id !== undefined) {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id || w.team!=team.id || w.type!=type});
					cmessage = `Ok! You are no longer watching for ${strType} updates from the ${league.name} ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching ${strType} events from the ${league.name} ${team.name} on server ${message.guild.name}`;
				} else {
					data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.team!=team.id || w.type!=type});
					cmessage = `Ok! You are no longer watching for ${strType} updates from the ${team.name}.`;
					lmessage = `${message.author.username} has stopped watching ${strType} events from the ${team.name} on server ${message.guild.name}`;
				}
			} else if (league.id !== undefined) {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.league!=league.id || w.type!=type});
				cmessage = `Ok! You are no longer watching for ${strType} updates from ${league.name}.`;
				lmessage = `${message.author.username} has stopped watching ${strType} events from ${league.name} on server ${message.guild.name}`;
			} else {
				data.watchers = data.watchers.filter(w => {return w.guild!=message.guild.id || w.type!=type});
				cmessage = `Ok! You are no longer watching for ${strType} updates.`;
				lmessage = `${message.author.username} has stopped watching ${strType} events on server ${message.guild.name}`;
			}
		}

		if (data.watchers.length != len) {
			message.channel.send(cmessage.trim().replace(/ +/g, ' '));
			console.log(lmessage.trim().replace(/ +/g, ' '));
			saveData();
		} else
			message.channel.send('There were no watchers found to be removed');
		break;
	  }
	  case 'watch': {
		let channel = null, err, league, team, tmp, type = tokens[2].trim().toLowerCase();

		if (tokens[3]) {
			if (tmp = tokens[3].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(tmp[1]);
			else
				league = getLeague(tokens[3]);
		}

		if (tokens[4]) {
			if (tmp = tokens[4].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(tmp[1]);
			else
				team = getTeam(tokens[4], league);
		}

		if (tokens[5] && (tmp = tokens[5].match(/^<\#(\d+)>$/)))
			channel = message.guild.channels.get(tmp[1]);

		if (channel === null)
			channel = getDefaultChannel(message.guild, data.guilds[message.guild.id].defaultChannel);

		if (type == 'help')
			err = new Error(eval('`' + app.messages.help.watch + '`'));
		else if (!/^(all|all.?news|bids|contracts|draft|games|news|trades|waivers)$/.test(type))
			err = new Error(`${type} is not a valid watcher type. See ${app.config.prefix} ${tokens[1].toLowerCase()} help for more information`);
		else if (!league && !tokens[3].match(/^<\#(\d+)>$/))
			err = new Error(`${tokens[3]} is not a valid league`);
		else if (type == 'games' && !team && !tokens[4].match(/^<\#(\d+)>$/))
			err = new Error(`${tokens[4]} is not a valid team in ${league.name}`);
		else if (!channel)
			err = new Error(`The channel you requested could not be found in your server`);

		if (err) {
			message.channel.send(err.message, {code: app.config.help.format, split: app.config.help.split});
			break;
		}

		var types = [], updated = false;

		if (type == 'all')
			types.push('bids', 'contracts', 'draft', 'games', 'news', 'trades', 'waivers');
		else if (/^all.?news$/.test(type))
			types.push('bids', 'contracts', 'draft', 'news', 'trades', 'waivers');
		else
			types.push(type);

		types.forEach(type => {
			let watcher = {guild: message.guild.id, channel: channel.id, league: league.id, team: team ? team.id : null, type: type}, _watcher = data.watchers.filter(w => {return w.guild==watcher.guild && w.league==watcher.league && w.team==watcher.team && w.type==watcher.type}).shift(), strType = type.replace(/([^w])s$/, '$1'), cmessage, lmessage;

			if (_watcher) {
				if (_watcher.channel != watcher.channel) {
					_watcher.channel = watcher.channel;
					updated = true;

					cmessage = `Ok! Your ${strType} update watcher for the ${league.name}${team.name ? ' ' + team.name : ''} will now send messages to channel #${channel.name}`;
					lmessage = `${message.author.username} has updated the ${strType} watcher for the ${league.name}${team.name ? ' ' + team.name : ''} to use ${message.guild.name}#${channel.name}`;
				} else if (types.length == 1)
					cmessage = team ? `You are already watching for ${strType} updates from the ${league.name} ${team.name} on channel #${channel.name}` : `You are already watching for ${strType} updates from ${league.name} on channel #${channel.name}`
			} else {
				data.watchers.push(watcher);
				updated = true;

				if (team) {
					cmessage = `Ok! You are now watching for ${strType} updates from the ${league.name} ${team.name}!`;
					lmessage = `${message.author.username} has started watching ${strType} events from the ${league.name} ${team.name} on channel ${message.guild.name}#${channel.name}`;
				} else {
					cmessage = `Ok! You are now watching for ${strType} updates from ${league.name}!`;
					lmessage = `${message.author.username} has started watching ${strType} events from ${league.name} on channel ${message.guild.name}#{$channel.name}`;
				}
			}

			if (cmessage) message.channel.send(cmessage);
			if (lmessage) console.log(lmessage);
		});

		if (updated)
			saveData();
		else if (types.length > 1)
			message.channel.send('Great news! You were already watching for everything you wanted!');
		break;
	  }
	}
});

client.login(app.config.token);

/**
 * Set up filesystem data watchers
 */
fs.watch('./data', (ev, filename) => {
	if (ev != 'change') return;
	let data;

	if (filename == 'leagues.json')
		leagues = require('./data/leagues.json');
	else if (filename == 'teams.json')
		teams = require('teams.json');
	else if (data = filename.match(/^news-(\d+)(?:-(\d+))?\.json$/))
		updateNewsSubscribers(data[1], data[2]);
	else if (data = filename.match(/^schedule-(\d+)(?:-(\d+))?\.json$/))
		updateScheduleSubscribers(data[1], data[2]);
});

/**
 * Set up the pseudo-cron task runner
 */
let cron;

setTimeout(cron = () => {
	if (typeof cron == 'function') {
		var _cron = cron;
		cron = setInterval(_cron, 60000);
	}

	let date = new Date(), dst = date.getTimezoneOffset() < new Date(date.valueOf() - (date.valueOf() % 31557600000)).getTimezoneOffset();
	date.setUTCHours(date.getUTCHours() - (dst ? 4 : 5));

	if (date.getUTCHours() % 8 == 0) {
		if (date.getUTCMinutes() == 0)
			child.fork(`${__dirname}/scripts/update_leagues.js`, {silent:true});
		else if (date.getUTCMinutes() == 15)
			child.fork(`${__dirname}/scripts/update_teams.js`, {silent:true});
	}

	if ((date.getUTCHours() > 19 && date.getUTCMinutes() % 5 == 0) || date.getUTCMinutes() % 30 == 0)
		child.fork(`${__dirname}/scripts/update_news.js`, {silent:true});

	if (date.getUTCHours() < 20 && date.getUTCMinutes() == 0)
		child.fork(`${__dirname}/scripts/update_schedules.js`, {silent:true});
	else if (date.getUTCDay() <= 4 && date.getUTCHours() > 19 && date.getUTCMinutes() % 10 == 0)
		child.fork(`${__dirname}/scripts/update_schedules.js`, {silent:true});
	else if ((date.getUTCDay() == 5 || date.getUTCDay() == 6) && date.getUTCHours() > 19 && date.getUTCMinutes() == 0)
		child.fork(`${__dirname}/scripts/update_schedules.js`, {silent:true});
}, first = 60000 - (Date.now() % 60000));

/**
 * Set up the cleanup handlers
 */
process.on('SIGINT', () => {
	process.exit(2);
});

process.on('uncaughtException', err => {
	console.log(err.stack);
	process.exit(99);
});

process.on('exit', () => {
	console.log(`Shutting down ${app.config.name} v${pkg.version.replace(/^v+/g, '')}...`);
	client.destroy();
	clearInterval(cron);
});
