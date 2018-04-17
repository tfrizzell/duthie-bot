/**
 * This module provides an API for interaction with vghl.myvirtualgaming.com.
 */
'use strict';

const API = require('./api');
const moment = require('moment-timezone');

const config = global.config || require('../../config.json');
moment.tz.setDefault('America/New_York');

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
            dateFormat = 'YYYY-MM-DD HH:mm:ss',
            subdomain = 'vghl'
        } = options;

        this.dateFormat = dateFormat;
        this.subdomain = subdomain;
    }

    buildUrl({
        league = 'vghl',
        path = '',
        ...params
    } = {}) {
        return `https://${this.subdomain}.myvirtualgaming.com/vghlleagues/${league}/${path}?${this.buildQueryString(params)}`.replace(/\?+$/, '');
    }

    getInfo({leagueId}) {
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
                    codename: helper.static.codename[leagueId] || json.feed.title._value.replace(/^(\S+).*/, '$1').trim(),
                    id: leagueId,
                    name: json.feed.title._value.replace(/(.*?) Home$/, '$1').trim()
                });
            }).catch(reject)
        });
    }

    getSchedule({leagueId}) {
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

                        const reGame = /<div(?:[^>]+)?mvg-table(?:[^>]+)?>.*?<div(?:[^>]+)?schedule-team-name(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-score(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-name(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-team-score(?:[^>]+)?>(.*?)<\/div>.*?<div(?:[^>]+)?schedule-summary-link(?:[^>]+)?><a(?:[^>]+)?&(?:amp;)?id=(\d+)(?:[^>]+)?>.*?<\/a><\/div>/i;
                        const reDate = /<div(?:[^>]+)?col-sm-12(?:[^>]+)?><table(?:[^>]+)?><thead(?:[^>]+)?><tr(?:[^>]+)?><th(?:[^>]+)?> \w+ (\d+)\w{2} (\w+) (\d{4}) @ (\d+:\d+[ap]m) <\/th><\/tr><\/thead><\/table><\/div>/i;
                        const reDateOrGame = new RegExp(`(?:${reGame.source}|${reDate.source})`, 'ig');
                        let date;
                        let line;

                        while (line = reDateOrGame.exec(html)) {
                            let data;

                            if (data = reDate.exec(line)) {
                                date = moment(`${data[2]} ${data[1]}, ${data[3]} ${data[4]}`, 'MMMM D, YYYY h:mma');
        
                                if (moment().month() <= 6 && date.month() >= 7) {
                                    date.subtract(1, 'years');
                                } else if (moment().month() >= 7 && date.month() <= 6) {
                                    date.add(1, 'years');
                                }
                            }

                            if (data = reGame.exec(line)) {
                                games.push({
                                    id: parseInt(data[5]),
                                    date: date.format(this.dateFormat),
                                    visitor: {
                                        id: teams[data[1]],
                                        score: !isNaN(data[2]) ? parseInt(data[2]) : null
                                    },
                                    home: {
                                        id: teams[data[3]],
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

    getTeams({leagueId}) {
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
                    teams[id] = {id: id, name: team[2].trim(), shortname: team[2].trim()};
                }

                this.get({
                    league: leagueId,
                    path: 'standings'
                }).then(html => {
                    const reTeams = new RegExp(`<a(?:[^>]+)?/${leagueId}/rosters\\?id=(\\d+)(?:[^>]+)?><img(?:[^>]+)?> (.*?)</a>`, 'ig');
                    let team;

                    while (team = reTeams.exec(html)) {
                        const id = parseInt(team[1]);
                        teams[id] = {id: id, ...teams[id], shortname: ({...teams[id]}.shortname || '').replace(team[2], '').trim() || team[2]};
                    }

                    resolve(teams);
                }).catch(reject);
            }).catch(reject);
        });
    }
}

module.exports = MyVirtualGaming_API;
module.exports.new = (options = {...config.sites}.myvirtualgaming) => new MyVirtualGaming_API(options);
