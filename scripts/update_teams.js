const fs = require('fs');
const http = require('http');
const url = require('url');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const leagues = require(`${dir}/data/leagues.json`);
const path = `${dir}/data/teams.json`;
let teams = {}, tmap = {};

function save() {
	fs.readFile(path, 'utf8', (err, json) => {
		if (err) {
			console.error(err.message);
			return;
		}

		Object.keys(teams).forEach(id => {teams[id].leagues=tmap[id].filter((l,i) => {return tmap[id].indexOf(l)==i}).sort()});
		let data = JSON.stringify(teams);
		if (json == data) process.exit();

		fs.writeFile(path, data, err => {
			if (err) console.error(err.message);
			process.exit();
		});
	});
}

fs.stat(path, (err, stats) => {
	if (err) fs.writeFileSync(path, '{}');
	let ids = Object.keys(leagues), total = ids.length, finished = 0;

	ids.forEach(function(id) {
		let league = leagues[id], regex = new RegExp(`<div(?:[^>]+)?class="team_box_icon"(?:[^>]+)?>.*?<a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?>(.*?)</a></div>`, 'ig'), regex1 = new RegExp(`<td(?:[^>]+)?><img(?:[^>]+)?/team\\d+.png(?:[^>]+)?> \\d+\\) <a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?>(.*?)</a></td>`, 'ig');

		http.get(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=standing&leagueid=${id}&seasonid=${league.season}`, res => {
			const { statusCode } = res;
			const contentType = res.headers['content-type'];
			let err;

			if (statusCode !== 200)
				err = new Error(`Failed to fetch team list for league ${id} (status code=${statusCode})`);
			else if (!/^text\/html/.test(contentType))
				err = new Error(`Failed to fetch team list for league ${id} (content-type=${contentType})`);

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
					for (let i = 0, end = data.length; i < end; i++) {
						let [a, link, name] = data[i].match(/<a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a>/);
						link = url.parse(link, true).query;

						let id = parseInt(link.teamid);
						teams[id] = teams[id] || {id: id, leagues: [], name: name.trim(), shortname: null};
						tmap[id] = (tmap[id] || []).concat(parseInt(link.leagueid));
					}
				}

				if (data = html.match(regex1)) {
					for (let i = 0, end = data.length; i < end; i++) {
						let [a, link, name] = data[i].match(/<a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a>/);
						link = url.parse(link, true).query;

						let id = parseInt(link.teamid);

						if (teams[id]) {
							teams[id].shortname = teams[id].shortname || name.trim();
							tmap[id] = (tmap[id] || []).concat(parseInt(link.leagueid));
						}
					}
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
