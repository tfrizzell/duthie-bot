const child = require('child_process');
const fs = require('fs');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const prefix = 'schedule-';
let watched = {};

const data = require(`${dir}/data/data.json`);
const leagues = require(`${dir}/data/leagues.json`);
const teams = require(`${dir}/data/teams.json`);

Promise.all(
	data.watchers.map(watcher => {
		return new Promise(resolve => {
			if (watcher.type != 'games' || (watched[watcher.league] && watched[watcher.league][watcher.team]))
				return resolve();

			if (!watched[watcher.league])
				watched[watcher.league] = {};

			watched[watcher.league][watcher.team] = true;

			fs.stat(`${dir}/data/${prefix}${watcher.league}-${watcher.team}.json`, err => {
				if (!err)
					return resolve();

				fs.writeFile(err.path, '[]', err => {
					if (!err)
						return resolve();

					console.error(err.message);
					process.exit();
				});
			});
		});
	})
).then(() => {
	fs.readdir(`${dir}/data`, (err, files) => {
		if (err) {
			console.error(err.message);
			process.exit();
		}

		let regex = new RegExp(`^${prefix}(\\d+)-(\\d+).json$`);

		Promise.all(
			files.map(file => {
				return new Promise(resolve => {
					if (!file.match(regex))
						return resolve();

					let [a, league, team] = file.match(regex);

					if (!(league = leagues[league]) || !(team = teams[team]))
						return resolve();

					if (!watched[league.id][team.id])
						return fs.unlink(`${dir}/data/${file}`, resolve);

					child.fork(`${dir}/scripts/download_schedule.js`, [league.id, team.id])
						.on('message', message => {
							if (process.send)
								process.send(message);
						})
						.on('exit', resolve);
				});
			})
		).then(() => {
			process.exit();
		});
	});
});