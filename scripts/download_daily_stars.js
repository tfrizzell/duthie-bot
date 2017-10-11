const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
const config = require(`${dir}/config.json`);
const pkg = require(`${dir}/package.json`);

const leagues = require(`${dir}/data/leagues.json`);
let [league, date] = process.argv.slice(2);
let thread;

if (!leagues[league]) {
	console.error(`Invalid league: ${league}`);
	process.exit();
} else
	league = leagues[league];

if (process.argv.length < 4) {
	date = new Date();
	date.setDate(date.getDate() - 1);
} else
	date = new Date(`${date} GMT-0400`);

try {
	let [tday, tmonth, tdate, tyear] = date.toLocaleDateString('nu-fullwide', {day: 'numeric', month: 'long', weekday: 'long', year: 'numeric'}).split(/\s+,?|,?\s+/);
	let tord = (() => (tdate < 11) || (tdate > 13) ? ['st','nd','rd','th'][Math.min((tdate - 1) % 10, 3)] : 'th')();
	thread = `${league.name} Daily 3 Stars For ${tday} ${tmonth} ${tdate}${tord}, ${tyear}`;
} catch (x) {}

if (!thread) {
	console.error(`Invalid date: ${process.argv[3]}`);
	process.exit();
}

const prefix = 'daily-stars-';
const path = `${dir}/data/${prefix}${league.id}.json`;
date = thread.split(' ').slice(5).join(' ');
console.log(`Downloading ${thread}`);

function downloadDailyStars() {
	let regex0 = new RegExp('<h3(?:[^>]+)?><a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)</a></h3>', 'i');
	let regex1 = new RegExp('<tr(?:[^>]+)?>(<td(?:[^>]+)?>.*?</td>){7,8}</tr>', 'ig');

	request({
		url: `http://www.leaguegaming.com/forums/index.php?search/1/&q=${thread}&o=date&c[node]=${league.forum}`,
		headers: {
			'User-Agent': `${config.name}/${pkg.version.replace(/^v+/g,'')}`
		}
	}, (err, res, html) => {
		if (!err) {
			if (res.statusCode != 200)
				err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(res.headers['content-type']))
				err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (content-type=${res.headers['content-type']})`);
		}

		if (err) {
			console.error(err.message);
			return resolve();
		}

		let data = html.replace(/>\s+</g, '><').match(regex0);

		if (!data)
			process.exit();

		request({
			url: `http://www.leaguegaming.com/forums/${data[1]}`,
			headers: {
				'User-Agent': `${config.name}/${pkg.version.replace(/^v+/g,'')}`
			}
		}, (err, res, html) => {
			if (!err) {
				if (res.statusCode != 200)
					err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (status=${res.statusCode})`);
				else if (!/^text\/html/.test(res.headers['content-type']))
					err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (content-type=${res.headers['content-type']})`);
			}

			if (err) {
				console.error(err.message);
				return resolve();
			}

			let data = html.replace(/>\s+</g, '><').match(regex1);

			if (!data)
				process.exit();

			let stars = {date: date, forwards: [], defenders: [], goalies: []};
			let group;

			for (let i = 0, end = data.length; i < end; i++) {
				let starData = data[i].match(/<td(?:[^>]+)?>(.*?)<\/td>/ig).map(v => v.replace(/<\/?td(?:[^>]+)?>/ig, ''));

				if (!starData || starData.length < 7)
					continue;

				let rank = parseInt(starData[0]) || starData[0].match(/\/star\.gif/g).length;

				if (rank == 1) {
					if (!group)
						group = stars.forwards;
					else if (group == stars.forwards)
						group = stars.defenders;
					else if (group == stars.defenders)
						group = stars.goalies;
					else if (group == stars.goalies)
						break;
				}

				group.push({
					rank: rank,
					team: parseInt(starData[1].match(/\/team(\d+)\.(png|svg)/)[1]),
					name: starData[rank<4?2:1].replace(/(<.*?>|\(\w{1,2}\))/ig, '').trim(),
					stats: starData.slice(-4).map(v => parseFloat(v))
				});
			}

			fs.writeFile(path, JSON.stringify(stars), err => {
				if (err)
					console.error(err.message);
				else if (process.send)
					process.send([league.id, stars], () => process.exit());

				if (!process.send)
					process.exit();
			});
		});
	});
}


fs.readFile(path, (err, data) => {
	if (!err) {
		data = JSON.parse(data);

		if (data.date == date)
			process.exit();

		downloadDailyStars();
	} else
		fs.writeFile(path, '{}', downloadDailyStars);
});