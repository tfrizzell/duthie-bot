PRAGMA foreign_keys=off;
BEGIN TRANSACTION;

INSERT INTO leagues (id, siteId, leagueId, name, codename, extraData, enabled) VALUES 
 (1, 1, 37, 'LGHL', 'LGHL', '{"forumId":128,"seasonId":29}', 1),
 (2, 1, 38, 'LGAHL', 'LGAHL', '{"forumId":439,"seasonId":29}', 1),
 (3, 1, 39, 'LGCHL', 'LGCHL', '{"forumId":371,"seasonId":29}', 1),
 (4, 1, 67, 'LGHL PSN', 'LGHLPSN', '{"forumId":586,"seasonId":7}', 1),
 (5, 1, 68, 'LGAHL PSN', 'LGAHLPSN', '{"forumId":595,"seasonId":7}', 1),
 (6, 1, 69, 'LGCHL PSN', 'LGCHLPSN', '{"forumId":610,"seasonId":7}', 1),
 (7, 1, 90, 'ESHL', 'ESHL', '{"forumId":469,"seasonId":4}', 1),
 (8, 1, 91, 'ESHL PSN', 'ESHL PSN', '{"forumId":605,"seasonId":3}', 1),
 (9, 1, 97, 'LG World Cup', 'LGWORLDCUP', '{"forumId":187,"seasonId":2}', 1),
 (10, 2, 'vgnhl', 'VG NHL', 'VGNHL', '{}', 0),
 (11, 2, 'vgahl', 'VG AHL', 'VGAHL', '{}', 0),
 (12, 2, 'vgphl', 'VG PHL', 'VGPHL', '{}', 0),
 (13, 2, 'vghlwc', 'VG World Championship', 'VGWC', '{}', 0),
 (14, 2, 'vghlclub', 'VG Club', 'VGCLUB', '{}', 0);

INSERT INTO sites (id, siteId, name, enabled) VALUES
 (1, 'leaguegaming', 'LeagueGaming.com', 1),
 (2, 'myvirtualgaming', 'MyVirtualGaming.com', 1);

INSERT INTO watcher_types VALUES 
 (1, 'bids', 1),
 (2, 'contracts', 1),
 (3, 'daily-stars', 1),
 (4, 'draft', 1),
 (5, 'games', 1),
 (6, 'news', 1),
 (7, 'trades', 1),
 (8, 'waivers', 1);

COMMIT;
PRAGMA foreign_keys=on;