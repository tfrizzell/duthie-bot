'use strict';

const querystring = require('querystring');
const request = require('request');
const xml2json = require('xml2json');

const config = global.conf || require(__configfile);
const pkg = global.pkg || require(__pkgfile);

const fn = {
    get: (url, options = {}) => {
        return fn.request(url, undefined, {...options, method: 'GET'});
    },
    post: (url, data = {}, options = {}) => {
        return fn.request(url, data, {...options, method: 'POST'});
    },
    request: (url, data = {}, options = {}) => {
        return new Promise((resolve, reject) => {
            request({
                ...options,
                url: url,
                form: data,
                encoding: 'utf8',
                headers: {
                    ...options.headers,
                    'User-Agent': `${config.name}/${pkg.version.replace(/^v+/g,'')}`
                }
            }, (err, res, content) => {
                if (err) {
                    return reject (`Failed to request to ${url}: ${err}`);
                } else if (res.statusCode !== 200) {
                    return reject(`Failed to request to ${url}: received status code ${res.statusCode}`);
                }

                const contentType = res.headers['content-type'].replace(/;.*$/, '');

                switch (true) {
                    case /^application\/(.+\+)?json$/.test(contentType):
                        content = JSON.parse(content);
                        break;

                    case /^application\/(.+\+)?xml$/.test(contentType):
                        content = xml2json.toJson(content, {
                            object: true,
                            coerce: true,
                            alternateTextNode: '_value'
                        });
                        break;

                    case /^text\/(.+\+)?(html|text)$/.test(contentType):
                        content = content.replace(/>\s+</g, '><');
                        break;

                    default: 
                        return reject(`Failed to request to ${url}: received unsupported content type ${contentType}`);
                }

                resolve(content);
            });
        });
    }
};

module.exports = class API {
    buildQueryString(params = {}) {
        return querystring.stringify(params);
    }

    buildUrl(params = {}) {
        throw new ReferenceError(`\`buildUrl\` is not implemented in class ${this.constructor.name}`);
    }

    get(url = {}, options = {}) {
        if (typeof url === 'object') {
            return fn.get(this.buildUrl(url), options);
        } else {
            return fn.get(url, options);
        }
    }

    getInfo() {
        throw new ReferenceError(`\`getInfo\` is not implemented in class ${this.constructor.name}`);
    }

    getNews() {
        throw new ReferenceError(`\`getNews\` is not implemented in class ${this.constructor.name}`);
    }

    getSchedule() {
        throw new ReferenceError(`\`getSchedule\` is not implemented in class ${this.constructor.name}`);
    }

    getStandings() {
        throw new ReferenceError(`\`getStandings\` is not implemented in class ${this.constructor.name}`);
    }

    getStars() {
        throw new ReferenceError(`\`getStars\` is not implemented in class ${this.constructor.name}`);
    }

    getTeams() {
        throw new ReferenceError(`\`getTeams\` is not implemented in class ${this.constructor.name}`);
    }

    post(url, data = {}, options = {}) {
        if (typeof url === 'object') {
            return fn.post(this.buildUrl(url), data, options);
        } else {
            return fn.post(url, data, options);
        }
    }

    request(url, data = {}, options = {}) {
        if (typeof url === 'object') {
            return fn.request(this.buildUrl(url), data, options);
        } else {
            return fn.request(url, data, options);
        }
    }
};
