const child = require('child_process');
const fs = require('fs');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const data = require(`${dir}/data/data.json`);
const leagues = require(`${dir}/data/leagues.json`);
const prefix = 'news-';
const teams = require(`${dir}/data/teams.json`);

let watched = {};
data.watchers.forEach(w => {
	if (!/^(bids|contracts|draft|news|trades|waivers)$/.test(w.type)) return;
	watched[w.league] = watched[w.league] || {};
	watched[w.league][w.team] = true;
	try { fs.statSync(`${dir}/data/${prefix}${w.league}.json`); } catch (err) { fs.writeFileSync(`${dir}/data/${prefix}${w.league}.json`, '[]'); }
});

fs.readdir(`${dir}/data`, (err, files) => {
	if (err) {
		console.log(err.message);
		process.exit();
	}

	let total = 0, finished = 0, regex = new RegExp(`^${prefix}(\\d+)(-(\\d+))?.json$`);

	files.filter(function(f) {
		return f.match(regex);
	}).forEach(function(f) {
		let [prefix, league, team] = f.replace(/\.json$/, '').split(/[-\.]/);

		if (!(league = leagues[league]) || (team && !(team = teams[team])))
			return;

		if (!watched[league.id])
			return fs.unlink(`${dir}/data/${prefix}${league.id}.json`);

		if (team && !watched[league.id][team.id])
			return fs.unlink(`${dir}/data/${prefix}${league.id}-${team.id}.json`);

		child.fork(`${dir}/scripts/download_news.js`, [league.id, team ? team.id : '']).on('exit', () => { if (++finished >= total) process.exit(); });
		total++;
	});
});