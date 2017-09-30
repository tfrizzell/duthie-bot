const fs = require('fs');
const http = require('http');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const leagues = require(`${dir}/data/leagues.json`);
const teams = require(`${dir}/data/teams.json`);
let [league, team] = process.argv.slice(2);

if (!leagues[league]) {
	console.error(`Invalid league: ${league}`);
	process.exit();
} else
	league = leagues[league];

if (teams[team])
	team = teams[team];
else
	team = null;

const file = `${dir}/data/news-${league.id}${team ? '-' + team.id : ''}.json`;
console.log(`Downloading news for S${league.season} ${league.name}${team ? ' ' + team.name : ''}`);

fs.stat(file, (err, stats) => {
	if (err) fs.writeFileSync(file, '[]');

	http.get(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=team_news&teamid=${team ? team.id : 0}&typeid=0&displaylimit=${team ? 250 : 500}&leagueid=${league.id}&seasonid=${league.season}`, res => {
		const { statusCode } = res;
		const contentType = res.headers['content-type'];
		let err;

		if (statusCode !== 200)
			err = new Error(`Failed to fetch news for S${league.season} ${league.name}${team ? ' ' + team.name : ''} (status=${statusCode}`);
		else if (!/^text\/html/.test(contentType))
			err = new Error(`Failed to fetch schedule for S${league.season} ${league.name}${team ? ' ' + team.name : ''} (content-type=${contentType})`);

		if (err) {
			console.error(err.message);
			res.resume();
			return;
		}

		let html = '';
		res.setEncoding('utf8');
		res.on('data', chunk => { html += chunk; });
		res.on('end', () => {
			let regex = new RegExp('<li(?:[^>]+)? NewsFeedItem(?:[^>]+)?>\\s+?(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>\\s+?)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/feed/(.*?).png(?:[^>]+)?>\\s+?)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>\\s+?)?</a>\\s+?<div(?:[^>]+)?>\\s+?<h3(?:[^>]+)?>(.*?)</h3>\\s+?<abbr(?:[^>]+)?>(.*?)</abbr>\\s+?</div>\\s+?</li>', 'ig'), data;
			if (!(data = html.match(regex))) return;

			let prev = require(file), next = [], regex1 = new RegExp(regex.source, regex.flags.replace('g', ''));

			for (let i = 0, end = data.length; i < end; i++) {
				let news = data[i].match(regex1);

				if (news) {
					let _teams = [], team;

					if ((team = teams[news[1]]) && team.leagues.indexOf(league.id) != -1)
						_teams.push(team.id);

					if ((team = teams[news[3]]) && team.leagues.indexOf(league.id) != -1)
						_teams.push(team.id);

					let item = {league: league.id, message: news[4].replace(/<img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?> <span(?:[^>]+)?>(.*?)<\/span>/g, (a,b) => {
						let team;

						if (team = teams[b]) {
							if (team.leagues.indexOf(league.id) != -1)
								_teams.push(team.id);

							return team.name;
						} else
							return a;
					}).replace(/<(?:[^>]+)?>/g, '').replace(/[ ]+/g, ' ').trim(), new: false, teams: _teams.filter((v,i,a) => {return a.indexOf(v)==i}).sort(), timestamp: news[5].trim(), type: (news[2] || '').trim()};

					if (!item.type) {
						if (item.message.match(/.* have earned the player rights for .*? with a bid/i))
							item.type = 'bids';
						else if (item.message.match(/.*? have agreed to a \d+ season deal/i))
							item.type = 'contracts';
						else if (item.message.match(/.*? have drafted .*? \d+\w{2} overall/i))
							item.type = 'draft';
						else
							item.type = 'news';
					} else if (/^arrow\d+$/.test(item.type))
						item.type = 'roster';

					if (item.type != 'draft' && item.type != 'roster' && !/s$/.test(item.type))
						item.type += 's';

					if (prev.length && !prev.filter(function(p){return p.league==item.league && p.message==item.message && p.teams==item.teams && p.timestamp==item.timestamp && p.type==item.type}).length)
						item.new = true;

					item.new = _teams.indexOf(15) != -1;
					next.push(item);
				}
			}

			data = JSON.stringify(next.filter((v,i,a) => {return a.indexOf(v)==i}).reverse());
			if (JSON.stringify(prev) == data) process.exit();

			fs.writeFile(file, data, err => {
				if (err) console.error(err.message);
				process.exit();
			});
		});
	}).on('error', err => {
		console.error(err.message);
		process.exit();
	});
});
