/**
 * This module provides an API for interaction with vghl.myvirtualgaming.com.
 */
'use strict';

const moment = require('moment-timezone');

const API = require('./api');

const config = require('../../config.json');

const timezone = 'America/New_York';

const helper = {
    static: {
        codename: {
            vghlclub: 'VGCLUB',
            vghlwc: 'VGHLWC'
        }
    }
};

class MyVirtualGaming_API extends API {
    constructor(options = {}) {
        super(options);

        const {
            subdomain = 'vghl'
        } = options;

        this.subdomain = subdomain;

        this.buildUrl = this.buildUrl.bind(this);
        this.getGames = this.getGames.bind(this);
        this.getInfo = this.getInfo.bind(this);
        this.getTeams = this.getTeams.bind(this);
        this.normalize = this.normalize.bind(this);
    }

    static ['new'](options = {...config.sites}.myvirtualgaming) {
        return new MyVirtualGaming_API(options);
    }

    buildUrl({
        league = 'vghl',
        path = '',
        ...params
    } = {}) {
        return `https://${this.subdomain}.myvirtualgaming.com/vghlleagues/${league}/${path}?${this.buildQueryString(params)}`.replace(/\?+$/, '');
    }

    getGames({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return Promise.all([
            this.getTeamMap({leagueId, seasonId}),

            this.get({
                league: leagueId,
                path: 'schedule'
            }).then(html => {
                const reWeeks = /<option(?:[^>]+)?value=["']?(\d{8})["']?(?:[^>]+)?>\d{4}-\d{2}-\d{2}<\/option>/ig;
                const promises = [];
                let week;

                while (week = reWeeks.exec(html)) {
                    promises.push(this.get({
                        league: leagueId,
                        path: 'schedule',
                        filter_scheduled_week: week[1]
                    }).then(html => {
                        const reDate = /<div(?:[^>]+)?col-sm-12(?:[^>]+)?><table(?:[^>]+)?><thead(?:[^>]+)?><tr(?:[^>]+)?><th(?:[^>]+)?>\s+\w+ (\d+)\w{2} (\w+) (\d{4}) @ (\d+:\d+[ap]m)\s+<\/th><\/tr><\/thead><\/table><\/div>/i;
                        const reGame = /<div(?:[^>]+)?mvg-table(?:[^>]+)?><div(?:[^>]+)?><div(?:[^>]+)?><div(?:[^>]+)?><div(?:[^>]+)?\bschedule-team-logo\b(?:[^>]+)?><img(?:[^>]+)?\/(\w+)\.[^\.]+(?:[^>]+)?><\/div><div(?:[^>]+)?\bschedule-team\b(?:[^>]+)?><div(?:[^>]+)?\bschedule-team-name\b(?:[^>]+)?>[^<]+<\/div><div(?:[^>]+)?\bschedule-team-record\b(?:[^>]+)?>[^<]+<\/div><\/div><div(?:[^>]+)?\bschedule-team-score\b(?:[^>]+)?>([^<]+)<\/div><\/div><div(?:[^>]+)?><div(?:[^>]+)?\bschedule-team-logo\b(?:[^>]+)?><img(?:[^>]+)?\/(\w+)\.[^\.]+(?:[^>]+)?><\/div><div(?:[^>]+)?\bschedule-team\b(?:[^>]+)?><div(?:[^>]+)?\bschedule-team-name\b(?:[^>]+)?>[^<]+<\/div><div(?:[^>]+)?\bschedule-team-record\b(?:[^>]+)?>[^<]+<\/div><\/div><div(?:[^>]+)?\bschedule-team-score\b(?:[^>]+)?>([^<]+)<\/div><\/div><\/div><div(?:[^>]+)?\bschedule-summary-link\b(?:[^>]+)?><a(?:[^>]+)?&(?:amp;)?id=(\d+)(?:[^>]+)?>[^<]+<\/a><\/div><\/div><\/div>/i;
                        const reDateOrGame = new RegExp(`(?:${reDate.source}|${reGame.source})`, 'ig');
                        const games = [];
                        let data;
                        let date;

                        while (data = reDateOrGame.exec(html)) {
                            if (data[1]) {
                                date = moment.tz(`${data[2]} ${data[1]}, ${data[3]} ${data[4]}`, 'MMMM D, YYYY h:mma', timezone);
        
                                if (moment.tz(timezone).month() <= 6 && date.month() >= 7) {
                                    date.subtract(1, 'years');
                                } else if (moment.tz(timezone).month() >= 7 && date.month() <= 6) {
                                    date.add(1, 'years');
                                }
                            }

                            if (data[5]) {
                                games.push({
                                    id: parseInt(data[9]),
                                    date: date.toISOString(),
                                    visitor: {
                                        id: data[5].trim(),
                                        score: !Number.isNaN(Number(data[6])) ? parseInt(data[6]) : null
                                    },
                                    home: {
                                        id: data[7].trim(),
                                        score: !Number.isNaN(Number(data[8])) ? parseInt(data[8]) : null
                                    }
                                });
                            }
                        }

                        return games;
                    }));
                }

                return Promise.all(promises);
            })
        ]).then(([teams, games]) => {
            games = games.reduce((games, _games) => [...games, ..._games], [])
                        .sort((a, b) => (new Date(a.date) - new Date(b.date)) || (a.id - b.id));

            for (const game of games) {
                game.visitor.id = teams[game.visitor.id];
                game.home.id = teams[game.home.id];
            }

            return games;
        });
    }

    getInfo({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                league: leagueId,
                path: leagueId,
                format: 'feed',
                type: 'atom'
            }).then(json => {
                if (!json.feed.id.match(`/vghlleagues/${leagueId}/${leagueId}`)) {
                    return reject('Failed to get league info: feed did not match requested league');
                }

                this.get({
                    league: leagueId,
                    path: 'standings',
                    ...(seasonId ? {filter_schedule: seasonId} : undefined)
                }).then(html => {
                    const seasons = (html.match(/<select(?:[^>]+)?\bfilter_schedule\b(?:[^>]+)?>(.*?)<\/select>/) || ['', ''])[1].match(/<option(?:[^>]+)?value=["']?(\d+)["']?(?:[^>]+)?>/g).map(opt => opt.replace(/.*?value=["']?(\d+)["']?.*/, '$1'));

                    resolve({
                        codename: this.normalize(helper.static.codename[leagueId] || json.feed.title._value.replace(/^(\S+).*/, '$1').trim()),
                        id: leagueId,
                        name: this.normalize(json.feed.title._value.replace(/(.*?) Home$/, '$1').trim()),
                        seasonId: seasons[seasons.length - 1]
                    });
                }).catch(err => {
                    if (!(err instanceof API.Error)) {
                        throw err;
                    }

                    const seasons = (err.content.match(/<select(?:[^>]+)?\bfilter_schedule\b(?:[^>]+)?>(.*?)<\/select>/) || ['', ''])[1].match(/<option(?:[^>]+)?value=["']?(\d+)["']?(?:[^>]+)?>/g).map(opt => opt.replace(/.*?value=["']?(\d+)["']?.*/, '$1'));

                    if (!seasons.length) {
                        throw err;
                    }

                    resolve({
                        codename: this.normalize(helper.static.codename[leagueId] || json.feed.title._value.replace(/^(\S+).*/, '$1').trim()),
                        id: leagueId,
                        name: this.normalize(json.feed.title._value.replace(/(.*?) Home$/, '$1').trim()),
                        seasonId: seasons[seasons.length - 1]
                    });
                });
            }).catch(reject)
        });
    }

    getTeamMap({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return this.get({
            league: leagueId,
            path: 'rosters',
            ...(seasonId ? {filter_schedule: seasonId} : undefined)
        }).then(html => {
            const reTeams = /<a(?:[^>]+)?\/rosters\?id=(\d+)(?:[^>]+)?>(?:\s+)?<img(?:[^>]+)?\/(\w+)\.\w{3,4}(?:[^>]+)?>(?:\s+)?<\/a>/ig;
            const teams = {};
            let team;

            while (team = reTeams.exec(html)) {
                teams[team[2]] = parseInt(team[1]);
            }

            return teams;
        });
    }

    getTeams({leagueId, seasonId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                league: leagueId,
                path: 'player-statistics',
                ...(seasonId ? {filter_schedule: seasonId} : undefined)
            }).then(html => {
                const reTeams = /<option(?:[^>]+)?value=["']?(\d+)["']?(?:[^>]+)?>(.*?)<\/option>/ig;
                const teamList = html.match(/<select(?:[^>]+)?filter_stat_team(?:[^>]+)?>(.*?)<\/select>/i)[1];
                const teams = {};
                let team;

                while (team = reTeams.exec(teamList)) {
                    const id = parseInt(team[1]);
                    teams[id] = {id: id, name: this.normalize(team[2].trim()), shortname: this.normalize(team[2].trim())};
                }

                this.get({
                    league: leagueId,
                    path: 'standings',
                    ...(seasonId ? {filter_schedule: seasonId} : undefined)
                }).then(html => {
                    const reTeams = new RegExp(`<a(?:[^>]+)?/${leagueId}/rosters\\?id=(\\d+)(?:[^>]+)?><img(?:[^>]+)?> (.*?)</a>`, 'ig');
                    let team;

                    while (team = reTeams.exec(html)) {
                        const id = parseInt(team[1]);

                        teams[id] = {
                            id: id,
                            ...teams[id],
                            shortname: this.normalize(({...teams[id]}.shortname || '').replace(this.normalize(team[2]), '').trim() || team[2])
                        };
                    }

                    resolve(teams);
                }).catch(reject);
            }).catch(reject);
        });
    }

    normalize(string) {
        return super.normalize(string).replace(/Bellevile/, 'Belleville');
    }
}

module.exports = MyVirtualGaming_API;
