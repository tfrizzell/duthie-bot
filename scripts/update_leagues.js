const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const path = `${dir}/data/leagues.json`;

fs.stat(path, (err, stats) => {
	if (err) fs.writeFileSync(path, '{}');
	let leagues = require(path), ids = Object.keys(leagues), total = ids.length, finished = 0;

	ids.forEach(function(id) {
		let league = leagues[id], regex = new RegExp(`<li(?:[^>]+)? custom-tab-${league.id} (?:[^>]+)?><a(?:[^>]+)?/league\\.(\\d+)/(?:[^>]+)?>.*?<span(?:[^>]+)?>(.*?)</span></a>`, 'i'), regex1 = new RegExp(`<a(?:[^>]+)?leagueid=${league.id}&(?:amp;)?seasonid=(\\d+)(?:[^>]+)?>Standings</a>`, 'i');

		request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=standing&leagueid=${id}&seasonid=1`, (err, res, html) => {
			html = html.replace(/>\s+</g, '><');
			let contentType = res.headers['content-type'];

			if (res.statusCode != 200)
				err = new Error(`Failed to fetch information for league ${id} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(contentType))
				err = new Error(`Failed to fetch information for league ${id} (content-type=${contentType})`);

			if (!err) {
				let data;

				if (data = html.match(regex)) {
					leagues[id].code = data[2].trim().toUpperCase().replace(/[^A-Z0-9]/g, '');
					leagues[id].forum = parseInt(data[1]);
					leagues[id].id = parseInt(id);
					leagues[id].name = data[2].trim();

					if (data = html.match(regex1))
						leagues[id].season = parseInt(data[1]);
				}
			} else
				console.error(err.message);

			if (++finished < total) return;

			fs.readFile(path, 'utf8', (err, json) => {
				if (err) {
					console.error(err.message);
					process.exit();
				}

				var data = JSON.stringify(leagues);
				if (json == data) process.exit();

				fs.writeFile(path, data, err => {
					if (err) console.error(err.message);
					process.exit();
				});
			});
		});
	});
});