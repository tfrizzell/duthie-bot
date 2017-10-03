const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const leagues = require(`${dir}/data/leagues.json`);
const prefix = 'schedule-';
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

const path = `${dir}/data/${prefix}${league.id}-${team.id}.json`;
console.log(`Downloading schedule for S${league.season} ${league.name} ${team.name}`);

function downloadSchedule() {
	let regex0 = new RegExp(`<tr(?:[^>]+)?><td(?:[^>]+)?>(.*?)</td><td(?:[^>]+)?>.*?</td><td(?:[^>]+)?><a(?:[^>]+)?page=game&(?:amp;)?gameid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))? @ <img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))?</a></td></tr>`, 'ig');
	let regex1 = new RegExp(regex0.source, regex0.flags.replace('g', ''));

	request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league_page&page=team_page_schedule&teamid=${team.id}&leagueid=${league.id}&seasonid=${league.season}`, (err, res, html) => {
		if (!err) {
			if (res.statusCode != 200)
				err = new Error(`Failed to fetch schedule for S${league.season} ${league.name} ${team.name} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(res.headers['content-type']))
				err = new Error(`Failed to fetch schedule for S${league.season} ${league.name} ${team.name} (content-type=${res.headers['content-type']})`);
		}

		if (err) {
			console.error(err.message);
			return resolve();
		}

		let data = html.replace(/>\s+</g, '><').match(regex0);

		if (!data)
			process.exit();

		let oldSchedule = require(path), newSchedule = {}, updated = false, updating = !!Object.keys(oldSchedule).length;

		for (let i = 0, end = data.length; i < end; i++) {
			let gameData = data[i].match(regex1);

			if (!gameData)
				continue;

			let game = {date: gameData[1].replace(' -  ', ''), home: {id: parseInt(gameData[6]), name: gameData[7].trim(), score: !isNaN(gameData[8]) ? parseInt(gameData[8]) : null}, id: parseInt(gameData[2]), updated: false, visitor: {id: parseInt(gameData[3]), name: gameData[4].trim(), score: !isNaN(gameData[5]) ? parseInt(gameData[5]) : null}};

			if (oldSchedule[game.id])
				game.updated = JSON.stringify(oldSchedule[game.id]).replace(/,"updated":.*?/, '') != JSON.stringify(game).replace(/,"updated":.*?/, '');
			else
				game.updated = updating;

			newSchedule[game.id] = game;
			udpates = updated || game.updated;
		}

		if (!updated)
			process.exit();

		fs.writeFile(path, JSON.stringify(newSchedule), err => {
			if (err)
				console.error(err.message);

			process.exit();
		 });
	});
}

fs.stat(path, err => {
	if (err)
		fs.writeFile(path, '{}', downloadSchedule);
	else
		downloadSchedule();
});