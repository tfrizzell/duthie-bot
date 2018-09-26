'use strict';

const buildTeamInfoObject = (city = '', name = '') =>
    ({name: `${city} ${name}`.replace(/\s+/, ' ').trim(), shortname: String(name).trim()});

const teamUtils = module.exports = {
    NHL: {
        getTeamFromAbbrev: (...args) =>
            teamUtils.NHL.getTeamFromAbbreviation(...args),
        getTeamFromAbbreviation: (abbrev) => {
            switch (String(abbrev).toUpperCase()) {
                case 'ANA':
                    return buildTeamInfoObject('Anaheim', 'Ducks');

                case 'ARI':
                    return buildTeamInfoObject('Arizona', 'Coyotes');

                case 'BOS':
                    return buildTeamInfoObject('Boston', 'Bruins');

                case 'BUF':
                    return buildTeamInfoObject('Buffalo', 'Sabres');

                case 'CAR':
                    return buildTeamInfoObject('Carolina', 'Hurricanes');

                case 'CBJ':
                    return buildTeamInfoObject('Columbus', 'Blue Jackets');

                case 'CGY':
                    return buildTeamInfoObject('Calgary', 'Flames');

                case 'CHI':
                    return buildTeamInfoObject('Chicago', 'Blackhawks');

                case 'COL':
                    return buildTeamInfoObject('Colorado', 'Avalanche');

                case 'DAL':
                    return buildTeamInfoObject('Dallas', 'Stars');

                case 'DET':
                    return buildTeamInfoObject('Detroit', 'Red Wings');

                case 'EDM':
                    return buildTeamInfoObject('Edmonton', 'Oilers');

                case 'FLA':
                    return buildTeamInfoObject('Florida', 'Panthers');

                case 'LA':
                case 'LAK':
                    return buildTeamInfoObject('Los Angeles', 'Kings');

                case 'MIN':
                    return buildTeamInfoObject('Minnesota', 'Wild');

                case 'MTL':
                    return buildTeamInfoObject('Montreal', 'Canadiens');

                case 'NJ':
                case 'NJD':
                    return buildTeamInfoObject('New Jersey', 'Devils');

                case 'NSH':
                    return buildTeamInfoObject('Nashville', 'Predators');

                case 'NYI':
                    return buildTeamInfoObject('New York', 'Islanders');

                case 'NYR':
                    return buildTeamInfoObject('New York', 'Rangers');

                case 'OTT':
                    return buildTeamInfoObject('Ottawa', 'Senators');

                case 'PHI':
                    return buildTeamInfoObject('Philadelphia', 'Flyers');

                case 'PIT':
                    return buildTeamInfoObject('Pittsburgh', 'Penguins');

                case 'SJ':
                case 'SJS':
                    return buildTeamInfoObject('San Jose', 'Sharks');

                case 'STL':
                    return buildTeamInfoObject('St. Louis', 'Blues');

                case 'TB':
                case 'TBL':
                    return buildTeamInfoObject('Tampa Bay', 'Lightning');

                case 'TOR':
                    return buildTeamInfoObject('Toronto', 'Maple Leafs');

                case 'VAN':
                    return buildTeamInfoObject('Vancouver', 'Canucks');

                case 'VGK':
                    return buildTeamInfoObject('Vegas', 'Golden Knights');

                case 'WPG':
                    return buildTeamInfoObject('Winnipeg', 'Jets');

                case 'WSH':
                    return buildTeamInfoObject('Washington', 'Capitals');
            }
        }
    }
};
