'use strict';


///////////////////////////////////////
//     GLOBAL VARIABLE CREATION      //
///////////////////////////////////////
global.config = global.config || require('./config.json');
global.db = global.db || require('./lib/db');
global.logger = global.logger || require('./lib/logger');
global.pkg = global.pkg || require('./package.json');


///////////////////////////////////////
//    PREPARED STATEMENT REGISTRY    //
///////////////////////////////////////
const stmts = [];

global.prepareStatement = (...args) => {
	const stmt = db.prepare(...args);
	const finalize = stmt.finalize.bind(stmt);

	stmt.finalize = (...args) => {
		const result = finalize(...args);
		stmts.splice(stmts.indexOf(stmt), 1);
		return result;
	};

	stmts.push(stmt);
	return stmt;
};


///////////////////////////////////////
//         LOAD DEPENDENCIES         //
///////////////////////////////////////
const Discord = require('discord.js');


///////////////////////////////////////
//          STARTUP MESSAGE          //
///////////////////////////////////////
logger.info(`Starting ${config.name} v${pkg.version.replace(/^v+/g, '')} with node.js v${process.version.replace(/^v+/g, '')}, discord.js v${Discord.version.replace(/^v+/g, '')}`);


///////////////////////////////////////
//   DISCORD CLIENT INITIALIZATION   //
///////////////////////////////////////
global.client = new Discord.Client();
const cleanupClient = require('./lib/discord')(client);

client.login(config.token).then(() => {
	logger.info('Opened connection to Discord');
});


///////////////////////////////////////
//             CRON JOBS             //
///////////////////////////////////////
require('./lib/cron')({client, db});


///////////////////////////////////////
//     EVENT AND SIGNAL HANDLERS     //
///////////////////////////////////////
process.on('exit', () => {
	logger.info(`Shutting down ${config.name} v${pkg.version.replace(/^v+/g, '')}`);
});

require('./lib/node/exceptions');

require('./lib/node/cleanup')(() => Promise.all([
	new Promise(resolve => {
		if (client.status !== 0) {
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

		db.commit(err => {
			if (err) {
				logger.error(err);
			}
		});

		while (stmts.length > 0) {
			stmts[0].finalize();
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
