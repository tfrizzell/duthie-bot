/**
 * This module rolls up all commands into a single export.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const filename = path.basename(__filename);
const files = fs.readdirSync(__dirname);

for (const file of files) {
    if (file == filename) {
        continue;
    }

    const [name, ext] = file.split('.');
    module.exports[name] = require(`./${file}`);
}

module.exports.ping = () => Promise.resolve('PONG');
