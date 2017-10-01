const fs = require('fs');
const request = require('request');

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

	request(`http://www.leaguegaming.com/forums/index.php?leaguegaming/league&action=league&page=team_news&teamid=${team ? team.id : 0}&typeid=0&displaylimit=${team ? 250 : 500}&leagueid=${league.id}&seasonid=${league.season}`, (err, res, html) => {
		html = html.replace(/>\s+</g, '><');
		let contentType = res.headers['content-type'];

		if (res.statusCode != 200)
			err = new Error(`Failed to fetch news for S${league.season} ${league.name}${team ? ' ' + team.name : ''} (status=${res.statusCode})`);
		else if (!/^text\/html/.test(contentType))
			err = new Error(`Failed to fetch schedule for S${league.season} ${league.name}${team ? ' ' + team.name : ''} (content-type=${contentType})`);

		if (err) {
			console.error(err.message);
			process.exit();
		}

		let regex = new RegExp('<li(?:[^>]+)? NewsFeedItem(?:[^>]+)?>(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/feed/(.*?).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?/team(\\d+).png(?:[^>]+)?>)?</a><div(?:[^>]+)?><h3(?:[^>]+)?>(.*?)</h3><abbr(?:[^>]+)?>(.*?)</abbr></div></li>', 'ig'), data;
		if (!(data = html.match(regex))) process.exit();

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
				}).replace(/<(?:[^>]+)?>/g, '').replace(/[ ]+/g, ' ').trim(), new: false, teams: _teams.filter((v,i,a) => {return a.indexOf(v)==i}).sort((a,b) => {return a-b}), timestamp: news[5].trim(), type: (news[2] || '').trim()};

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

				if (/^(?!draft|roster).*?[^s]$/.test(item.type))
					item.type += 's';

				if (prev.length && !prev.filter(function(p){return p.league==item.league && p.message==item.message && p.teams.toString()==item.teams.toString() && p.timestamp==item.timestamp && p.type==item.type}).length)
					item.new = true;

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
});