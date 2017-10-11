const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const pkg = require(`${dir}/package.json`);

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

const prefix = 'schedule-';
const path = `${dir}/data/${prefix}${league.id}-${team.id}.json`;
console.log(`Downloading schedule for S${league.season} ${league.name} ${team.name}`);

function downloadSchedule() {
	let regex0 = new RegExp(`<tr(?:[^>]+)?><td(?:[^>]+)?>(.*?)</td><td(?:[^>]+)?>.*?</td><td(?:[^>]+)?><a(?:[^>]+)?page=game&(?:amp;)?gameid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))? @ <img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>(.*?)(?: - (\\d+))?</a></td></tr>`, 'ig');
	let regex1 = new RegExp(regex0.source, regex0.flags.replace('g', ''));

	request({
		url: `http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league_page&page=team_page_schedule&teamid=${team.id}&leagueid=${league.id}&seasonid=${league.season}`,
		headers: {'User-Agent': `${pkg.name}/${pkg.version.replace(/^v+/g,'')}`}
	}, (err, res, html) => {
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

		let oldSchedule = require(path);
		let newSchedule = [];
		let update = false;
		let updating = !!oldSchedule.length;

		for (let i = 0, end = data.length; i < end; i++) {
			let gameData = data[i].match(regex1);

			if (!gameData)
				continue;

			let game = {date: gameData[1].replace(' -  ', ' '), home: {id: parseInt(gameData[6]), name: gameData[7].trim(), score: !isNaN(gameData[8]) ? parseInt(gameData[8]) : null}, id: parseInt(gameData[2]), updated: updating, visitor: {id: parseInt(gameData[3]), name: gameData[4].trim(), score: !isNaN(gameData[5]) ? parseInt(gameData[5]) : null}};
			let oldGame;

			if (oldGame = oldSchedule.filter(old => old.id == game.id).shift())
				game.updated = oldGame.updated || (oldGame.date != game.date) || (oldGame.home.id != game.home.id) || (oldGame.home.score != game.home.score) || (oldGame.visitor.id != game.visitor.id) || (oldGame.visitor.score != game.visitor.score);

			newSchedule.push(game);
			update = update || !updating || game.updated;
		}

		if (!update)
			process.exit();

		fs.writeFile(path, JSON.stringify(newSchedule), err => {
			if (err)
				console.error(err.message);
			else if (process.send)
				process.send([league.id, team.id, newSchedule], () => process.exit());

			if (!process.send)
				process.exit();
		 });
	});
}

fs.stat(path, err => {
	if (err)
		fs.writeFile(path, '[]', downloadSchedule);
	else
		downloadSchedule();
});