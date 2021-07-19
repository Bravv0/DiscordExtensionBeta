# Welcome

Welcome to the Discord Extension Beta. 
This is were all testing for the Discord Extension V2.0.0 will take place. 
If you're looking to test out the latest extension please grab the latest extension from [Here](Extension/Oxide.Ext.Discord.dll).
All currently supported plugins can be found [Here](Plugins).

## Major Changes

**Overview of Changes:**

- **Single Websocket Per Bot Token**: If multiple plugins are using the same bot token then those plugins will be grouped together for websocket and REST calls. 
This mean that reloading your plugin will no longer reset the websocket. 
I'm hoping with this update to solve the connection issues but if you're still not trusting of it there are methods available to disconnect and reconnect the web socket.


- **Discord API V9**: Currently the discord extension V1 uses Discord API V6. 
This version has been deprecated and will stop working in the future. 
V9 is the latest version and will be constantly updated for any API changes.


- **Gateway Intents**: Switching to API >= V8 requires us to use Gateway Intents. 
These intents are to be specified when connecting and let Discord know which socket events your bot wants to listen for. I recommend only specifying the intents that you need. I will have documentation indicating what each intent will give you. If you have multiple plugins sharing a bot token and requires new intents not previously requested then the bot will disconnect and reconnect to the web socket with the updated intents. This switch will help with performance because we aren't receiving events we don't care about anymore.


- **REST Error Callback**: Sometimes REST calls fail and there is no way to know if that has happened in the current version. 
With the new version you have the option to listen for errors on all REST calls.
  

- **Hook Overhaul**: Hooks have been changed and renamed to better fit oxide naming conventions. 
Hooks have been split up so that hooks relating to Direct Messages / Guilds have a separate hook for each. For example `OnDiscordDirectMessageCreated` and `OnDiscordGuildMessageCreated`. Where possible Channel and Guild objects have been added to hook calls.


- **Performance**: Performance is one of the biggest concerns people have had with Discord Extension V1. 
While these issues wouldn't be noticed in smaller discord servers they would appear in server with a large number of users.
There were multiple issues that combined together made things worse which have been solved in V2. Here are some of the changes that have been made to improve performance.
    - Combining all bots using that use the same discord token to use a single websocket connection and not per plugin. 
        This will prevent the server from performing the same work multiple times.
    - All lists of discord entities have been replaced with Hash's instead. 
      This fixes having to loop over every Guild Member to find the discord user you were looking for.
    - Gateway intents were added in Discord API v8. This will require each plugin to specify which Discord gateway event a plugin want's to listen to.
        Discord will only send those discord events over the websocket which improves performance for us as well.
    - There are many more changes that aren't as impactful as these as well.  
    

## New Features:
- Added the ability for plugins to upload files to discord during message create / update
- Added support for Discord Message Components [Learn More](https://discord.com/developers/docs/interactions/message-components)
- Added support for Discord Slash Commands [Learn More](https://blog.discord.com/slash-commands-are-here-8db0a385d9e6)
- Added a universal linking API provided by [DiscordLink](Extension/Developers/Docs/DiscordLink.md). This allows plugins that use DiscordLink to use any discord link plugin they wish and not have to worry about compatability.
- Added the ability for plugins to register commands in discord similar to how it's done in oxide [DiscordCommand](Extension/Developers/Docs/DiscordCommand.md)
- Added the ability for plugins to register to DiscordMessages in specific channels using [DiscordSubscriptions](Extension/Developers/Docs/DiscordSubscriptions.md)
- Replaced using strings for Discord ID's with [Snowflake](Extension/Developers/Docs/Snowflake.md). Snowflake is backwards compatible with using string and only the type needs to be changed.