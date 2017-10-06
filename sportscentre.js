/**
 * Load dependencies
 */
const child = require('child_process');
const cron = require('cron');
const Discord = require('discord.js');
const fs = require('fs');

const client = new Discord.Client();
const config = require('./config.json');
const pkg = require('./package.json');

/**
 * Load application data
 */
let data = require('./data/data.json');
let leagues = require('./data/leagues.json');
let teams = require('./data/teams.json');

/**
 * Log startup message
 */
log(`Starting ${config.name} v${pkg.version.replace(/^v+/g,'')}...\n                               node.js v${process.version.replace(/^v+/g,'')}, discord.js v${Discord.version.replace(/^v+/g,'')}\n`);

/**
 * Set up the Discord client event handlers, and log in
 */
client.once('ready', () => {
	log(`${config.name} has logged in to Discord and is performing startup checks...`);
	client.user.setGame('initializing...');

	client.guilds.forEach(g => {
		joinGuild(g, false);
	});

	Object.keys(data.guilds).forEach(id => {
		if (!client.guilds.get(id))
			leaveGuild({id: id}, false);
	});

	saveData().then(() => {
		log(`${config.name} now ready and active on ${client.guilds.size} ${client.guilds.size != 1 ? 'guilds' : 'guild'}!`);
	 	client.user.setGame('in testing...');

		let news = {}, schedule = {}, stars = {};
	
		data.watchers.forEach(w => {
			if (w.type == 'daily-stars') {
				if (stars[w.league])
					return;
	
				stars[w.league] = true;
				sendDailyStarUpdates(w.league);
			} else if (w.type == 'games') {
				if (schedule[w.league] && schedule[w.league][w.team])
					return;
	
				schedule[w.league] = schedule[w.league] || {};
				schedule[w.league][w.team] = true;
				sendScheduleUpdates(w.league, w.team);
			} else if (w.type == 'news') {
				if (news[w.league])
					return;
	
				news[w.league] = true;
				sendNewsUpdates(w.league);
			}
		});
	});
});

client.on('guildCreate', guild => {
	if (!data.guilds[guild.id])
		log(`${config.name} has joined guild ${guild.name}, and is now active on ${client.guilds.size} ${client.guilds.size != 1 ? 'guilds' : 'guild'}`);

	joinGuild(guild);
});

client.on('guildDelete', guild => {
	if (data.guilds[guild.id])
		log(`${config.name} has left guild ${guild.name || guild.id}, and is now active on ${client.guilds.size} ${client.guilds.size != 1 ? 'guilds' : 'guild'}`);

	leaveGuild(guild);
});

// TODO: Clean up client.on('message', ...) handler
client.on('message', message => {
	if (message.author.bot || message.content.trim().substr(0, config.prefix.length) != config.prefix)
		return;

	let guild = message.guild ? data.guilds[message.guild.id] : null;

	if (message.channel.type == 'voice')
		return message.channel.send(`I'm sorry, ${message.author.username}, but you can't do that here!`);

	let tokens = tokenize(message.content.trim()), command = tokens[1].toLowerCase();

	switch (command) {
	  case 'help': {
		  fs.readFile('./help/main.txt', 'utf8', (err, text) => {
			  if ((tokens[2] || '').toLowerCase() == 'me')
				  message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
			  else
				  message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
		  });
		break;
	  }
	  case 'ping': {
		message.channel.send(`PONG!`);
		break;
	  }
	  case 'unwatch': {
		let type = tokens[2].trim().toLowerCase();

		if ((!guild && type != 'help') || (guild && guild.admins.indexOf(message.author.id) == -1)) {
			message.channel.send(`I'm sorry, ${message.author.username}, but you aren't allowed to do that!`);
			break;
		}

		if (type == 'help') {
			fs.readFile('./help/unwatch.txt', 'utf8', (err, text) => {
				if ((tokens[3] || '').toLowerCase() == 'me')
					  message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
				  else
					  message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
			});

			break;
		}

		if (!isValidWatcher(type)) {
			message.channel.send(`"${tokens[2]}" is not a valid watcher type. See ${config.prefix} ${command} help for more information.`);
			break;
		}

		let league = getLeague(tokens[3]), team = getTeam(tokens[4], league);

		if (type == 'games' && !team) {
			message.channel.send(`I'm sorry, ${message.author.username}, but "${tokens[4]}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
			break;
		}

		let channel = null;

		if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
			channel = message.guild.channels.get(channel[1]);

		if (channel === undefined) {
			message.channel.send(`The channel you requested could not be round in your server.`);
			break;
		}

		let _message = {channel: '', log: ''}, oldLength = data.watchers.length;

		if (type == 'all') {
			data.watchers = data.watchers.filter(w => {return (w.guild!=message.guild.id) || (league && w.league!=league.id) || (team && w.team!=team.id) || (channel && w.channel!=channel.id)});
			_message.channel = `Ok! You are no longer watching any updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
			_message.log = `${message.author.tag} has stopped watching all events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
		} else if (/^all.?news$/.test(type)) {
			data.watchers = data.watchers.filter(w => {return (w.guild!=message.guild.id) || (league && w.league!=league.id) || (team && w.team!=team.id) || (channel && w.channel!=channel.id) || !isNewsWatcher(w)});
			_message.channel = `Ok! You are no longer watching any news updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
			_message.log = `${message.author.tag} has stopped watching all news events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
		} else {
			data.watchers = data.watchers.filter(w => {return (w.guild!=message.guild.id) || (league && w.league!=league.id) || (team && w.team!=team.id) || (channel && w.channel!=channel.id) || w.type!=type});
			_message.channel = `Ok! You are no longer watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
			_message.log = `${message.author.tag} has stopped watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
		}

		if (data.watchers.length != oldLength) {
			if (_message.channel)
				message.channel.send(_message.channel.trim().replace(/ +/g, ' '));

			if (_message.log)
				log(_message.log.trim().replace(/ +/g, ' '));

			saveData();
		} else
			message.channel.send(`Ok! I checked over your watcher data, and there was nothing to remove.`);
		break;
	  }
	  case 'watch': {
		let type = tokens[2].trim().toLowerCase();

		if ((!guild && type != 'help') || (guild && guild.admins.indexOf(message.author.id) == -1)) {
			message.channel.send(`I'm sorry, ${message.author.username}, but you aren't allowed to do that!`);
			break;
		}

		if (type == 'help') {
			fs.readFile('./help/watch.txt', 'utf8', (err, text) => {
				if ((tokens[3] || '').toLowerCase() == 'me')
					  message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
				  else
					  message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
			});

			break;
		}

		if (!isValidWatcher(type)) {
			message.channel.send(`I'm sorry, ${message.author.username}, but "${tokens[2]}" is not a valid watcher type. See ${config.prefix} ${command} help for more information.`);
			break;
		}

		let league = getLeague(tokens[3]);

		if (!league) {
			message.channel.send(`I'm sorry, ${message.author.username}, but "${tokens[3]}" is not a valid league. See ${config.prefix} ${command} help for more information.`);
			break;
		}
		
		let team = getTeam(tokens[4], league);

		if (type == 'games' && !team) {
			message.channel.send(`I'm sorry, ${message.author.username}, but "${tokens[4]}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
			break;
		}

		let channel = null;

		if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
			channel = message.guild.channels.get(channel[1]);

		if (channel === undefined) {
			message.channel.send(`I'm sorry, ${message.author.username}, but the channel you requested could not be found in your server.`);
			break;
		} else if (!channel)
			channel = getDefaultChannel(message.guild, guild.defaultChannel);

		let _message = '', oldLength = data.watchers.length, types = [];

		if (type == 'all')
			types.push('bids', 'contracts', 'daily-stars', 'draft', 'games', 'news', 'trades', 'waivers');
		else if (/^all.?news$/.test(type))
			types.push('bids', 'contracts', 'draft', 'news', 'trades', 'waivers');
		else
			types.push(type);

		types.forEach(type => {
			let watcher = {guild: message.guild.id, channel: channel.id, league: league.id, team: team ? team.id : null, type: type},
				_watcher = data.watchers.filter(w => {return (w.guild==watcher.guild) && (w.channel==watcher.channel) && (w.league==watcher.league) && (w.team==watcher.team) && (w.type==watcher.type)}).shift();

			if (_watcher || (type == 'game' && !watcher.team))
				return;

			data.watchers.push(watcher);
			log(`${message.author.tag} has started watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`);
		});

		if (oldLength != data.watchers.length) {
			if (type == 'all')
				message.channel.send(`Ok! You are now watching all updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`);
			else if (/^all.?news$/.test(type))
				message.channel.send(`Ok! You are now watching all news updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`);
			else
				message.channel.send(`Ok! You are now watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`);

			saveData();
		} else
			message.channel.send(`Ok! I checked over your watcher data, and there was nothing new to add.`);
		break;
	  }
	}
});

client.login(config.token);

/**
 * Set up the cron tasks
 */
const jobs = [
	new cron.CronJob('0  0     */8    *  *  *  ', updateLeagues, null, true, 'America/New_York'),
	new cron.CronJob('0  15    */8    *  *  *  ', updateTeams, null, true, 'America/New_York'),
	new cron.CronJob('0  */10  15-16  *  *  *  ', updateDailyStars, null, true, 'America/New_York'),
	new cron.CronJob('0  */10  *      *  *  *  ', updateNews, null, true, 'America/New_York'),
	new cron.CronJob('0  */5   20-23  *  *  *  ', updateNews, null, true, 'America/New_York'),
	new cron.CronJob('0  0     0-19   *  *  *  ', updateSchedules, null, true, 'America/New_York'),
	new cron.CronJob('0  */5   20-23  *  *  0-4', updateSchedules, null, true, 'America/New_York'),
	new cron.CronJob('0  */30  0-19   *  *  5-6', updateSchedules, null, true, 'America/New_York')
];

/**
 * Set up file watchers
 */
fs.watch('./data', (ev, filename) => {
	if (ev != 'change')
		return;

	let data;

	if ((data = filename.match(/^daily-stars-(\d+)\.json$/)) && !updateDailyStars.$running)
		sendDailyStarUpdates.apply(null, data.slice(1));
	else if (filename.match(/^leagues\.json$/) && !updateLeagues.$running)
		leagues = require('./data/leagues.json');
	else if ((data = filename.match(/^news-(\d+)\.json$/)) && !updateNews.$running)
		sendNewsUpdates.apply(null, data.slice(1));
	else if ((data = filename.match(/^schedule-(\d+)-(\d+)\.json$/)) && !updateSchedules.$running)
		sendScheduleUpdates.apply(null, data.slice(1));
	else if (filename.match(/^teams\.json$/) && !updateTeams.$running)
		teams = require('./data/teams.json');
});

/**
 * Set up cleanup handlers
 */
process.on('SIGINT', () => {
	process.exit(2);
});

process.on('uncaughtException', err => {
	console.error(err.stack);
	process.exit(99);
});

process.on('exit', () => {
	console.log(`Shutting down ${config.name} v${pkg.version.replace(/^v+/g, '')}...`);
	client.destroy();
});

/**
 * Common functionality for easy reuasbility
 */
function getDefaultChannel(guild, channel) {
	if (!(guild instanceof Discord.Guild))
		guild = client.guilds.get(guild);

    if (!guild)
    	return;

    if (channel) {
    	if ((channel instanceof Discord.TextChannel) && guild.channels.get(channel.id))
    		return channel;

    	if (channel = guild.channels.get(channel))
    		return channel;
    }

    if (guild.defaultChannel)
    	return guild.defaultChannel;

    return guild.channels.filter(c => {return (c.type=='text') && c.permissionsFor(client.user).has(Discord.Permissions.FLAGS.READ_MESSAGES)}).sort((a,b) => {return a.calculatedPosition-b.calculatedPosition}).first();
}

function getLeague(league) {
	if (!league || !league.toString)
		return;

	let lstring = league.toString().toUpperCase().replace(/[^A-Z0-9]+/g,'');
	return Object.keys(leagues).filter(i => {return (leagues[i]==league) || (leagues[i].id==league) || (leagues[i].code==lstring) || (leagues[i].name.toUpperCase().replace(/[^A-Z0-9]+/g,'')==lstring)}).map(i => {return leagues[i]}).shift();
}

function getTeam(team, league) {
	if (!team || !team.toString)
		return;

	let tstring = team.toString().toUpperCase().replace(/[^A-Z0-9]+/g,'');

	if (league && !(league = getLeague(league)))
		return;

	return Object.keys(teams).filter(i => {return ((teams[i]==team) || (teams[i].id==team) || (teams[i].name.toUpperCase().replace(/[^A-Z0-9]+/g,'')==tstring) || (teams[i].shortname.toUpperCase().replace(/[^A-Z0-9]+/g,'')==tstring)) && (!league || (teams[i].leagues.indexOf(league.id)!=-1))}).map(i => {return teams[i]}).shift();
}

function joinGuild(guild, save) {
	if (!data.guilds[guild.id])
		data.guilds[guild.id] = {};

	if (!data.guilds[guild.id].admins || !data.guilds[guild.id].admins.length)
		data.guilds[guild.id].admins = [guild.owner.id];

	if (!data.guilds[guild.id].defaultChannel)
		data.guilds[guild.id].defaultChannel = getDefaultChannel(guild).id;

	if (save !== false)
		saveData();
}

function leaveGuild(guild, save) {
	delete data.guilds[guild.id];

	data.watchers = data.watchers.filter(w => {return w.guild!=guild.id});

	if (save !== false)
		saveData();
}

function isNewsWatcher(watcher) {
	return /^(bids|contracts|draft|news|trades|waivers)$/.test(watcher instanceof Object ? watcher.type : watcher);
}

function isValidWatcher(watcher) {
	return /^(all|all.?news|games)$/i.test(watcher instanceof Object ? watcher.type : watcher) || isNewsWatcher(watcher);
}

function log() {
	[].forEach.call(arguments, message => {
		let date = new Date();
		console.log(`[${('0000' + date.getFullYear()).substr(-4)}-${('00' + (date.getMonth() + 1)).substr(-2)}-${('00' + date.getDate()).substr(-2)} ${date.toTimeString().replace(/ \S+$/, '')}] ${message}`);
	});
}

function saveData() {
	return new Promise((resolve, reject) => {
		fs.writeFile('./data/data.json', JSON.stringify(data), err => {
			if (err) {
				console.error(err);
				reject(err.message);
			} else
				resolve();
		});
	});
}

function sendDailyStarUpdates(league, stars) {
	if (!(league = getLeague(league)))
		return;

	let watchers = data.watchers.filter(w => {return (!w.league || (w.league==league.id)) && (w.type=='daily-stars')});

	if (!watchers.length)
		return;

	let path = `./data/daily-stars-${league.id}.json`, update = false;

	if (!(stars instanceof Object)) {
		try {
			stars = require(path);
		} catch (e) {
			if (!e.message.match(/Cannot find module/i))
				console.error(e.stack);

			return;
		}
	}

	watchers.forEach(w => {
		// TODO: Iterate over daily star data and send formatted message to channels
	});

	if (update)
		fs.writeFileSync(path, JSON.stringify(stars));
}

function sendNewsUpdates(league, news) {
	if (!(league = getLeague(league)))
		return;

	let watchers = data.watchers.filter(w => {return (!w.league || (w.league==league.id)) && isNewsWatcher(w)});

	if (!watchers.length)
		return;

	let path = `./data/news-${league.id}.json`, update = false;

	if (!(news instanceof Array)) {
		try {
			news = require(path);
		} catch (e) {
			if (!e.message.match(/Cannot find module/i))
				console.error(e.stack);

			return;
		}
	}

	news.forEach(item => {
		if (!item.new)
			return;

		let data, message;
		item.new = false;
		update = true;

		if (item.type == 'bids') {
			if (!(data = item.message.match(/have earned the player rights for (.*?) with a bid amount of (\S+)/i)))
				return;

			let player = {name: data[1].trim()}, bid = {amount: data[2].trim()}, team = getTeam(item.teams[0], league);
			message = `The ${team.name} have won bidding rights to ${player.name} with a bid of ${bid.amount}!`;
		} else if (item.type == 'contracts') {
			if (!(data = item.message.match(/^(.*?) and the .*? have agreed to a (\d+) season deal at (.*?) per season$/i)))
				return;

			let player = {name: data[1].trim()}, contract = {length: data[2].trim(), salary: data[3].trim()}, team = getTeam(item.teams[0], league);
			message = `The ${team.name} have signed ${player.name} to a ${contract.length} season contract worth ${contract.salary} per season!`;
		} else if (/^(draft|trades|waivers)$/.test(item.type) || (item.type == 'news' && item.message.match(/have (been eliminated|claimed|clinched|drafted|placed|traded)/i)))
			message = item.message;

		if (!message)
			return;

		let channels = [];

		watchers.filter(w => {return (!w.team || (item.teams.indexOf(w.team)!=-1)) && (w.type==item.type)}).forEach(w => {
			let guild = client.guilds.get(w.guild), channel = getDefaultChannel(guild, w.channel);

			if (channel)
				channels.push(channel);
		});

		channels.filter((v,i,a) => {return a.indexOf(v)==i}).forEach(channel => {
			log(`Sending message to ${channel.guild.name}#${channel.name}: ${message}`);
			channel.send(message)
		});
	});

	if (update)
		fs.writeFileSync(path, JSON.stringify(news));
}

function sendScheduleUpdates(league, team, schedule) {
	if (!(league = getLeague(league)) || !(team = getTeam(team, league)))
		return;

	let watchers = data.watchers.filter(w => {return (!w.league || w.league==league.id) && (!w.team || w.team==team.id) && (w.type=='games')});

	if (!watchers.length)
		return;

	let path = `./data/schedule-${league.id}-${team.id}.json`, update = false;

	if (!(schedule instanceof Array)) {
		try {
			schedule = require(path);
		} catch (e) {
			if (!e.message.match(/Cannot find module/i))
				console.error(e.stack);

			return;
		}
	}

	schedule.forEach(game => {
		if (!game.updated)
			return;

		game.updated = false;
		update = true;

		if (game.home.score === null || game.visitor.score === null)
			return;

		let channels = [], message, us, them;

		if (game.home.id == w.team) {
			us = game.home;
			them = game.visitor;
		} else {
			us = game.visitor;
			them = game.home;
		}

		if (us.score > them.score)
			message = `**The ${us.name} have defeated the ${them.name} by the score of ${us.score} to ${them.score}!**`;
		else if (us.score < them.score)
			message = `The _${us.name}_ have been defeated by the _${them.name}_ by the score of _${them.score} to ${us.score}_.`;
		else
			message = `The ${us} have tied the ${them.name} by the score of ${us.score} to ${them.score}.`;

		watchers.forEach(w => {
			let guild = client.guilds.get(w.guild), channel = getDefaultChannel(guild, w.channel);

			if (channel)
				channels.push(channel);
		});

		channels.filter((v,i,a) => {return a.indexOf(v)==i}).forEach(channel => {
			log(`Sending message to ${channel.guild.name}#${channel.name}: ${message}`);
			channel.send(message)
		});
	});

	if (update)
		fs.writeFileSync(path, JSON.stringify(schedule));
}

function tokenize(string) {
	return string.replace(/(["'])((?:(?=(\\?))\3.)*?)\1/g, (a,b,c) => {return c.replace(/\s/g, '\037')}).split(/\s+/).map(token => {return token.replace(/\037/g, ' ')});
}

function updateDailyStars() {
	if (updateDailyStars.$running)
		return;

	child.fork(`${__dirname}/scripts/update_daily_stars.js`, {silent: true})
		.on('message', message => {sendDailyStarsUpdates.apply(null,message)})
		.on('exit', () => {delete updateDailyStars.$running});
}

function updateLeagues() {
	if (updateLeagues.$running)
		return;

	updateLeagues.$running = true;

	child.fork(`${__dirname}/scripts/update_leagues.js`, {silent: true})
		.on('message', message => {leagues=message})
		.on('exit', () => {delete updateLeagues.$running});
}

function updateNews() {
	if (updateNews.$running)
		return;

	child.fork(`${__dirname}/scripts/update_news.js`, {silent: true})
		.on('mesage', message => {sendNewsUpdates.apply(null,message)})
		.on('exit', () => {delete updateNews.$running});
}

function updateSchedules() {
	if (updateSchedules.$running)
		return;

	child.fork(`${__dirname}/scripts/update_schedules.js`, {silent: true})
		.on('mesage', message => {sendScheduleUpdates.apply(null,message)})
		.on('exit', () => {delete updateSchedules.$running});
}

function updateTeams() {
	if (updateTeams.$running)
		return;

	child.fork(`${__dirname}/scripts/update_teams.js`, {silent: true})
		.on('message', message => {teams=message})
		.on('exit', () => {delete updateTeams.$running});
}