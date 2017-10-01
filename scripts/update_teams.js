const fs = require('fs');
const request = require('request');
const url = require('url');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const leagues = require(`${dir}/data/leagues.json`);
const path = `${dir}/data/teams.json`;

function save() {
	fs.readFile(path, 'utf8', (err, json) => {
		if (err) {
			console.error(err.message);
			process.exit();
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
	let teams = {}, lmap = {}, ids = Object.keys(leagues), total = ids.length, finished = 0;

	ids.forEach(function(id) {
		let league = leagues[id], regex = new RegExp(`<div(?:[^>]+)?class="team_box_icon"(?:[^>]+)?>.*?<a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?>(.*?)</a></div>`, 'ig'), regex1 = new RegExp(`<td(?:[^>]+)?><img(?:[^>]+)?/team\\d+.png(?:[^>]+)?> \\d+\\) <a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${league.id}&(?:amp;)?seasonid=${league.season}(?:[^>]+)?>(.*?)</a></td>`, 'ig');

		request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=standing&leagueid=${id}&seasonid=${league.season}`, (err, res, html) => {
			html = html.replace(/>\s+</g, '><');
			let contentType = res.headers['content-type'];

			if (res.statusCode != 200)
				err = new Error(`Failed to fetch team list for ${league.name} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(contentType))
				err = new Error(`Failed to fetch team list for ${league.name} (content-type=${contentType})`);

			if (!err) {
				let data;

				if (data = html.match(regex)) {
					for (let i = 0, end = data.length; i < end; i++) {
						let [a, link, name] = data[i].match(/<a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a>/);
						link = url.parse(link, true).query;

						let id = parseInt(link.teamid);
						teams[id] = teams[id] || {id: id, leagues: [], name: name.trim(), shortname: null};
						lmap[id] = (lmap[id] || []).concat(parseInt(link.leagueid));
					}
				}

				if (data = html.match(regex1)) {
					for (let i = 0, end = data.length; i < end; i++) {
						let [a, link, name] = data[i].match(/<a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a>/);
						link = url.parse(link, true).query;

						let id = parseInt(link.teamid);

						if (teams[id]) {
							teams[id].shortname = teams[id].shortname || name.trim();
							lmap[id] = (lmap[id] || []).concat(parseInt(link.leagueid));
						}
					}
				}
			} else
				console.error(err.message);

			if (++finished < total) return;

			fs.readFile(path, 'utf8', (err, json) => {
				if (err) {
					console.error(err.message);
					process.exit();
				}

				Object.keys(teams).forEach(id => {teams[id].leagues=lmap[id].filter((v,i,a) => {return a.indexOf(v)==i}).sort()});
				let data = JSON.stringify(teams);
				if (json == data) process.exit();

				fs.writeFile(path, data, err => {
					if (err) console.error(err.message);
					process.exit();
				});
			});
		});
	});
});