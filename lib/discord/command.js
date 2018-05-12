'use strict';

const parseMessage = content => {
    const tokens = [];

    content = content.replace(/(["'`])([^\1])(\1)/g, (match, delim, string) => {
        tokens.push(string);
        return `\${tokens[${tokens.length - 1}]`;
    });

    return content.trim().split(/\s+/).map(token => token.replace(/\$\{tokens\[(\d+)\]\}/g, (match, index) => tokens[index]));
};

module.exports = class DuthieCommand {
    constructor(message) {
        const content = message.content.trim().replace(/^(["'`])(.*?)(\1)$/, '$2');
        const tokens = parseMessage(content);
        const [prefix = '', name = '', subcommand = ''] = tokens;

        this.arguments = tokens.slice(2);
        this.content = content;
        this.message = message;
        this.name = name;
        this.params = this.arguments.reduce((params, arg) => {
            let [param, val] = arg.replace(/^\-+/, '').split('=');

            if (val !== undefined) {
                if (param === 'channel') {
                    val = val.replace(/^<\#(.*?)>$/, '$1');
                }

                if (!isNaN(val)) {
                    if (/\./.test(val)) {
                        val = parseFloat(val);
                    } else {
                        val = parseInt(val);
                    }
                } else {
                    val = val.trim();
                }

                return {...params, [param.trim().toLowerCase()]: val};
            } else {
                return params;
            }
        }, {});
        this.prefix = prefix;
        this.subcommand = subcommand.toLowerCase().replace(/(.)[-_](.)/g, (match, a, b) => `${a}${b.toUpperCase()}`);
        this.raw = message.content;
        this.tokens = tokens;
    }
}