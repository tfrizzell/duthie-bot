lg-sportscentre
===============
Originally written as a [Discord](https://discordapp.com/) webhook powered by a series of [PHP scripts](http://php.net/), **Duthie Bot** (code name: lg-sportscentre) has been reinvented as a [Discord](https://discordapp.com/) bot written in [NodeJS](https://nodejs.org/). Providing features such as game updates, news tracking, and daily star reporting, **Duthie Bot** is the perfect addition to your [LeagueGaming.com](http://www.leaguegaming.com) EA NHL team server.

Dependencies
============
 * [node.js](https://nodejs.org/) >= 6.11.3
 * [discord.js](//github.com/hydrabolt/discord.js) >= 11.2.1
 * [request](//github.com/request/request) >= 2.83.0
 * [node-cron](//github.com/kelektiv/node-cron) >= 1.3.0

How to Use
==========
To get started using **Duthie Bot**, you'll first need to authorize it to use your server. To accomplish this, go to https://discordapp.com/oauth2/authorize?&client_id=356076231185268746&scope=bot&permissions=0, select the server you're adding it to, and click "Authorize".

<p align="center"><img alt="duthie-bot-discord-authorize.png" src="https://i.imgur.com/Tk4Tk8z.png" /></p>

Once **Duthie Bot** has been added to your server, you will need to grant it permission to "Read Message" and "Send Messages" for any channel(s) you want it to have access to. Once **Duthie Bot** is able to read and send messages, you can begin configuring your update watchers.

Configuration
=============
From a channel **Duthie Bot** has access to, send the message `-lg help` to get started, or read the [Commands](#commands) section below.

Commands
========
#### List
Lists the set of data available to your server.
```vb
# SYNTAX
  -lg list <mode> [<type>[ <league>[ <team>[ <channel>]]]]

# MODE (required)
  There are currently three supported list modes:
      * leagues       - list all leagues supported by Duthie Bot
      * teams         - list all teams in actively supported leagues
      * watchers      - list all watchers on your server

# TYPE (optional, mode=watchers)
  When listing the watchers on your server, you can provide any valid watcher type to filter the output on. The valid watcher types are:
      * all (default) - an alias for all other types: bidding, contract, draft, games, news, trades, waivers
      * all-news      - an alias for all other news types: bidding, contract, draft, news, trades, waivers
      * bids          - announces any winning bids that match your league and/or team filters
      * contracts     - announces any new contracts that match your league and/or team filters
      * daily-stars   - announces any of the previous day's daily stars that match your league and/or team filters
      * draft         - announces any new draft picks that match your league and/or team filters
      * games         - announces any updated game scores that match your league and/or team filters
      * news          - announces any news items that don't fit any other type, passes through the Duthie Bot news filter, and that matches your league and/or team filters
      * trades        - announces any trades that match your league and/or team filters
      * waivers       - announces any players placed on or claimed off waivers that match your league and/or team filters

# LEAGUE (optional, mode=teams,watchers)
  When listing teams or watchers, you can provide any league to filter the output on. See -lg list leagues for a list of valid leagues.
  If specifying a league by name, be sure to wrap it in quotes (ex: "LGHL PSN") or remove any spaces (ex: LGHLPSN).

# TEAM (optional, mode=watchers)
  When listing watchers, you can provide any team to filter the output on. See -lg list teams for a list of valid teams.

  If specifying a team by name, be sure to wrap it in quotes (ex: "Columbus Blue Jackets") or remove any spaces (ex: ColumbusBlueJackets).

# CHANNEL (optional, mode=watchers)
  When listing watchers, you can provide any channel to filter the output on.
```

#### Ping
Sends a ping to **Duthie Bot** to make sure it's parsing and responding to messages
```vb
# SYNTAX
  -lg ping
```

#### Unwatch
Removes a watcher from your server's **Duthie Bot** data.
```vb
# SYNTAX
  -lg unwatch <type>[ <league>[ <team>]]

# TYPE (required)
  There are currently nine valid types:
      * all         - an alias for all other types: bidding, contract, draft, games, news, trades, waivers
      * all-news    - an alias for all other news types: bidding, contract, draft, news, trades, waivers
      * bids        - announces any winning bids that match your league and/or team filters
      * contracts   - announces any new contracts that match your league and/or team filters
      * daily-stars - announces any of the previous day's daily stars that match your league and/or team filters
      * draft       - announces any new draft picks that match your league and/or team filters
      * games       - announces any updated game scores that match your league and/or team filters
      * news        - announces any news items that don't fit any other type, passes through the Duthie Bot news filter, and that matches your league and/or team filters
      * trades      - announces any trades that match your league and/or team filters
      * waivers     - announces any players placed on or claimed off waivers that match your league and/or team filters

# LEAGUE (optional)
  The league argument is optional when deregistering a watcher. If omitted, all watchers that match the other arguments will be removed. To specify a league, simple enter the league's id or name found on LeagueGaming.com. For a list of valid leagues, see -lg list leagues.

  If specifying a league by name, be sure to wrap it in quotes (ex: "LGHL PSN") or remove any spaces (ex: LGHLPSN).

# TEAM (optional)
  The team argument is optional when deregistering a watcher. If omitted, all watchers that match the other arguments will be removed. To specify a team, simple enter the team's id or name found on LeagueGaming.com. For a list of valid teams, see -lg list teams.

  If specifying a team by name, be sure to wrap it in quotes (ex: "Toronto Maple Leafs") or remove any spaces (ex: TorontoMapleLeafs).
```

#### Watch
Adds a watcher to your server's **Duthie Bot** data.
```vb
# SYNTAX
  -lg watch <type> <league>[ <team>[ <channel>]]

# TYPE (required)
  There are currently nine valid types:
      * all         - an alias for all other types: bidding, contract, draft, games, news, trades, waivers
      * all-news    - an alias for all other news types: bidding, contract, draft, news, trades, waivers
      * bids        - announces any winning bids that match your league and/or team filters
      * contracts   - announces any new contracts that match your league and/or team filters
      * daily-stars - announces any of the previous day's daily stars that match your league and/or team filters
      * draft       - announces any new draft picks that match your league and/or team filters
      * games       - announces any updated game scores that match your league and/or team filters
      * news        - announces any news items that don't fit any other type, passes through the Duthie Bot news filter, and that matches your league and/or team filters
      * trades      - announces any trades that match your league and/or team filters
      * waivers     - announces any players placed on or claimed off waivers that match your league and/or team filters

# LEAGUE (required)
  To register a watcher with Duthie Bot, you must specify a supported league from LeagueGaming.com. Duthie Bot does not support leagueless watchers at this time. To specify a league, simply enter the league's id or name found on LeagueGaming.com. For a list of valid leagues, see -lg list leagues.

  If specifying a league by name, be sure to wrap it in quotes (ex: "LGHL PSN") or remove any spaces (ex: LGHLPSN).

# TEAM (optional)
  The team argument is optional for all types except games. Duthie Bot does not support teamless game watchers. To specify a team, simply enter the team's id or name found on LeagueGaming.com. For a list of valid teams, see -lg list teams.

  If specifying a team by name, be sure to wrap it in quotes (ex: "Columbus Blue Jackets") or remove any spaces (ex: ColumbusBlueJackets).

# CHANNEL (optional)
  If you want messages from this watcher sent to a specific channel, tag it at the end of the command. If none are specified, messages will be sent to the server's default channel.
```

License
=======
MIT
