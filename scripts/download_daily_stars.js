const fs = require('fs');
const request = require('request');

const dir = __dirname.replace(/\/scripts\/?$/, '');
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
	let [tday, tmonth, tdate, tyear] = date.toLocaleDateString('nu-fullwide', {day:'numeric', month:'long', weekday:'long', year:'numeric'}).split(/\s+,?|,?\s+/);
	let tord = (() => {return tdate<11||tdate>13?['st','nd','rd','th'][Math.min((tdate-1)%10,3)]:'th'})();
	thread = `${league.name} Daily 3 Stars For ${tday} ${tmonth} ${tdate}${tord}, ${tyear}`;
} catch (x) {}

if (!thread) {
	console.error(`Invalid date: ${process.argv[3]}`);
	process.exit();
}

const file = `${dir}/data/daily-stars-${league.id}.json`;
date = date.toJSON().substr(0, 10);
console.log(`Downloading ${thread}`);

fs.stat(file, (err, stats) => {
	if (err) fs.writeFileSync(file, '{}');

	let stars = require(file);
	if (stars.date == date) process.exit();

	request(`http://www.leaguegaming.com/forums/index.php?search/1/&q=${thread}&o=date&c[node]=586`, (err, res, html) => {
		html = html.replace(/>\s+</g, '><');
		let contentType = res.headers['content-type'];

		if (res.statusCode != 200)
			err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (status=${res.statusCode})`);
		else if (!/^text\/html/.test(contentType))
			err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (content-type=${contentType})`);

		if (err) {
			console.error(err.message);
			process.exit();
		}

		let regex = new RegExp('<h3(?:[^>]+)?><a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)</a></h3>', 'i'), data;
		if (!(data = html.match(regex))) process.exit();

		request(`http://www.leaguegaming.com/forums/${data[1]}`, (err, res, html) => {
			html = html.replace(/>\s+</g, '><');
			let contentType = res.headers['content-type'];

			if (res.statusCode != 200)
				err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (status=${res.statusCode})`);
			else if (!/^text\/html/.test(contentType))
				err = new Error(`Failed to fetch ${league.name} daily stars for ${date} (content-type=${contentType})`);

			if (err) {
				console.error(err.message);
				process.exit();
			}

			let regex = new RegExp('<tr(?:[^>]+)?>(<td(?:[^>]+)?>.*?){7,8}</tr>', 'ig'), data;
			if (!(data = html.match(regex))) process.exit();

			let stars = {data: date, forwards: [], defenders: [], goalies: []}, group;

			for (let i = 0, end = data.length; i < end; i++) {
				let star = data[i].match(/<td(?:[^>]+)?>(.*?)<\/td>/ig).map(v => {return v.replace(/<\/?td(?:[^>]+)?>/ig, '')});

				if (star) {
					let rank = parseInt(star[0]) || star[0].match(/\/star\.gif/g).length;

					if (rank == 1)
						group = (!group ? stars.forwards : (group == stars.forwards ? stars.defenders : stars.goalies));

					group.push({
						rank: parseInt(star[0]) || star[0].match(/\/star\.gif/g).length,
						team: parseInt(star[rank<4?3:1].match(/\/team(\d+)\.(png|svg)/)[1]),
						name: star[rank<4?2:1].replace(/(<.*?>|\(\w{1,2}\))/ig, '').trim(),
						stats: star.slice(-4).map(v => {return parseFloat(v)})
					});
				}
			}

			data = JSON.stringify(stars);

			fs.writeFile(file, data, err => {
				if (err) console.error(err.message);
				process.exit();
			});
		});
	});
});