const child = require('child_process');
const fs = require('fs');
const http = require('http');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const data = require(`${dir}/data/data.json`);
const leagues = require(`${dir}/data/leagues.json`);
const teams = require(`${dir}/data/teams.json`);

let watched = {};
data.watchers.forEach(w => {
	if (w.type != 'games') return;
	watched[w.league] = watched[w.league] || {};
	watched[w.league][w.team] = true;
	try { fs.statSync(`${dir}/data/schedule-${w.league}-${w.team}.json`); } catch (err) { fs.writeFileSync(`${dir}/data/schedule-${w.league}-${w.team}.json`, '{}'); }
});

fs.readdir(`${dir}/data`, (err, files) => {
	if (err) {
		console.log(err.message);
		process.exit();
	}

	let total = 0, finished = 0;

	files = files.filter(function(f) {
		return f.match(/^schedule-(\d+)-(\d+).json$/)
	}).forEach(function(f) {
		let [prefix, league, team] = f.replace(/\.json$/, '').split(/[-\.]/);

		if (!(league = leagues[league]) || (team && !(team = teams[team])))
			return;

		if (!watched[league.id])
			return fs.unlink(`${dir}/data/schedule-${league.id}.json`);

		if (team && !watched[league.id][team.id])
			return fs.unlink(`${dir}/data/schedule-${league.id}-${team.id}.json`);

		child.fork(`${dir}/scripts/download_schedule.js`, [league.id, team ? team.id : '']).on('exit', () => { if (++finished >= total) process.exit(); });
		total++;
	});
});

