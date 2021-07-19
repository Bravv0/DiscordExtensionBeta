# Discord Command

DiscordCommand is a library that allows plugin to register Discord Command callback similar to Oxide Chat Commands.
DiscordCommand will be called for any message created by a non bot.
When a plugin that has registered commands is unloaded then the commands will be removed.
This does **NOT** require a person to have linked with DiscordLink.

## Configuration

DiscordCommand has configuration options when can be set similar to oxide config. 
Once the Discord Extension has been loaded for the first time you will find a discord.config.json file in your oxide folder.
This configuration allows you to configure the command prefix for discord commands.

## Getting Started

If you take the method approach to registering Discord Commands you need to include this in your plugin.
```c#
private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();
```

## Callback

All commands have the following callback arguments.
On a DiscordMessage you are able to get the Channel ID, Guild ID, and DiscordUser for the message. If the user is linked you can get the IPlayer from the DiscordUser.

```c#
void MyChatCommand(DiscordMessage message, string cmd, string[] args)
```

## Registering Commands

There are multiple ways to register commands within the extension that are similar to oxide's chat system.

### Attributes

Attributes can be used to define commands in direct message or guild channels.
If you pass a 2nd argument of true the extension will use all lang files and use the specified key for the command.
**Note:** Using methods to register commands provides more control for guild channels.

```c#
// Supports commands that are used in a direct message to the bot
[DirectMessageCommand("mycommand")]
[DirectMessageCommand("mylangkey", true)]
void MessageCommand(DiscordMessage message, string cmd, string[] args)

// Supports commands that are used in a guild
[GuildCommand("mycommand")]
[GuildCommand("mylangkey", true)]
void MessageCommand(DiscordMessage message, string cmd, string[] args)
```

### Methods

Methods can also be used to define commands. Methods allows more control over command registration.
When registering guild commands you can specify allowed guild channels / categories

```c#
// Registers "command" as a direct message command
_dcCommands.AddDirectMessageCommand("command", this, "methodname");

// Registers the lang key key from the plugin as a direct message command for all lang files
_dcCommands.AddDirectMessageLocalizedCommand("langkey", this, "methodname");

// Registers "command" as a guild command allowed only in channel with ID 123445678
// Notes: You can pass an empty list or null for the snowflake list to allow for all channels
_dcCommands.AddGuildCommand("command", this, new List<Snowflake> { (Snowflake)123445678}, "methodname");

// Registers the lang key key from the plugin as a guild command allowed only in channel with ID 123445678
// Notes: You can pass an empty list or null for the snowflake list to allow for all channels
_dcCommands.AddGuildLocalizedCommand("langkey", this, new List<Snowflake> { (Snowflake)123445678},"methodname");
```

### Helpers

These helpers can be added to your plugin to make it easier with registering commands by methods.

```c#
/// <summary>
/// Registers commands with discord
/// </summary>
/// <param name="command">Name of the method to use in callback</param>
/// <param name="commandName">The name of the command</param>
/// <param name="direct">Should we register this command for direct messages</param>
/// <param name="guild">Should we register this command for guilds</param>
/// <param name="allowedChannels">If registering guilds the allowed channels / categories this command can be used in</param>
public void RegisterDiscordCommand(string commandName, string command, bool direct, bool guild, List<Snowflake> allowedChannels)
{
    if (direct)
    {
        _dcCommands.AddDirectMessageCommand(commandName, this, command);
    }

    if (guild)
    {
        _dcCommands.AddGuildCommand(commandName, this, allowedChannels, command);
    }
}
```

```c#
/// <summary>
/// Registers commands with discord using lang keys
/// </summary>
/// <param name="command">Name of the method to use in callback</param>
/// <param name="langKey">The name of the lang key dictionary</param>
/// <param name="direct">Should we register this command for direct messages</param>
/// <param name="guild">Should we register this command for guilds</param>
/// <param name="allowedChannels">If registering guilds the allowed channels / categories this command can be used in</param>
public void RegisterDiscordLangCommand(string command, string langKey, bool direct, bool guild, List<Snowflake> allowedChannels)
{
    if (direct)
    {
        _dcCommands.AddDirectMessageLocalizedCommand(langKey, this, command);
    }

    if (guild)
    {
        _dcCommands.AddGuildLocalizedCommand(langKey, this, allowedChannels, command);
    }
}
```