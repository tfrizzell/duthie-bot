/**
 * This module provides an API for interaction with leaguegaming.com.
 */
'use strict';

const crypto = require('crypto');
const moment = require('moment-timezone');
const url = require('url');

const API = require('./api');
const teamUtils = require('../team-utils');

const config = require('../../config.json');

const timezone = 'America/New_York';

class SPNHL_API extends API {
    constructor(...args) {
        super(...args);

        this.buildUrl = this.buildUrl.bind(this);
        this.getGames = this.getGames.bind(this);
        this.getTeams = this.getTeams.bind(this);
    }

    static ['new'](options = {...config.sites}.spnhl) {
        return new SPNHL_API(options);
    }

    buildUrl({
        file = 'index.php'
    } = {}) {
        return `https://thespnhl.com/api/${file}`;
    }

    getGames() {
        return new Promise((resolve, reject) => {
            this.get({
                file: 'schedule.php'
            }).then(games => {
                resolve(games);
            }).catch(reject);
        });
    }

    getTeams() {
        return new Promise((resolve, reject) => {
            this.get({
                file: 'teams.php'
            }).then(teams => {
                resolve(teams.map(team => ({
                    id: team.id,
                    ...teamUtils.NHL.getTeamFromAbbreviation(team.abbrev)
                })));
            }).catch(reject);
        });
    }
}

module.exports = SPNHL_API;
