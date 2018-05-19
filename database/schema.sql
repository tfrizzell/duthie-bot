BEGIN TRANSACTION;


/**
 * CREATE TABLES ...
 */
 CREATE TABLE data_daily_stars (
 leagueId integer NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 posGroup text NOT NULL,
 rank integer NOT NULL,
 team text NOT NULL,
 name text NOT NULL,
 position text NOT NULL,
 metadata text NOT NULL DEFAULT '{}',
 timestamp text NOT NULL,
 queued integer NOT NULL DEFAULT 1,
 PRIMARY KEY (leagueId, posGroup, rank)
) WITHOUT ROWID;

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
 message text NOT NULL,
 type text DEFAULT NULL,
 timestamp text NOT NULL,
 queued integer NOT NULL DEFAULT 1,
 UNIQUE (leagueId, newsId)
);

CREATE TABLE data_news_team_map (
 newsId integer NOT NULL REFERENCES data_news (id) ON DELETE CASCADE ON UPDATE CASCADE,
 siteId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 mappedTeamId text NOT NULL,
 PRIMARY KEY (newsId, siteId, mappedTeamId),
 FOREIGN KEY (siteId, mappedTeamId) REFERENCES team_map (siteId, mappedTeamId) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE guild_admins (
 guildId text NOT NULL REFERENCES guilds (id) ON DELETE CASCADE ON UPDATE CASCADE,
 memberId text NOT NULL,
 PRIMARY KEY (guildId, memberId)
) WITHOUT ROWID;

CREATE TABLE guilds (
 id text NOT NULL PRIMARY KEY,
 archived text
) WITHOUT ROWID;

CREATE TABLE league_team_map (
 leagueId integer NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 teamId integer NOT NULL REFERENCES teams (id) ON DELETE CASCADE ON UPDATE CASCADE,
 PRIMARY KEY (leagueId, teamId)
) WITHOUT ROWID;

CREATE TABLE leagues (
 id integer NOT NULL PRIMARY KEY,
 siteId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 leagueId text NOT NULL,
 name text NOT NULL UNIQUE,
 codename text NOT NULL UNIQUE,
 extraData text NOT NULL DEFAULT '{}',
 customTeams integer NOT NULL DEFAULT 0,
 disabled integer NOT NULL DEFAULT 0,
 UNIQUE (siteId, leagueId)
);

CREATE TABLE sites (
 id integer NOT NULL PRIMARY KEY,
 siteId text UNIQUE,
 name text NOT NULL UNIQUE
);

CREATE TABLE team_map (
 siteId integer NOT NULL REFERENCES sites (id) ON DELETE CASCADE ON UPDATE CASCADE,
 mappedTeamId text NOT NULL,
 teamId integer NOT NULL REFERENCES teams (id) ON DELETE CASCADE ON UPDATE CASCADE,
 PRIMARY KEY(siteId, mappedTeamId)
) WITHOUT ROWID;

CREATE TABLE teams (
 id integer NOT NULL PRIMARY KEY,
 name text NOT NULL UNIQUE,
 shortname text NOT NULL,
 codename text UNIQUE,
 codeshortname text
);

CREATE TABLE watcher_types (
 id integer NOT NULL PRIMARY KEY,
 name text UNIQUE,
 description text NOT NULL
);

CREATE TABLE watchers (
 id integer NOT NULL PRIMARY KEY,
 guildId text NOT NULL REFERENCES guilds (id) ON DELETE CASCADE ON UPDATE CASCADE,
 typeId integer NOT NULL REFERENCES watcher_types (id) ON DELETE CASCADE ON UPDATE CASCADE,
 leagueId integer NOT NULL REFERENCES leagues (id) ON DELETE CASCADE ON UPDATE CASCADE,
 teamId integer REFERENCES teams (id) ON DELETE CASCADE ON UPDATE CASCADE,
 channelId text,
 archived text,
 UNIQUE (guildId, typeId, leagueId, teamId, channelId)
);


/**
 * CREATE TRIGGERS ...
 */
CREATE TRIGGER check_unique_watcher_insert BEFORE INSERT ON watchers
WHEN
	NEW.teamId IS NULL OR NEW.channelId IS NULL
BEGIN
	SELECT CASE WHEN((
		SELECT 1 FROM watchers 
			WHERE 
				guildId = NEW.guildId AND 
				typeId = NEW.typeId AND 
				leagueId = NEW.leagueId AND 
				(teamId = NEW.teamId OR (teamId IS NULL AND NEW.teamId IS NULL)) AND
				(channelId = NEW.channelId OR (channelId IS NULL AND NEW.channelId IS NULL)) AND
				archived IS NULL
		) NOTNULL)
	THEN
		RAISE(ABORT, "UNIQUE constraint failed: watchers.id")
	END;
END;

CREATE TRIGGER check_unique_watcher_update BEFORE UPDATE ON watchers
WHEN
	NEW.teamId IS NULL OR NEW.channelId IS NULL
BEGIN
	SELECT CASE WHEN((
		SELECT 1 FROM watchers 
			WHERE 
				guildId = NEW.guildId AND 
				typeId = NEW.typeId AND 
				leagueId = NEW.leagueId AND 
				(teamId = NEW.teamId OR (teamId IS NULL AND NEW.teamId IS NULL)) AND
				(channelId = NEW.channelId OR (channelId IS NULL AND NEW.channelId IS NULL)) AND
				archived IS NULL
		) NOTNULL)
	THEN
		RAISE(ABORT, "UNIQUE constraint failed: watchers.id")
	END;
END;

CREATE TRIGGER game_queue AFTER UPDATE ON data_games
WHEN
	IFNULL(NEW.queued, 0) != 1 AND NEW.visitorScore IS NOT NULL AND NEW.homeScore IS NOT NULL AND
	(NEW.visitorTeam != OLD.visitorTeam OR NEW.visitorScore != IFNULL(OLD.visitorScore, -1) OR NEW.homeTeam != OLD.homeTeam OR NEW.homeScore != IFNULL(OLD.homeScore, -1))
BEGIN
	UPDATE data_games SET queued = 1 WHERE id = NEW.id;
END;

CREATE TRIGGER news_team_added AFTER INSERT ON data_news_team_map
WHEN
	(SELECT message FROM data_news WHERE id = NEW.newsId) LIKE '%::team=%::%'
BEGIN
	UPDATE data_news SET message = REPLACE(message, '::team=' || NEW.mappedTeamId || '::', (SELECT team.name FROM team_map map JOIN teams team ON team.id = map.teamId WHERE map.siteId = NEW.siteId AND map.mappedTeamId = NEW.mappedTeamId)) WHERE id = NEW.newsId;
END;

CREATE TRIGGER team_create_codename AFTER INSERT ON teams
BEGIN
	UPDATE teams SET
		codename = UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(NEW.name, '/', ''), '-', ''), '''', ''), '.', ''), ' ', '')),
		codeshortname = UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(NEW.shortname, '/', ''), '-', ''), '''', ''), '.', ''), ' ', ''))
	WHERE id = NEW.id;
END;

CREATE TRIGGER team_update_codename AFTER UPDATE ON teams
WHEN
	NEW.codename != OLD.codename OR NEW.codename = ''
BEGIN
	UPDATE teams SET 
		codename = UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(NEW.name, '/', ''), '-', ''), '''', ''), '.', ''), ' ', '')),
		codeshortname = UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(NEW.shortname, '/', ''), '-', ''), '''', ''), '.', ''), ' ', ''))
	WHERE id = NEW.id;
END;


/**
 * CREATE VIEWS ...
 */
 CREATE VIEW daily_stars AS SELECT
 star.leagueId,
 league.name AS leagueName,
 star.posGroup AS 'group',
 star.rank,
 team.id AS teamId,
 team.name AS teamName,
 star.name AS playerName,
 star.position,
 star.metadata,
 star.timestamp,
 star.queued
FROM
 data_daily_stars star
 JOIN leagues league ON league.id = star.leagueId
 JOIN team_map map ON map.siteId = league.siteId AND map.mappedTeamId = star.team
 JOIN teams team ON team.id = map.teamId
WHERE
 league.disabled = 0
ORDER BY
 CASE
  WHEN star.posGroup = 'forwards' THEN 1
  WHEN star.posGroup = 'defenders' THEN 2
  WHEN star.posGroup = 'goalies' THEN 3
  ELSE 4
 END,
 star.posGroup ASC,
 star.rank ASC;

CREATE VIEW games AS SELECT
 game.id,
 game.leagueId,
 league.name AS leagueName,
 game.gameId,
 game.timestamp,
 visitor.id AS visitorTeam,
 visitor.name AS visitorName,
 visitor.shortname AS visitorShortname,
 game.visitorScore,
 home.id AS homeTeam,
 home.name AS homeName,
 home.shortname AS homeShortname,
 game.homeScore,
 league.customTeams,
 game.queued
FROM
 data_games game
 JOIN leagues league ON league.id = game.leagueId
 LEFT JOIN team_map vis ON vis.siteId = league.siteId AND vis.mappedTeamId = game.visitorTeam
 LEFT JOIN teams visitor ON visitor.id = vis.teamId
 LEFT JOIN team_map hom ON hom.siteId = league.siteId AND hom.mappedTeamId = game.homeTeam
 LEFT JOIN teams home ON home.id = hom.teamId
WHERE
 league.disabled = 0
GROUP BY
 game.id
ORDER BY
 game.timestamp ASC,
 game.gameId ASC,
 game.id ASC;

CREATE VIEW news AS SELECT
 news.id,
 news.leagueId,
 league.name AS leagueName,
 news.newsId,
 GROUP_CONCAT(team.id) AS teams,
 news.message,
 news.type,
 news.timestamp,
 league.customTeams,
 news.queued
FROM
 data_news news
 JOIN leagues league ON league.id = news.leagueId
 LEFT JOIN data_news_team_map map ON map.newsId = news.id
 LEFT JOIN team_map teams ON teams.siteId = league.siteId AND teams.mappedTeamId = map.mappedTeamId
 LEFT JOIN teams team ON team.id = teams.teamId
WHERE
 league.disabled = 0
GROUP BY
 news.id
ORDER BY
 news.timestamp ASC,
 news.id ASC;


COMMIT;