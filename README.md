[![build and test](https://github.com/tfrizzell/lg-sportscentre/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/tfrizzell/lg-sportscentre/actions/workflows/build-and-test.yml)

> [!IMPORTANT]
> **Duthie Bot** has been shut down as of __March 18, 2024__ due to data scriping issues, and at the request of the sites it supports. If you'd like to continue with this type of functionality, you're encouraged to reach out to the teams behind [leaguegaming.com](https://leaguegaming.com/) and/or [thespnhl.com](https://thespnhl.com/) about developing bots specifically for their sites.

Duthie Bot
===============
**Duthie Bot** is the perfect way to keep your Discord server up to date with game results, trades, roster transactions, and many more pieces of information from our supported leagues.

How to Use
==========
To get started using **Duthie Bot**, you'll first need to authorize it to use your server. To accomplish this, go to https://discord.com/api/oauth2/authorize?client_id=356076231185268746&permissions=2048&scope=bot%20applications.commands, select the server you're adding it to, and click "Authorize".

<p align="center"><img alt="duthie-bot-discord-authorize.png" src="https://i.imgur.com/Tk4Tk8z.png" /></p>

Configuration
=============
As of *version 3*, **Duthie Bot** now runs using Discord's slash command feature. Simply type `/duthie` in your server, and follow the command palette.

Commands
========
#### Admin
Manage **Duthie Bot** administrators for your server.
```vb
# SYNTAX
  /duthie admin add @user
  /duthie admin list
  /duthie admin remove @user
```

#### List
List the available data in **Duthie Bot**.
```vb
# SYNTAX
  /duthie list admins
  /duthie list leagues (...filters)
  /duthie list sites (...filters)
  /duthie list teams (...filters)
  /duthie list watchers (...filters)
  /duthie list watcher-types
```

#### Ping
Sends a ping to **Duthie Bot** to make sure it's responding to commands.
```vb
# SYNTAX
  /duthie ping
```

#### Watcher
Manage **Duthie Bot** watchers for your server.
```vb
# SYNTAX
  /duthie watcher add [league] [team] [type] (#channel) (site)
  /duthie watcher list
  /duthie watcher remove [league] [team] [type] (#channel) (site)
  /duthie watcher remove-all
```

Supported Sites
===============
**Duthie Bot** currently supports the sites listed in the feature table below:

&nbsp;                  | **[leaguegaming.com](https://www.leaguegaming.com)** | **[myvirtualgaming.com](https://vghl.myvirtualgaming.com)** | **[thespnhl.com](https://thespnhl.com)**
------------------------|:----------------------------------------------------:|:-----------------------------------------------------------:|:----------------------------------------:
**Contract Signings**   | ✔️                                                   | ✔️                                                         | ❌                                      
**Daily Stars**         | ✔️                                                   | ❌                                                         | ❌                                      
**Draft Picks**         | ✔️                                                   | ✔️                                                         | ❌                                      
**Game Results**        | ✔️                                                   | ✔️                                                         | ✔️                                      
**Roster Transactions** | ✔️                                                   | ✔️                                                         | ❌                                      
**Team News**           | ✔️                                                   | ❌                                                         | ❌                                      
**Trades**              | ✔️                                                   | ✔️                                                         | ❌                                      
**Waiver Wire**         | ✔️                                                   | ❌                                                         | ❌                                      
**Winning Bids**        | ✔️                                                   | ✔️                                                         | ❌                                      

To view a list of supported leagues, use the `/duthie list leagues` command in your server.

License
=======
MIT
