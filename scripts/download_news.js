const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const prefix = 'news-';

const leagues = require(`${dir}/data/leagues.json`);
const teams = require(`${dir}/data/teams.json`);
let [league] = process.argv.slice(2);

if (!leagues[league]) {
	console.error(`Invalid league: ${league}`);
	process.exit();
} else
	league = leagues[league];

const path = `${dir}/data/${prefix}${league.id}.json`;
console.log(`Downloading news for S${league.season} ${league.name}...`);

function downloadNews() {
	let regex0 = new RegExp('<li(?:[^>]+)? NewsFeedItem(?:[^>]+)?>(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/(?:feed|icons?)/(.*?).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>)?</a><div(?:[^>]+)?><h3(?:[^>]+)?>(.*?)</h3><abbr(?:[^>]+)?>(.*?)</abbr></div></li>', 'ig'),
	    regex1 = new RegExp(regex0.source, regex0.flags.replace('g', ''));

	request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=team_news&teamid=0&typeid=0&displaylimit=500&leagueid=${league.id}&seasonid=${league.season}`, (err, res, html) => {
		if (!err) {
			if (res.statusCode != 200)
				err = new Error(`Failed to fetch news for S${league.season} ${league.name} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(res.headers['content-type']))
				err = new Error(`Failed to fetch news for S${league.season} ${league.name} (content-type=${res.headers['content-type']})`);
		}

		if (err) {
			console.error(err.message);
			return resolve();
		}

		let data = html.replace(/>\s+</g, '><').match(regex0);

		if (!data)
			process.exit();

		let oldItems = require(path), newItems = [], update = false, updating = !!oldItems.length;

		for (let i = 0, end = data.length; i < end; i++) {
			let itemData = data[i].match(regex1);

			if (!itemData)
				continue;

			let item = {league: league.id, message: '', new: updating, teams: [], timestamp: itemData[5].trim(), type: (itemData[2] || '').trim()}, oldItem, team;

			[1, 3].forEach(i => {
				if ((team = teams[itemData[i]]) && team.leagues.indexOf(league.id) != -1)
					item.teams.push(team.id);
			});

			item.message = itemData[4].replace(/<img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?><span(?:[^>]+)?>(.*?)<\/span>/g, (a, b) => {
				if (team = teams[b]) {
					if (team.leagues.indexOf(league.id) != -1)
						item.teams.push(team.id);

					return team.name;
				} else
					return a;
			}).replace(/<(?:[^>]+)?>/g, '').replace(/[ ]+/g, ' ').trim();

			item.teams = item.teams.filter((v,i,a) => {return a.indexOf(v)==i}).sort();

			if (item.message.match(/ have (placed .*? on|claimed .*? off of) waivers /i))
				item.type = 'waiver';
			else if (item.message.match(/ have traded /i))
				item.type = 'trade'
			else if (/^arrow\d+$/.test(item.type))
				item.type = 'roster';
			else if (item.message.match(/ have drafted /i))
				item.type = 'draft';
			else if (item.message.match(/ have agreed to a \d+ season deal /i))
				item.type = 'contract';
			else if (item.message.match(/ have earned the player rights for .*? with a bid /i))
				item.type = 'bid';
			else if (!item.type)
				item.type = '';

			/**************************** workaround *****************************/
			/** This is a temporary workaround until sportscentre.js is updated **/
			if (!item.type)
				item.type = 'news';

			if (/^(?!draft|roster).*?[^s]$/.test(item.type))
				item.type += 's';
			/**************************** /workaround ****************************/

			if (oldItem = oldItems.filter(i => {return (i.league==item.league) && (i.message==item.message) && (i.teams.toString()==item.teams.toString()) && (i.timestamp==item.timestamp) && (i.type==item.type)}).pop())
				item.new = oldItem.new;

			newItems.unshift(item);
			update = update || item.new;
		}

		if (!update)
			process.exit();

		fs.writeFile(path, JSON.stringify(newItems), err => {
			if (err)
				console.error(err.message);
			else if (process.send)
				process.send([league.id, newItems], () => {process.exit()});

			if (!process.send)
				process.exit();
		});
	});
}

fs.stat(path, err => {
	if (err)
		fs.writeFile(path, '[]', downloadNews);
	else
		downloadNews();
});