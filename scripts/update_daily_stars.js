const child = require('child_process');
const fs = require('fs');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const prefix = 'daily-stars-';

const data = require(`${dir}/data/data.json`);
const leagues = require(`${dir}/data/leagues.json`);
let watched = {};

Promise.all(
	data.watchers.map(watcher => {
		return new Promise(resolve => {
			if (watcher.type != 'daily-stars' || watched[watcher.league])
				return resolve();
	
			watched[watcher.league] = true;
	
			fs.stat(`${dir}/data/${prefix}${watcher.league}.json`, err => {
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

		let regex = new RegExp(`^${prefix}(\\d+).json$`);

		Promise.all(
			files.map(file => {
				return new Promise(resolve => {
					if (!file.match(regex))
						return resolve();

					let [a, league] = file.match(regex);
			
					if (!(league = leagues[league]))
						return resolve();
	
					if (!watched[league.id])
						return fs.unlink(`${dir}/data/${file}`, resolve);
			
					child.fork(`${dir}/scripts/download_daily_stars.js`, [league.id]).on('exit', resolve);
				})
			})
		).then(() => {
			process.exit();
		});
	});
});