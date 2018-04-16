'use strict';

const path = require('path');
const parsed = path.parse(__dirname);

global.__rootdir = parsed.dir;
global.__libdir = path.join(__rootdir, 'lib');

global.__configfile = path.join(__rootdir, 'config.json');
global.__dbfile = path.join(__rootdir, 'duthie-bot.db');
global.__pkgfile = path.join(__rootdir, 'package.json');
