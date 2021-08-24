## Features

* Allows players with permission to use discord to run server console commands

## Discord Link
This plugin supports Discord Link provided by the Discord Extension.
This plugin will work with any plugin that provides linked player data through Discord Link.

## Permissions

`discordcommands.use` - Allows players to use the /command discord bot command

## Discord Command

* `/exec {command}` -- will execute the given command on the server  
  **Example:** `/exec o.reload DiscordCore`  
**Note** Command can be configured in oxide/lang/DiscordCommands.json

## Configuration

```json
{
  "Discord Bot Token": "",
  "Discord Server ID (Optional if bot only in 1 guild)": "",
  "Command Settings": {
    "Allow Discord Commands In Direct Messages": true,
    "Allow Discord Commands In Guild": false,
    "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)": [],
    "Allow Commands for members having role (Role ID)": [],
    "Restrictions": {
      "Enable Command Restrictions": false,
      "Blacklist = listed commands cannot be used without permission, Whitelist = Cannot use any commands unless listed and have permission": "Blacklist",
      "Command Restrictions": {
        "command": {
          "Allowed Discord Roles": [
            "1234512321"
          ],
          "Allowed Server Groups": [
            "admin"
          ]
        }
      }
    }
  },
  "Log Settings": {
    "Log command usage in server console": true,
    "Command Usage Logging Channel ID": "",
    "Display Server Log Messages to user after running command": true,
    "Display Server Log Messages Duration (Seconds)": 1.0
  },
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

## Localization
```json
{
  "NoPermission": "You do not have permission to use this command",
  "Blacklisted": "This command is blacklisted and you do not have permission to use it.",
  "WhiteListedNotAdded": "This command is not added to the command whitelist and cannot be used.",
  "WhiteListedNoPermission": "You do not have the whitelisted permission to use this command.",
  "CommandInfoText": "To execute a command on the server",
  "RanCommand": "Ran Command: {0}",
  "ExecCommand": "exec",
  "CommandLogging": "{0} ran command '{1}'",
  "CommandHelpTextV2": "Send commands to the rust server:\nType /{0} {{command}} - to execute that command on the server\nExample: /{0} o.reload DiscordCommand"
}
```
