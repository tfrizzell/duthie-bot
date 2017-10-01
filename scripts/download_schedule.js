const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const leagues = require(`${dir}/data/leagues.json`);
const teams = require(`${dir}/data/teams.json`);
let [league, team] = process.argv.slice(2);

if (!leagues[league]) {
	console.error(`Invalid league: ${league}`);
	process.exit();
} else
	league = leagues[league];

if (!teams[team]) {
	console.error(`Invalid team: ${team}`);
	process.exit();
} else
	team = teams[team];

const file = `${dir}/data/schedule-${league.id}-${team.id}.json`;
console.log(`Downloading schedule for S${league.season} ${league.name} ${team.name}`);

fs.stat(file, (err, stats) => {
	if (err) fs.writeFileSync(file, '{}');

	request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league_page&page=team_page_schedule&teamid=${team.id}&leagueid=${league.id}&seasonid=${league.season}`, (err, res, html) => {
		html = html.replace(/>\s+</g, '><');
		let contentType = res.headers['content-type'];

		if (res.statusCode != 200)
			err = new Error(`Failed to fetch schedule for S${league.season} ${league.name} ${team.name} (status=${res.statusCode})`);
		else if (!/^text\/html/.test(contentType))
			err = new Error(`Failed to fetch schedule for S${league.season} ${league.name} ${team.name} (content-type=${contentType})`);

		if (err) {
			console.error(err.message);
			process.exit();
		}

		let regex = new RegExp(`<tr(?:[^>]+)?><td(?:[^>]+)?>(.*?)</td><td(?:[^>]+)?>.*?</td><td(?:[^>]+)?><a(?:[^>]+)?page=game&(?:amp;)?gameid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))? @ <img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))?</a></td></tr>`, 'ig'), data;
		if (!(data = html.match(regex))) process.exit();

		let prev = require(file), next = {}, regex1 = new RegExp(regex.source, regex.flags.replace('g', ''));

		for (let i = 0, end = data.length; i < end; i++) {
			let game = data[i].match(regex1);

			if (game) {
				let id = parseInt(game[2]);
				next[id] = {date: game[1].replace(' -  ', ' '), home: {id: parseInt(game[6]), name: game[7].trim(), score: !isNaN(game[8]) ? game[8] : null}, id: id, visitor: {id: parseInt(game[3]), name: game[4].trim(), score: !isNaN(game[5]) ? game[5] : null}};

				if (prev[id]) {
					delete prev[id].updated;
					next[id].updated = (JSON.stringify(prev[id]) != JSON.stringify(next[id]));
				} else
					next[id].updated = !!Object.keys(prev).length;
			}
		}

		fs.readFile(file, 'utf8', (err, json) => {
			if (err) {
				console.error(err.message);
				process.exit();
			}

			let data = JSON.stringify(next);
			if (json == data) process.exit();

			fs.writeFile(file, data, err => {
				if (err) console.error(err.message);
				process.exit();
			 });
		});
	});
});