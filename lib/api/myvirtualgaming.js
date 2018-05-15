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

    buildUrl({
        league = 'vghl',
        path = '',
        ...params
    } = {}) {
        return `https://${this.subdomain}.myvirtualgaming.com/vghlleagues/${league}/${path}?${this.buildQueryString(params)}`.replace(/\?+$/, '');
    }

    getGames({leagueId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.getTeams({leagueId}).then(teams => {
                teams = Object.values(teams).reduce((teams, team) => ({...teams, [team.shortname]: parseInt(team.id)}), {});

                this.get({
                    league: leagueId,
                    path: 'schedule'
                }).then(html => {
                    const reWeeks = /<option(?:[^>]+)?value=["']?(\d{8})["']?(?:[^>]+)?>\d{4}-\d{2}-\d{2}<\/option>/ig;
                    const promises = [];
                    const games = [];
                    let week;

                    while (week = reWeeks.exec(html)) {
                        promises.push(this.get({
                            league: leagueId,
                            path: 'schedule',
                            filter_scheduled_week: week[1]
                        }));
                    }

                    Promise.all(promises).then(html => {
                        html = html.join('');

                        const reDate = /<div(?:[^>]+)?col-sm-12(?:[^>]+)?><table(?:[^>]+)?><thead(?:[^>]+)?><tr(?:[^>]+)?><th(?:[^>]+)?>\s+\w+ (\d+)\w{2} (\w+) (\d{4}) @ (\d+:\d+[ap]m)\s+<\/th><\/tr><\/thead><\/table><\/div>/i;
                        const reGame = /<div(?:[^>]+)?mvg-table(?:[^>]+)?>.*?<div(?:[^>]+)?schedule-team-name(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-score(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-name(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-score(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-summary-link(?:[^>]+)?><a(?:[^>]+)?&(?:amp;)?id=(\d+)(?:[^>]+)?>.*?<\/a><\/div>/i;
                        const reDateOrGame = new RegExp(`(?:${reDate.source}|${reGame.source})`, 'ig');
                        let date;
                        let line;

                        while (line = reDateOrGame.exec(html)) {
                            let data;

                            if (data = reDate.exec(line)) {
                                date = moment.tz(`${data[2]} ${data[1]}, ${data[3]} ${data[4]}`, 'MMMM D, YYYY h:mma', timezone);
        
                                if (moment.tz(timezone).month() <= 6 && date.month() >= 7) {
                                    date.subtract(1, 'years');
                                } else if (moment.tz(timezone).month() >= 7 && date.month() <= 6) {
                                    date.add(1, 'years');
                                }
                            }

                            if (data = reGame.exec(line)) {
                                games.push({
                                    id: parseInt(data[5]),
                                    date: date.toISOString(),
                                    visitor: {
                                        id: teams[this.normalize(data[1]).trim()],
                                        score: !isNaN(data[2]) ? parseInt(data[2]) : null
                                    },
                                    home: {
                                        id: teams[this.normalize(data[3]).trim()],
                                        score: !isNaN(data[4]) ? parseInt(data[4]) : null
                                    }
                                });
                            }
                        }

                        resolve(games);
                    }).catch(err => {
                        reject(err);
                    });
                }).catch(reject);
            }).catch(reject);
        });
    }

    getInfo({leagueId}) {
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

                resolve({
                    codename: this.normalize(helper.static.codename[leagueId] || json.feed.title._value.replace(/^(\S+).*/, '$1').trim()),
                    id: leagueId,
                    name: this.normalize(json.feed.title._value.replace(/(.*?) Home$/, '$1').trim())
                });
            }).catch(reject)
        });
    }

    getTeams({leagueId}) {
        if (!leagueId) {
            throw new TypeError(`leagueId is not defined`);
        }

        return new Promise((resolve, reject) => {
            this.get({
                league: leagueId,
                path: 'player-statistics'
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
                    path: 'standings'
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
module.exports.new = (options = {...config.sites}.myvirtualgaming) => new MyVirtualGaming_API(options);
