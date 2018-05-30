/**
 * This module provides an API for interaction with leaguegaming.com.
 */
'use strict';

const crypto = require('crypto');
const moment = require('moment-timezone');
const url = require('url');

const API = require('./api');

const config = require('../../config.json');

const timezone = 'America/New_York';

const helper = {
    news: {
        cleanMessage: (message = '') => {
            return message
                .replace(/<img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?><span(?:[^>]+)?>.*?<\/span>/g, (a, b) => `::team=${b}::`)
                .replace(/<\/span><span /g, '</span> <span ')
                .replace(/<[^>]+>/g, '')
                .replace(/[ ]+/g, ' ');
        },
        generateHash: (item = []) => {
            const buf = [
                helper.news.cleanMessage(item[4]),
                helper.news.getTeams(item[4]),
                moment.tz((item[5] || '').trim(), 'YYYY-MM-DD hh:mma', timezone).valueOf()
            ];
        
            const shasum = crypto.createHash('sha1');
            shasum.update(JSON.stringify(buf));
            return shasum.digest('hex');
        },
        getTeams: (message = '') => {
            return (message.match(/<img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?><span(?:[^>]+)?>(.*?)<\/span>/g) || []).map(t => t.replace(/.*?team(\d+).*/, '$1'));
        }
    }
};

class LeagueGaming_API extends API {
    constructor(...args) {
        super(...args);

        this.buildUrl = this.buildUrl.bind(this);
        this.getGames = this.getGames.bind(this);
        this.getInfo = this.getInfo.bind(this);
        this.getNews = this.getNews.bind(this);
        this.getTeams = this.getTeams.bind(this);
    }

    buildUrl({
        file = 'index.php',
        path = 'leaguegaming/league',
        ...params
    } = {}) {
        return `https://www.leaguegaming.com/forums/${file}?${path || ''}&${this.buildQueryString(params)}`.replace(/\?&/, '&');
    }

    getDailyStars({forumId, date = moment.tz(timezone).subtract(1, 'days')}) {
        if (!forumId) {
            throw new TypeError(`forumId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                path: 'search/1/',
                q: moment.tz(date, timezone).format('[Daily 3 Stars For] dddd MMMM Do, YYYY'),
                o: 'date',
                'c[title_only]': 1,
                'c[node]': forumId
            }).then(html => {
                const reThread = /<h3(?:[^>]+)?><a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a><\/h3>/i;
                let data;

                if (!(data = reThread.exec(html))) {
                    return resolve({});
                }

                this.get(`https://www.leaguegaming.com/forums/${data[1]}`).then(html => {
                    const reGroup = /<div(?:[^>]+)?d3_title(?:[^>]+)?>(.*?)<\/div>/i;
                    const reStar = /<tr(?:[^>]+)?><td(?:[^>]+)?>((?:<img(?:[^>]+)?\/star\.gif(?:[^>]+)?>)+|\d+\.)<\/td>(?:<td(?:[^>]+)?t_threestars(?:[^>]+)?><div(?:[^>]+)?><img (?:[^>]+)?\/team(\d+)\.svg(?:[^>]+)?><img(?:[^>]+)?><\/div><\/td><td(?:[^>]+)?>(.*?)<br><span(?:[^>]+)?>\((.*?)\)<\/span><\/td>|<td(?:[^>]+)?><img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?> (.*?) \((.*?)\)<\/td>)<td(?:[^>]+)?><a(?:[^>]+)?>.*?<\/a><\/td>((?:<td>(.*?)<\/td>)+)<\/tr>/i;
                    const reGroupOrStar = new RegExp(`(?:${reGroup.source}|${reStar.source})`, 'ig');
                    let stars = {};
                    let group;
                    let line;

                    while (line = reGroupOrStar.exec(html)) {
                        let data;
    
                        if (data = reGroup.exec(line)) {
                            group = data[1].toLowerCase();
                        }

                        if (data = reStar.exec(line)) {
                            const metadata = data[8].match(/<td>(.*?)<\/td>/g).map(v => v.replace(/<(?:[^>]+)?>/g, ''));

                            stars[group] = [
                                ...(stars[group] || []),
                                {
                                    rank: ((data[1].match(/star\.gif/g) || []).length) || parseInt(data[1]),
                                    team: parseInt(data[2]) || parseInt(data[5]),
                                    name: data[3] || data[6],
                                    position: data[4] || data[7],
                                    metadata: (group !== 'goalies')
                                        ? {
                                            'Points': parseInt(metadata[0]),
                                            'Goals': parseInt(metadata[1]),
                                            'Assists': parseInt(metadata[2]),
                                            '+/-': (metadata[3] >= 0 ? '+' : '') + parseInt(metadata[3])
                                        } : {
                                            'SV%': Number((parseFloat(metadata[0]) / 100) + 0.0005).toFixed(3),
                                            'GAA': Number(parseFloat(metadata[1]) + 0.005).toFixed(2),
                                            'Saves': parseInt(metadata[2]),
                                            'Shots': parseInt(metadata[3])
                                        }
                                }
                            ];
                        }
                    }

                    resolve(stars);
                });
            }).catch(reject);
        });
    }

    getGames({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        if (!seasonId) {
            throw new TypeError(`seasonId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                action: 'league',
                page: 'league_schedule_all',
                leagueid: leagueId,
                seasonid: seasonId
            }).then(html => {
                const reDate = /<h4(?:[^>]+)?sh4(?:[^>]+)?>(.*?)<\/h4>/i;
                const reGame = /<span(?:[^>]+)?sweekid(?:[^>]+)?>Week (\d+)<\/span>(?:<span(?:[^>]+)?sgamenumber(?:[^>]+)?>Game# (\d+)<\/span>)<img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?><a(?:[^>]+)?&gameid=(\d+)(?:[^>]+)?><span(?:[^>]+)?steamname(?:[^>]+)?>(.*?)<\/span><span(?:[^>]+)?sscore(?:[^>]+)?>(vs|(\d+)\D+(\d+))<\/span><span(?:[^>]+)?steamname(?:[^>]+)?>(.*?)<\/span><\/a><img(?:[^>]+)?\/team(\d+)\.png(?:[^>]+)?>/i;
                const reDateOrGame = new RegExp(`(?:${reDate.source}|${reGame.source})`, 'ig');
                const games = [];
                let date;
                let line;

                while (line = reDateOrGame.exec(html)) {
                    let data;

                    if (data = reDate.exec(line)) {
                        date = moment.tz(data[1], 'MMMM DD[th]', timezone);

                        if (moment.tz(timezone).month() <= 6 && date.month() >= 7) {
                            date.subtract(1, 'years');
                        } else if (moment.tz(timezone).month() >= 7 && date.month() <= 6) {
                            date.add(1, 'years');
                        }
                    }

                    if (data = reGame.exec(line)) {
                        games.push({
                            id: parseInt(data[4]),
                            date: date.toISOString(),
                            visitor: {
                                id: parseInt(data[10]),
                                score: !Number.isNaN(Number(data[8])) ? parseInt(data[8]) : null
                            },
                            home: {
                                id: parseInt(data[3]),
                                score: !Number.isNaN(Number(data[7])) ? parseInt(data[7]) : null
                            }
                        });
                    }
                }

                resolve(games);
            }).catch(reject);
        });
    }

    getInfo({leagueId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                action: 'league',
                page: 'standings',
                leagueid: leagueId,
                seasonid: 1
            }).then(html => {
                const reInfo = new RegExp(`<li(?:[^>]+)? custom-tab-${leagueId} (?:[^>]+)?><a(?:[^>]+)?/league\\.(\\d+)/(?:[^>]+)?>.*?<span(?:[^>]+)?>(.*?)</span>.*?</a>`, 'i');
                const reSeason = new RegExp(`<a(?:[^>]+)?leagueid=${leagueId}&(?:amp;)?seasonid=(\\d+)(?:[^>]+)?>Standings</a>`, 'i');
                const info = {codename: undefined, forumId: undefined, id: parseInt(leagueId), name: undefined, seasonId: undefined};
                let data;

                if (data = reInfo.exec(html)) {
                    info.codename = data[2].trim().toUpperCase().replace(/[^A-Z0-9]/g, '');
                    info.forumId = parseInt(data[1]);
                    info.name = this.normalize(data[2].trim());

                    if (data = reSeason.exec(html)) {
                        info.seasonId = parseInt(data[1]);
                    }
                }

                resolve(info);
            }).catch(reject)
        });
    }

    getNews({leagueId, seasonId, teamid = 0, typeid = 0, displaylimit = 500} = {}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        if (!seasonId) {
            throw new TypeError(`seasonId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                action: 'league',
                page: 'team_news',
                teamid,
                typeid,
                displaylimit,
                leagueid: leagueId,
                seasonid: seasonId
            }).then(html => {
                const reNews = /<li(?:[^>]+)? NewsFeedItem(?:[^>]+)?>(?:<a(?:[^>]+)?><img(?:[^>]+)?\/team(\d+).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?\/(?:feed|icons?)\/(.*?).png(?:[^>]+)?>)?(?:<a(?:[^>]+)?><img(?:[^>]+)?\/team(\d+).png(?:[^>]+)?>)?<\/a><div(?:[^>]+)?><h3(?:[^>]+)?>(.*?)<\/h3><abbr(?:[^>]+)?>(.*?)<\/abbr><\/div><\/li>/ig;
                const items = [];
                let item;

                while (item = reNews.exec(html)) {
                    item = {
                        id: helper.news.generateHash(item),
                        message: this.normalize(helper.news.cleanMessage(item[4])),
                        teams: [...new Set([item[1], item[3], ...helper.news.getTeams(item[4])])].filter(team => !Number.isNaN(Number(team)) && team > 0).map(team => parseInt(team)).sort(),
                        timestamp: moment.tz((item[5] || '').trim(), 'YYYY-MM-DD hh:mma', timezone).toISOString(),
                        type: (item[2] || '').trim()
                    };

                    switch(true) {
                        case / have (placed .*? on|claimed .*? off of) waivers /i.test(item.message):
                            item.type = 'waiver';
                            break;

                        case / have traded /i.test(item.message):
                            item.type = 'trade';
                            break;

                        case /^arrow\d+$/.test(item.type):
                            item.type = 'roster';
                            break;

                        case / have drafted /i.test(item.message):
                            item.type = 'draft';
                            break;

                        case / have agreed to a \d+ season deal /i.test(item.message):
                            item.type = 'contract';
                            break;

                        case / have earned the player rights for .*? with a bid /i.test(item.message):
                            item.type = 'bid';
                            break;

                        case /^profile$/.test(item.type):
                        case / gamertag has changed from /i.test(item.message):
                            item.type = 'account';
                            break;

                        default:
                            item.type = 'news';
                    }

                    items.push(item);
                }

                resolve(items);
            }).catch(reject);
        });
    }

    getTeams({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        if (!seasonId) {
            throw new TypeError(`seasonId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                action: 'league',
                page: 'standing',
                leagueid: leagueId,
                seasonid: seasonId
            }).then(html => {
                const reNames = new RegExp(`<div(?:[^>]+)?class="team_box_icon"(?:[^>]+)?>.*?<a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${leagueId}&(?:amp;)?seasonid=${seasonId}(?:[^>]+)?>(.*?)</a></div>`, 'ig');
                const reShortnames = new RegExp(`<td(?:[^>]+)?><img(?:[^>]+)?/team\\d+.png(?:[^>]+)?> \\d+\\) .*?\\*?<a(?:[^>]+)?page=team_page&(?:amp;)?teamid=(\\d+)&(?:amp;)?leagueid=${leagueId}&(?:amp;)?seasonid=${seasonId}(?:[^>]+)?>(.*?)</a></td>`, 'ig');
                const reInfo = /<a(?:[^>]+)?href="(.*?)"(?:[^>]+)?>(.*?)<\/a>/i;
                const teams = {};
                let team;

                while (team = reNames.exec(html)) {
                    const id = parseInt(team[1]);
                    teams[id] = {id: id, name: this.normalize(team[2].trim()), shortname: this.normalize(team[2].trim())};
                }

                while (team = reShortnames.exec(html)) {
                    const id = parseInt(team[1]);
                    teams[id] = {id: id, ...teams[id], shortname: this.normalize(team[2].trim())};
                }

                resolve(teams);
            }).catch(reject);
        });
    }
};

module.exports = LeagueGaming_API;
module.exports.new = (options = {...config.sites}.leaguegaming) => new LeagueGaming_API(options);
