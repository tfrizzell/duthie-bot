/**
 * This module rolls up all commands into a single export.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const filename = path.basename(__filename);

module.exports.ping = () => 'PONG';

for (const file of fs.readdirSync(__dirname)) {
    if (file == filename) {
        continue;
    }

    const [name, ext] = file.split('.');
    module.exports[name] = require(`./${file}`);
}
