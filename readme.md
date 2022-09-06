# TeamspeakActivityBot
## What is this?
TeamspeakActivityBot is a bot written in C# for TeamSpeak 3.

## What can it do?
Track active as well as total connected time of users, update a channel to provide a leaderboard showing which users are most active or connected and some minor fun facts.

Provide some chat commands in the serverwide chat.
(It's sadly not possible to react to commands in all channels, because the query client would have to be in all channels simultaneously.)

## Requirements to build
- Visual Studio 2022
- Just [my fork](https://github.com/cub1con/TeamSpeak3QueryApi) of nikeee's awesome [TeamSpeak3Query API](https://github.com/nikeee/TeamSpeak3QueryApi). (I fixed a deadlock in my fork to prevent locking up the bot if too many commands were processed at once).
