'use strict';

///////////////////////////////////////
//         LOAD DEPENDENCIES         //
///////////////////////////////////////
const Discord = require('discord.js');

const db = require('./lib/db');
const logger = require('./lib/logger');

const config = require('./config.json');
const pkg = require('./package.json');


///////////////////////////////////////
//          STARTUP MESSAGE          //
///////////////////////////////////////
logger.info(`Starting ${config.name} v${pkg.version.replace(/^v+/g, '')} with node.js v${process.version.replace(/^v+/g, '')}, discord.js v${Discord.version.replace(/^v+/g, '')}`);


///////////////////////////////////////
//   DISCORD CLIENT INITIALIZATION   //
///////////////////////////////////////
const client = require('./lib/discord/client').create();
require('./lib/discord');

client
	.login(config.token)
	.catch(err => {client.emit('error', err)});


///////////////////////////////////////
//             CRON JOBS             //
///////////////////////////////////////
require('./lib/cron');


///////////////////////////////////////
//     EVENT AND SIGNAL HANDLERS     //
///////////////////////////////////////
process.on('exit', () => {
	logger.info(`Shutting down ${config.name} v${pkg.version.replace(/^v+/g, '')}`);
});

require('./lib/node/exceptions');

require('./lib/node/cleanup')(() => Promise.all([
	new Promise(resolve => {
		if (client === null) {
			return resolve();
		}

		client.destroy().then(err => {
			if (err) {
				logger.error(err);
			}

			logger.info('Closed connection to Discord');
			resolve();
		}).catch(err => {
			logger.error(err);
			resolve();
		});
	}),
	new Promise(resolve => {
		if (!db.open) {
			return resolve();
		}

		db.close(err => {
			if (err) {
				logger.error(err);
			}

			logger.info('Closed connection to database');
			resolve();
		});
	})
]));
