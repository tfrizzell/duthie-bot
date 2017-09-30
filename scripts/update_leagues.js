const fs = require('fs');
const http = require('http');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const path = `${dir}/data/leagues.json`;
let leagues = {};

function save() {
	fs.readFile(path, 'utf8', (err, json) => {
		if (err) {
			console.error(err.message);
			return;
		}

		var data = JSON.stringify(leagues);
		if (json == data) process.exit();

		fs.writeFile(path, data, err => {
			if (err) console.error(err.message);
			process.exit();
		});
	});
}

fs.stat(path, (err, stats) => {
	if (err) fs.writeFileSync(path, '{}');
	leagues = require(path);
	let ids = Object.keys(leagues), total = ids.length, finished = 0;

	ids.forEach(function(id) {
		let league = leagues[id], regex = new RegExp(`<li(?:[^>]+)? custom-tab-${league.id} (?:[^>]+)?>(?:\\s+)?<a(?:[^>]+)?/league\\.(\\d+)/(?:[^>]+)?>.*?<span(?:[^>]+)?>(.*?)</span></a>`, 'i'), regex1 = new RegExp(`<a(?:[^>]+)?leagueid=${league.id}&(?:amp;)?seasonid=(\\d+)(?:[^>]+)?>Standings</a>`, 'i');

		http.get(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=standing&leagueid=${id}&seasonid=1`, res => {
			const { statusCode } = res;
			const contentType = res.headers['content-type'];
			let err;

			if (statusCode !== 200)
				err = new Error(`Failed to fetch information for league ${id} (status code=${statusCode})`);
			else if (!/^text\/html/.test(contentType))
				err = new Error(`Failed to fetch information for league ${id} (content-type=${contentType})`);

			if (err) {
				console.error(err.message);
				res.resume();
				return;
			}

			let html = '';
			res.setEncoding('utf8');
			res.on('data', chunk => { html += chunk; });
			res.on('end', () => {
				let data;

				if (data = html.match(regex)) {
					leagues[id].code = data[2].trim().toUpperCase().replace(/[^A-Z0-9]/g, '');
					leagues[id].forum = parseInt(data[1]);
					leagues[id].id = parseInt(id);
					leagues[id].name = data[2].trim();

					if (data = html.match(regex1))
						leagues[id].season = parseInt(data[1]);
				}

				if (++finished >= total)
					save();
			});
		}).on('error', err => {
			console.error(err.message);

			if (++finished >= total)
				save();
		});
	});
});
