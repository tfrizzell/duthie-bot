'use strict';

const child_process = require('child_process');
const cron = require('cron');
const Discord = require('discord.js');

global.config = global.config || require('./config.json');
global.db = global.db || require('./lib/db');
global.logger = global.logger || require('./lib/logger');
global.pkg = global.pkg || require('./package.json');

logger.info(`Starting ${config.name} v${pkg.version.replace(/^v+/g, '')} with node.js v${process.version.replace(/^v+/g, '')}, discord.js v${Discord.version.replace(/^v+/g, '')}`);

const client = new Discord.Client();
require('./lib/discord')(client);

client.login(config.token).then(() => {
	logger.info('Opened connection to Discord');
});

/** Install cron jobs **/

process.on('exit', () => {
	logger.info(`Shutting down ${config.name} v${pkg.version.replace(/^v+/g, '')}`);
});

require('./lib/node/exceptions');

require('./lib/node/cleanup')(() => Promise.all([
	new Promise(resolve => {
		if (client.status !== 0) {
			return resolve();
		}

		client.destroy().then(() => {
			logger.info('Closed connection to Discord');
			resolve();
		}).catch(err => {
			logger.error(err);
			resolve();
		})
	}),
	new Promise(resolve => {
		if (!db.open) {
			return resolve();
		}

		db.commit(err => {
			if (err) {
				logger.error(err);
			}
		});

		db.close(err => {
			if (err) {
				logger.error(err);
			} else {
				logger.info('Closed connection to database');
			}

			resolve();
		})
	})
]));
