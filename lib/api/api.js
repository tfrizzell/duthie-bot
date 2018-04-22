/**
 * This module provides an pseudo-abstracted API class to be extended by 
 * the supported site providers.
 */
'use strict';

const HTMLEntities = require('html-entities').AllHtmlEntities;
const querystring = require('querystring');
const request = require('request');
const xml2json = require('xml2json');

const config = global.config || require('../../config.json');
const pkg = global.pkg || require('../../package.json');

const entities = new HTMLEntities();
const logger = global.logger || require('../logger');

const helper = {
    get: (url, options = {}) => {
        return helper.request(url, undefined, {...options, method: 'GET'});
    },
    post: (url, data = {}, options = {}) => {
        return helper.request(url, data, {...options, method: 'POST'});
    },
    request: (url, data = {}, options = {}) => {
        return new Promise((resolve, reject) => {
            if (Object.keys(data).length > 0) {
                logger.verbose(`Sending ${options.method} request to ${url} with data:`, data);
            } else {
                logger.verbose(`Sending ${options.method} request to ${url}`);
            }

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
                const contentType = res.headers['content-type'].replace(/;.*$/, '');
                logger.verbose(`Received response from ${url} (content-type=${contentType}):`);

                if (err) {
                    return reject(`Failed to request to ${url}: ${err}`);
                } else if (res.statusCode !== 200) {
                    return reject(`Failed to request to ${url}: received status code ${res.statusCode}`);
                }

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
                        content = content.replace(/>\s+</g, '><').replace(/[\r\n]+/g, '');
                        break;

                    default: 
                        return reject(`Failed to request to ${url}: received unsupported content type ${contentType}`);
                }

                resolve(content);
            });
        });
    }
};

class API {
    constructor() {
        this.buildQueryString = this.buildQueryString.bind(this);
        this.buildUrl = this.buildUrl.bind(this);
        this.get = this.get.bind(this);
        this.getGames = this.getGames.bind(this);
        this.getInfo = this.getInfo.bind(this);
        this.getNews = this.getNews.bind(this);
        this.getStandings = this.getStandings.bind(this);
        this.getStars = this.getStars.bind(this);
        this.getTeams = this.getTeams.bind(this);
        this.normalize = this.normalize.bind(this);
        this.post = this.post.bind(this);
        this.request = this.request.bind(this);
    }

    buildQueryString(params = {}) {
        return querystring.stringify(params);
    }

    buildUrl(params = {}) {
        throw new ReferenceError(`\`buildUrl\` is not implemented in class ${this.constructor.name}`);
    }

    get(url = {}, options = {}) {
        if (typeof url === 'object') {
            return helper.get(this.buildUrl(url), options);
        } else {
            return helper.get(url, options);
        }
    }

    getGames() {
        throw new ReferenceError(`\`getGames\` is not implemented in class ${this.constructor.name}`);
    }

    getInfo() {
        throw new ReferenceError(`\`getInfo\` is not implemented in class ${this.constructor.name}`);
    }

    getNews() {
        throw new ReferenceError(`\`getNews\` is not implemented in class ${this.constructor.name}`);
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

    normalize(string) {
        return entities.decode(string).normalize('NFD').replace(/[\u0300-\u036f]/g, "");
    }

    post(url, data = {}, options = {}) {
        if (typeof url === 'object') {
            return helper.post(this.buildUrl(url), data, options);
        } else {
            return helper.post(url, data, options);
        }
    }

    request(url, data = {}, options = {}) {
        if (typeof url === 'object') {
            return helper.request(this.buildUrl(url), data, options);
        } else {
            return helper.request(url, data, options);
        }
    }
}

module.exports = API;
