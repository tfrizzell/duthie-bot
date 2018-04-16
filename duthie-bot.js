/**
 * Load dependencies
 */
const child = require('child_process');
const cron = require('cron');
const Discord = require('discord.js');
const fs = require('fs');

const client = new Discord.Client();
const config = require(`${__dirname}/config.json`);
const pkg = require(`${__dirname}/package.json`);

/**
 * Load application data
 */
let data = require(`${__dirname}/data/data.json`);
let leagues = require(`${__dirname}/data/leagues.json`);
let teams = require(`${__dirname}/data/teams.json`);

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

	client.guilds.forEach(guild => joinGuild(guild, false));

	Object.keys(data.guilds).forEach(id => {
		if (!client.guilds.get(id))
			leaveGuild({id: id}, false);
	});

	log(`${config.name} now ready and active on ${client.guilds.size} ${client.guilds.size != 1 ? 'guilds' : 'guild'}!`);
 	client.user.setGame('in testing...');
	saveData();
});

client.on('ready', () => {
	let news = {}, schedule = {}, stars = {};

	data.watchers.forEach(watcher => {
		if (watcher.type == 'games') {
			if (schedule[watcher.league] && schedule[watcher.league][watcher.team])
				return;

			schedule[watcher.league] = schedule[watcher.league] || {};
			schedule[watcher.league][watcher.team] = true;
			sendScheduleUpdates(watcher.league, watcher.team);
		} else if (watcher.type == 'news') {
			if (news[watcher.league])
				return;

			news[watcher.league] = true;
			sendNewsUpdates(watcher.league);
		}
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
		return message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but you can't do that here!`);

	let tokens = tokenize(message.content.trim());
	let command = (tokens[1] || '').toLowerCase();
	let [type, league, team, channel, _message, oldLength] = [];

	switch (command) {
		case 'help':
			fs.readFile(`${__dirname}/help/main.txt`, 'utf8', (err, text) => {
				if ((tokens[2] || '').toLowerCase() == 'me')
					message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
				else
					message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
			});
			break;

		case 'list':
			let mode = (tokens[2] || '').trim().toLowerCase();
			let none = true;

			switch (mode) {
				case 'help':
				default:
					fs.readFile(`${__dirname}/help/list.txt`, 'utf8', (err, text) => {
						if ((tokens[3] || '').toLowerCase() == 'me')
							message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
						else
							message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
					});
				break;

				case 'league':
				case 'leagues':
					_message = `${('ID' + ' '.repeat(5)).substr(0, 5)}   ${('NAME' + ' '.repeat(15)).substr(0, 15)}   ${('CODE' + ' '.repeat(10)).substr(0, 10)}\n${'-'.repeat(5)}   ${'-'.repeat(15)}   ${'-'.repeat(10)}`;
		
					Object.keys(leagues).forEach(i => {
						_message += `\n${(i + ' '.repeat(5)).substr(0, 5)}   ${(leagues[i].name + ' '.repeat(15)).substr(0, 15)}   ${(leagues[i].code + ' '.repeat(10)).substr(0, 10)}`;
						none = false;
					});

					if (none)
						_message += `There are no leagues available`;

					message.channel.send(escapeMarkdown(_message), {code: config.help.code, split: config.help.split});
				break;
	
				case 'team':
				case 'teams':
					if (tokens[4] == 'PSN') {
						tokens[3] += tokens[4];
						[].splice.apply(tokens, [4, 1].concat(tokens.slice(5)));
					}
		
					league = getLeague(tokens[3]);
					_message = `${('ID' + ' '.repeat(5)).substr(0, 5)}   ${('NAME' + ' '.repeat(30)).substr(0, 30)}   ${('SHORTNAME' + ' '.repeat(15)).substr(0, 15)}   ${('LEAGUES' + ' '.repeat(30)).substr(0, 30)}\n${'-'.repeat(5)}   ${'-'.repeat(30)}   ${'-'.repeat(15)}   ${'-'.repeat(30)}`;
		
					Object.keys(teams).forEach(i => {
						if (league && teams[i].leagues.indexOf(league.id) == -1)
							return;
		
						_message += `\n${(i + ' '.repeat(5)).substr(0, 5)}   ${(teams[i].name + ' '.repeat(30)).substr(0, 30)}   ${(teams[i].shortname + ' '.repeat(15)).substr(0, 15)}   ${(teams[i].leagues.map(i => leagues[i].name).join(', ') + ' '.repeat(30)).substr(0, 30)}`;
						none = false;
					});

					if (none)
						_message += `There are no teams available${league ? ' for ' + league.name : ''}`;
		
					message.channel.send(escapeMarkdown(_message), {code: config.help.code, split: config.help.split});
				break;

				case 'watcher':
				case 'watchers':
					type = (tokens[3] || '').trim().toLowerCase();

					if (tokens[5] == 'PSN') {
						tokens[4] += tokens[5];
						[].splice.apply(tokens, [5, 1].concat(tokens.slice(6)));
					}
					
					league = getLeague(tokens[4]);

					if (tokens[4] && !league) {
						message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[4])}" is not a valid league. See ${config.prefix} ${command} help for more information.`);
						break;
					}

					team = getTeam(tokens[5], league);

					if ((type == 'games' || (tokens[5] && !tokens[5].match(/^<\#(\d+)>$/))) && !team) {
						message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[5])}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
						break;
					}

					channel = null;
			
					if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
						channel = message.guild.channels.get(channel[1]);
			
					if (channel === undefined) {
						message.channel.send(`The channel you requested could not be round in your server.`);
						break;
					}

					_message = `${('TYPE' + ' '.repeat(15)).substr(0, 15)}   ${('LEAGUE' + ' '.repeat(15)).substr(0, 15)}   ${('TEAM' + ' '.repeat(30)).substr(0, 30)}   ${('CHANNEL' + ' '.repeat(25)).substr(0, 25)}\n${'-'.repeat(15)}   ${'-'.repeat(15)}   ${'-'.repeat(30)}   ${'-'.repeat(15)}`;
					let types = [];
					
					if (type == 'all' || !type)
						types.push('bids', 'contracts', 'daily-stars', 'draft', 'games', 'news', 'trades', 'waivers');
					else if (/^all.?news$/.test(type))
						types.push('bids', 'contracts', 'draft', 'news', 'trades', 'waivers');
					else
						types.push(type);

					data.watchers.forEach(watcher => {
						if (watcher.guild != message.guild.id || (types.indexOf(watcher.type) == -1) || (league && (watcher.league != league.id)) || (team && (watcher.team != team.id)) || (channel && (watcher.channel != channel.id)))
							return;
						
						let _league = getLeague(watcher.league);
						let _team = getTeam(watcher.team, _league);
						let _channel = message.guild.channels.get(watcher.channel);
						_message += `\n${(watcher.type + ' '.repeat(15)).substr(0, 15)}   ${((_league ? _league.name : 'All') + ' '.repeat(15)).substr(0, 15)}   ${((_team ? _team.name : 'All') + ' '.repeat(30)).substr(0, 30)}   ${(_channel.name + ' '.repeat(15)).substr(0, 15)}`;
						none = false;
					});

					if (none)
						_message += `\nThere are no watchers matching your criteria`;
		
					message.channel.send(escapeMarkdown(_message), {code: config.help.code, split: config.help.split});
				break;
			}
		break;

		case 'ping':
			message.channel.send(`PONG!`);
		break;
	
		case 'unwatch':
			type = (tokens[2] || '').trim().toLowerCase();
	
			if ((!guild && type != 'help') || (guild && guild.admins.indexOf(message.author.id) == -1)) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but you aren't allowed to do that!`);
				break;
			}
	
			if (type == 'help' || !isValidWatcher(type)) {
				fs.readFile(`${__dirname}/help/unwatch.txt`, 'utf8', (err, text) => {
					if ((tokens[3] || '').toLowerCase() == 'me')
						message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
					else
						message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
				});
	
				break;
			}
	
			if (tokens[4] == 'PSN') {
				tokens[3] += tokens[4];
				[].splice.apply(tokens, [4, 1].concat(tokens.slice(5)));
			}
	
			league = getLeague(tokens[3]);

			if (tokens[3] && !league) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[3])}" is not a valid league. See ${config.prefix} ${command} help for more information.`);
				break;
			}

			team = getTeam(tokens[4], league);

			if ((type == 'games' || (tokens[4] && !tokens[4].match(/^<\#(\d+)>$/))) && !team) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[4])}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
				break;
			}

			channel = null;
	
			if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(channel[1]);
	
			if (channel === undefined) {
				message.channel.send(`The channel you requested could not be round in your server.`);
				break;
			}
	
			_message = {channel: '', log: ''};
			oldLength = data.watchers.length;
	
			if (type == 'all') {
				data.watchers = data.watchers.filter(watcher => (watcher.guild != message.guild.id) || (league && watcher.league != league.id) || (team && watcher.team != team.id) || (channel && watcher.channel != channel.id));
				_message.channel = `Ok! You are no longer watching any updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
				_message.log = `${message.author.tag} has stopped watching all events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
			} else if (/^all.?news$/.test(type)) {
				data.watchers = data.watchers.filter(watcher => (watcher.guild != message.guild.id) || (league && watcher.league != league.id) || (team && watcher.team != team.id) || (channel && watcher.channel != channel.id) || !isNewsWatcher(watcher));
				_message.channel = `Ok! You are no longer watching any news updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
				_message.log = `${message.author.tag} has stopped watching all news events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
			} else {
				data.watchers = data.watchers.filter(watcher => (watcher.guild != message.guild.id) || (league && watcher.league != league.id) || (team && watcher.team != team.id) || (channel && watcher.channel != channel.id) || (watcher.type != type));
				_message.channel = `Ok! You are no longer watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`;
				_message.log = `${message.author.tag} has stopped watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`;
			}
	
			if (data.watchers.length != oldLength) {
				if (_message.channel)
					message.channel.send(escapeMarkdown(_message.channel.trim().replace(/ +/g, ' ')));
	
				if (_message.log)
					log(_message.log.trim().replace(/ +/g, ' '));
	
				saveData();
			} else
				message.channel.send(`Ok! I checked over your watcher data, and there was nothing to remove.`);
		break;
	
		case 'watch':
			type = (tokens[2] || '').trim().toLowerCase();
	
			if ((!guild && type != 'help') || (guild && guild.admins.indexOf(message.author.id) == -1)) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but you aren't allowed to do that!`);
				break;
			}
	
			if (type == 'help' || !isValidWatcher(type)) {
				fs.readFile(`${__dirname}/help/watch.txt`, 'utf8', (err, text) => {
					if ((tokens[3] || '').toLowerCase() == 'me')
						message.author.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
					else
						message.channel.send(eval('`' + text + '`'), {code: config.help.code, split: config.help.split});
				});
	
				break;
			}
	
			if (tokens[4] == 'PSN') {
				tokens[3] += tokens[4];
				[].splice.apply(tokens, [4, 1].concat(tokens.slice(5)));
			}
	
			league = getLeague(tokens[3]);
	
			if (!league) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[3])}" is not a valid league. See ${config.prefix} ${command} help for more information.`);
				break;
			}
	
			team = getTeam(tokens[4], league);

			if ((type == 'games' || (tokens[4] && !tokens[4].match(/^<\#(\d+)>$/))) && !team) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but "${escapeMarkdown(tokens[4])}" is not a valid team. See ${config.prefix} ${command} help for more information.`);
				break;
			}
	
			channel = null;
	
			if (channel = tokens[tokens.length - 1].match(/^<\#(\d+)>$/))
				channel = message.guild.channels.get(channel[1]);
	
			if (channel === undefined) {
				message.channel.send(`I'm sorry, ${escapeMarkdown(message.author.username)}, but the channel you requested could not be found in your server.`);
				break;
			} else if (!channel)
				channel = getDefaultChannel(message.guild, guild.defaultChannel);
	
			_message = '';
			oldLength = data.watchers.length;
			let types = [];
	
			if (type == 'all')
				types.push('bids', 'contracts', 'daily-stars', 'draft', 'games', 'news', 'trades', 'waivers');
			else if (/^all.?news$/.test(type))
				types.push('bids', 'contracts', 'draft', 'news', 'trades', 'waivers');
			else
				types.push(type);
	
			types.forEach(type => {
				let watcher = {guild: message.guild.id, channel: channel.id, league: league.id, team: team ? team.id : null, type: type};
				let _watcher = data.watchers.filter(_watcher => (watcher.guild == _watcher.guild) && (watcher.channel == _watcher.channel) && (watcher.league == _watcher.league) && (watcher.team == _watcher.team) && (watcher.type == _watcher.type)).shift();
	
				if (_watcher || (type == 'game' && !watcher.team))
					return;
	
				data.watchers.push(watcher);
				log(`${message.author.tag} has started watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} events${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel ' + message.guild.name + '#' + channel.name : ''}`);
			});
	
			if (oldLength != data.watchers.length) {
				if (type == 'all')
					message.channel.send(escapeMarkdown(`Ok! You are now watching all updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`));
				else if (/^all.?news$/.test(type))
					message.channel.send(escapeMarkdown(`Ok! You are now watching all news updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`));
				else
					message.channel.send(escapeMarkdown(`Ok! You are now watching ${type.replace(/\W+/g, ' ').replace(/([^w])s$/, '$1')} updates${league || team ? ' from the' : ''}${league ? ' ' + league.name : ''}${team ? ' ' + team.name : ''}${channel ? ' in channel #' + channel.name : ''}.`));
	
				saveData();
			} else
				message.channel.send(`Ok! I checked over your watcher data, and there was nothing new to add.`);
		break;
	}
});

client.login(config.token);

/**
 * Set up the cron tasks
 */
const jobs = [
	new cron.CronJob('0  0     */8    *  *  *  ', updateLeagues, null, true, 'America/New_York'),
	new cron.CronJob('0  15    */8    *  *  *  ', updateTeams, null, true, 'America/New_York'),
	new cron.CronJob('0  */10  14-15  *  *  *  ', updateDailyStars, null, true, 'America/New_York'),
	new cron.CronJob('0  */10  0-19   *  *  *  ', updateNews, null, true, 'America/New_York'),
	new cron.CronJob('0  */5   20-23  *  *  *  ', updateNews, null, true, 'America/New_York'),
	new cron.CronJob('0  0     0-19   *  *  *  ', updateSchedules, null, true, 'America/New_York'),
	new cron.CronJob('0  */5   20-23  *  *  0-4', updateSchedules, null, true, 'America/New_York'),
	new cron.CronJob('0  30    0-19   *  *  5-6', updateSchedules, null, true, 'America/New_York')
];

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
	log(`Shutting down ${config.name} v${pkg.version.replace(/^v+/g, '')}...`);
	client.destroy();
});

/**
 * Common functionality for easy reuasbility
 */
function escapeMarkdown(input) {
	return typeof input == 'string' ? input.replace(/[\*\_\~\`]/g, a => `\\${a}`) : input;
}

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

    return guild.channels.filter(channel => (channel.type == 'text') && channel.permissionsFor(client.user).has(Discord.Permissions.FLAGS.READ_MESSAGES)).sort((a, b) => a.calculatedPosition-b.calculatedPosition).first();
}

function getLeague(league) {
	if (!league || !league.toString)
		return;

	let lstring = league.toString().toUpperCase().replace(/[^A-Z0-9]+/g,'');
	return Object.keys(leagues).filter(i => (leagues[i] == league) || (leagues[i].id == league) || (leagues[i].code == lstring) || (leagues[i].name.toUpperCase().replace(/[^A-Z0-9]+/g,'') == lstring)).map(i => leagues[i]).shift();
}

function getTeam(team, league) {
	if (!team || !team.toString)
		return;

	let tstring = team.toString().toUpperCase().replace(/[^A-Z0-9]+/g,'');

	if (league && !(league = getLeague(league)))
		return;

	return Object.keys(teams).filter(i => ((teams[i] == team) || (teams[i].id == team) || (teams[i].name.toUpperCase().replace(/[^A-Z0-9]+/g,'') == tstring) || (teams[i].shortname.toUpperCase().replace(/[^A-Z0-9]+/g,'') == tstring)) && (!league || (teams[i].leagues.indexOf(league.id) != -1))).map(i => teams[i]).shift();
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
	data.watchers = data.watchers.filter(watcher => watcher.guild != guild.id);

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

function ordinal(number) {
	return (number < 11) || (number > 13) ? ['st','nd','rd','th'][Math.min((number - 1) % 10, 3)] : 'th';
}

function saveData() {
	fs.writeFileSync(`${__dirname}/data/data.json`, JSON.stringify(data));
}

function sendDailyStarUpdates(league, stars) {
	if (!(league = getLeague(league)))
		return;

	let watchers = data.watchers.filter(watcher => (!watcher.league || (watcher.league == league.id)) && (watcher.type == 'daily-stars'));

	if (!watchers.length)
		return;

	if (!(stars instanceof Object)) {
		try {
			stars = require(`${__dirname}/data/daily-stars-${league.id}.json`);
		} catch (e) {
			if (!e.message.match(/Cannot find module/i))
				console.error(e.stack);

			return;
		}
	}

	if (!stars || (!stars.forwards && !stars.defenders && !stars.goalies))
		return;

	let output = [];

	watchers.forEach(watcher => {
		let forwards = stars.forwards.filter(star => !watcher.team || (watcher.team == star.team));
		let defenders = stars.defenders.filter(star => !watcher.team || (watcher.team == star.team));
		let goalies = stars.goalies.filter(star => !watcher.team || (watcher.team == star.team));

		if (!(forwards.length + defenders.length + goalies.length))
			return;

		let guild = client.guilds.get(watcher.guild);
		let channel = getDefaultChannel(guild, watcher.channel);

		if (!channel)
			return;

		let team = getTeam(watcher.team, league);
		let message = `**Congratulations to the ${team ? escapeMarkdown((team.name + "'s ").replace(/s's $/, "s' ")) : ''}${escapeMarkdown(league.name)} Daily Stars for ${escapeMarkdown(stars.date)}:**`;

		forwards.forEach(star => message += `\n    * ${tagUser(star.name, guild)} - ${star.rank}${ordinal(star.rank)} Star Forward _(${star.stats[0]} Points, ${star.stats[1]} Goals, ${star.stats[2]} Assists, ${star.stats[3] > 0 ? '+' : (star.stats[3] < 0 ? '-' : '')}${star.stats[3]})_`);

		if (forwards.length)
			message += `\n`;

		defenders.forEach(star => message += `\n    * ${tagUser(star.name, guild)} - ${star.rank}${ordinal(star.rank)} Star Forward _(${star.stats[0]} Points, ${star.stats[1]} Goals, ${star.stats[2]} Assists, ${star.stats[3] > 0 ? '+' : (star.stats[3] < 0 ? '-' : '')}${star.stats[3]})_`);

		if (defenders.length)
			message += `\n`;

		goalies.forEach(star => message += `\n    * ${tagUser(star.name, guild)} - ${star.rank}${ordinal(star.rank)} Star Goalie _(${(star.stats[0] / 100).toFixed(3)} SV%, ${star.stats[1].toFixed(2)} GAA, ${star.stats[2]} Shots, ${star.stats[3]} Saves)_`);
		output.push([channel, message.trim()]);
	});

	output.filter((value, index, array) => array.indexOf(value) == index).forEach(data => {
		let [channel, message] = data;
		log(`Sending message to ${channel.guild.name}#${channel.name}: ${message.replace(/\n/g, '\\n')}`);
		channel.send(message)
	});
}

function sendNewsUpdates(league, news) {
	if (!(league = getLeague(league)))
		return;

	let watchers = data.watchers.filter(watcher => (!watcher.league || (watcher.league == league.id)) && isNewsWatcher(watcher));

	if (!watchers.length)
		return;

	let path = `${__dirname}/data/news-${league.id}.json`, update = false;

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

		if (item.type == 'bid' && (data = item.message.match(/have earned the player rights for (.*?) with a bid amount of (\S+)/i))) {
			let player = {name: data[1].trim()}, bid = {amount: data[2].trim()}, team = getTeam(item.teams[0], league);
			message = `The ${team.name} have won bidding rights to ${player.name} with a bid of ${bid.amount}!`;
		} else if (item.type == 'contract' && (data = item.message.match(/^(.*?) and the .*? have agreed to a (\d+) season deal at (.*?) per season$/i))) {
			let player = {name: data[1].trim()}, contract = {length: data[2].trim(), salary: data[3].trim()}, team = getTeam(item.teams[0], league);
			message = `The ${team.name} have signed ${player.name} to a ${contract.length} season contract worth ${contract.salary} per season!`;
		} else if (/^(draft|trade|waiver)$/.test(item.type) || (!item.type && item.message.match(/have (been eliminated|claimed|clinched|drafted|placed|traded)/i)))
			message = item.message;

		if (!message)
			return;

		let channels = [];

		watchers.filter(watcher => (!watcher.team || (item.teams.indexOf(watcher.team) != -1)) && (watcher.type.replace(/s$/, '') == item.type)).forEach(watcher => {
			let guild = client.guilds.get(watcher.guild);
			let channel = getDefaultChannel(guild, watcher.channel);

			if (channel)
				channels.push(channel);
		});

		channels.filter((value, index, array) => array.indexOf(value) == index).forEach(channel => {
			log(`Sending message to ${channel.guild.name}#${channel.name}: ${message}`);

			if (item.type == 'bid')
				message = message.replace(/rights to (.*?) with a/, (a, b) => `rights to ${tagUser(b, channel.guild)} with a`);
			else if (item.type == 'contract')
				message = message.replace(/have signed (.*?) to a/, (a, b) => `have signed ${tagUser(b, channel.guild)} to a`);
			else if (item.type == 'draft')
				message = message.replace(/have drafted (.*?) (\d+\w+) overall/, (a, b, c) => `have drafted ${tagUser(b, channel.guild)} ${c} overall`);

			channel.send(escapeMarkdown(message))
		});
	});

	if (update)
		fs.writeFileSync(path, JSON.stringify(news));
}

function sendScheduleUpdates(league, team, schedule) {
	if (!(league = getLeague(league)) || !(team = getTeam(team, league)))
		return;

	let watchers = data.watchers.filter(watcher => (!watcher.league || watcher.league == league.id) && (!watcher.team || watcher.team == team.id) && (watcher.type == 'games'));

	if (!watchers.length)
		return;

	let path = `${__dirname}/data/schedule-${league.id}-${team.id}.json`, update = false;

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

		let output = [];

		watchers.forEach(w => {
			let guild = client.guilds.get(w.guild), channel = getDefaultChannel(guild, w.channel), us, them;

			if (!channel)
				return;

			if (game.home.id == w.team) {
				us = game.home;
				them = game.visitor;
			} else {
				us = game.visitor;
				them = game.home;
			}

			if (us.score > them.score)
				output.push([channel, `The **${escapeMarkdown(us.name)}** have defeated the **${escapeMarkdown(them.name)}** by the score of **${us.score} to ${them.score}**!`]);
			else if (us.score < them.score)
				output.push([channel, `The _${escapeMarkdown(us.name)}_ have been defeated by the _${escapeMarkdown(them.name)}_ by the score of _${them.score} to ${us.score}_.`]);
			else
				output.push([channel, `The ${escapeMarkdown(us.name)} have tied the ${escapeMarkdown(them.name)} by the score of ${us.score} to ${them.score}.`]);
		});

		output.filter((value, index, array) => array.indexOf(value) == index).forEach(out => {
			let [channel, message] = out;
			log(`Sending message to ${channel.guild.name}#${channel.name}: ${message}`);
			channel.send(message)
		});
	});

	if (update)
		fs.writeFileSync(path, JSON.stringify(schedule));
}

function tagUser(input, guild) {
	let user;

	if (input instanceof Discord.GuildMember)
		user = input.user;

	if (!user && guild instanceof Discord.Guild) {
		let regex = new RegExp(input, 'i');
		let member = guild.members.find(member => (member.nickname && member.nickname.match(regex)) || member.user.username.match(regex));

		if (member)
			user = member.user;
	}

	return user ? `<@${user.id}>` : escapeMarkdown(input);
}

function tokenize(string) {
	return string.replace(/(["'])((?:(?=(\\?))\3.)*?)\1/g, (a,b,c) => {return c.replace(/\s/g, '\037')}).split(/\s+/).map(token => {return token.replace(/\037/g, ' ')});
}

function updateDailyStars() {
	if (updateDailyStars.$running)
		return;

	child.fork(`${__dirname}/scripts/update_daily_stars.js`, {silent: true})
		.on('message', message => {sendDailyStarUpdates.apply(null,message)})
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
		.on('message', message => {sendNewsUpdates.apply(null,message)})
		.on('exit', () => {delete updateNews.$running});
}

function updateSchedules() {
	if (updateSchedules.$running)
		return;

	child.fork(`${__dirname}/scripts/update_schedules.js`, {silent: true})
		.on('message', message => {sendScheduleUpdates.apply(null,message)})
		.on('exit', () => {delete updateSchedules.$running});
}

function updateTeams() {
	if (updateTeams.$running)
		return;

	child.fork(`${__dirname}/scripts/update_teams.js`, {silent: true})
		.on('message', message => {teams=message})
		.on('exit', () => {delete updateTeams.$running});
}