const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const path = `${dir}/data/leagues.json`;
let leagues = {};

function updateLeagues() {
	leagues = require(path);

	Promise.all(
		Object.keys(leagues).map(id => {
			return new Promise(resolve => {
				let league = leagues[id] || (leagues[id] = {});
				let regex0 = new RegExp(`<li(?:[^>]+)? custom-tab-${league.id} (?:[^>]+)?><a(?:[^>]+)?/league\\.(\\d+)/(?:[^>]+)?>.*?<span(?:[^>]+)?>(.*?)</span></a>`, 'i');
				let regex1 = new RegExp(`<a(?:[^>]+)?leagueid=${league.id}&(?:amp;)?seasonid=(\\d+)(?:[^>]+)?>Standings</a>`, 'i');
	
				request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=standing&leagueid=${id}&seasonid=1`, (err, res, html) => {
					if (!err) {
						if (res.statusCode != 200)
							err = new Error(`Failed to fetch information for league ${id} (status=${res.statusCode})`);
						else if (!/^text\/html/.test(res.headers['content-type']))
							err = new Error(`Failed to fetch information for league ${id} (content-type=${res.headers['content-type']})`);
					}
	
					if (err) {
						console.error(err.message);
						return resolve();
					}
	
					html = html.replace(/>\s+</g, '><');
					let data;
	
					if (data = html.match(regex0)) {
						league.code = data[2].trim().toUpperCase().replace(/[^A-Z0-9]/g, '');
						league.forum = parseInt(data[1]);
						league.id = parseInt(id);
						league.name = data[2].trim();
	
						if (data = html.match(regex1))
							league.season = parseInt(data[1]);
					}
	
					resolve();
				});
			});
		})
	).then(() => {
		fs.readFile(path, 'utf8', (err, json) => {
			if (err) {
				console.error(err.message);
				process.exit();
			}

			var data = JSON.stringify(leagues);

			if (json == data)
				process.exit();

			fs.writeFile(path, data, err => {
				if (err)
					console.error(err.message);

				process.exit();
			});
		});
	});
}

fs.stat(path, err => {
	if (err)
		fs.writeFile(path, '{}', updateLeagues);
	else
		updateLeagues();
});