PRAGMA foreign_keys=off;
BEGIN TRANSACTION;

CREATE TABLE data_games (
 id integer NOT NULL PRIMARY KEY,
 leagueId integer NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 gameId text NOT NULL,
 timestamp text NOT NULL,
 visitorTeam integer NOT NULL,
 visitorScore integer,
 homeTeam integer NOT NULL,
 homeScore integer,
 queued integer NOT NULL DEFAULT 0,
 UNIQUE (leagueId, gameId)
);

CREATE TABLE data_news (
 id integer NOT NULL PRIMARY KEY,
 leagueId text NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 newsId text NOT NULL,
 teams text NOT NULL DEFAULT '',
 message text NOT NULL,
 timestamp text NOT NULL,
 queued integer NOT NULL DEFAULT 1,
 UNIQUE (leagueId, newsId)
);

CREATE TABLE guilds (
 guildId text NOT NULL PRIMARY KEY,
 defaultChannelId text,
 deleted integer
) WITHOUT ROWID;

CREATE TABLE guild_admins (
 guildId text NOT NULL REFERENCES guilds (guildId) ON DELETE CASCADE ON UPDATE CASCADE,
 memberId text NOT NULL,
 PRIMARY KEY (guildId, memberId)
) WITHOUT ROWID;

CREATE TABLE league_teams (
 leagueId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 teamId text NOT NULL REFERENCES teams (teamId) ON DELETE CASCADE ON UPDATE CASCADE,
 PRIMARY KEY (leagueId, teamId)
) WITHOUT ROWID;

CREATE TABLE leagues (
 id integer NOT NULL PRIMARY KEY,
 siteId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 leagueId text NOT NULL,
 name text NOT NULL UNIQUE,
 codename text NOT NULL UNIQUE,
 extraData text NOT NULL DEFAULT '{}',
 enabled integer NOT NULL DEFAULT 1,
 UNIQUE (siteId, leagueId)
);

CREATE TABLE sites (
 id integer NOT NULL PRIMARY KEY,
 siteId text UNIQUE,
 name text NOT NULL UNIQUE,
 enabled integer NOT NULL DEFAULT 1
);

CREATE TABLE teams (
 siteId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 teamId text NOT NULL,
 name text NOT NULL,
 shortname text NOT NULL,
 PRIMARY KEY (siteId, teamId)
) WITHOUT ROWID;

CREATE TABLE watcher_types (
 id integer NOT NULL PRIMARY KEY,
 name text UNIQUE,
 enabled integer NOT NULL DEFAULT 1
);

CREATE TABLE watchers (
 id integer NOT NULL PRIMARY KEY,
 guildId text NOT NULL REFERENCES guilds (guildId) ON DELETE CASCADE ON UPDATE CASCADE,
 typeId integer NOT NULL REFERENCES watcher_types (id) ON DELETE CASCADE ON UPDATE CASCADE,
 leagueId integer NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 teamId integer REFERENCES teams (id) ON DELETE CASCADE ON UPDATE CASCADE,
 channelId integer NOT NULL DEFAULT '',
 deleted integer,
 UNIQUE (guildId, typeId, leagueId, teamId, channelId)
);

CREATE TRIGGER queue_game AFTER UPDATE ON data_games
WHEN 
	IFNULL(new.queued, 0) != 1 AND new.visitorScore IS NOT NULL AND new.homeScore IS NOT NULL AND 
	(new.visitorTeam != old.visitorTeam OR new.visitorScore != IFNULL(old.visitorScore, -1) OR new.homeTeam != old.homeTeam OR new.homeScore != IFNULL(old.homeScore, -1))
BEGIN
	UPDATE data_games SET queued = 1 WHERE id = new.id;
END;

COMMIT;
PRAGMA foreign_keys=on;