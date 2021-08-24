## Features

* Allows BI Directional syncing of Discord Roles and Oxide Groups.
* Announcements can be sent out in game chat and discord chat when a sync occurs.

## Discord Link
This plugin supports Discord Link provided by the Discord Extension.
This plugin will work with any plugin that provides linked player data through Discord Link.


### Multi Game Server Support

This plugin can work across multiple game server to one discord server. 
The only limitation is you cannot have multiple game servers syncing an oxide group to the same discord role.

## Configuration

```json
{
  "Discord Bot Token": "",
  "Discord Server ID (Optional if bot only in 1 guild)": "",
  "Sync Nicknames": false,
  "Update Rate (Seconds)": 2.0,
  "Use AntiSpamNames On Discord Nickname": false,
  "Sync Data": [
    {
      "Server Group": "Default",
      "Discord Role ID": "",
      "Sync Source (Server or Discord)": "Server",
      "Sync Notification Settings": {
        "Send message to Server": false,
        "Send Message To Discord": false,
        "Discord Message Channel (Name or ID)": "",
        "Send Message When Added": false,
        "Send Message When Removed": false,
        "Server Message Added Override Message": "",
        "Server Message Removed Override Message": "",
        "Discord Message Added Override Message": "",
        "Discord Message Removed Override Message": ""
      }
    },
    {
      "Server Group": "VIP",
      "Discord Role ID": "",
      "Sync Source (Server or Discord)": "Discord",
      "Sync Notification Settings": {
        "Send message to Server": false,
        "Send Message To Discord": false,
        "Discord Message Channel (Name or ID)": "",
        "Send Message When Added": false,
        "Send Message When Removed": false,
        "Server Message Added Override Message": "",
        "Server Message Removed Override Message": "",
        "Discord Message Added Override Message": "",
        "Discord Message Removed Override Message": ""
      }
    }
  ],
  "Plugin Log Level (None, Error, Warning, Info)": "Warning",
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

### Notification Tags
These are the tags that can be used in the notification messages

`{player.id}` - Player Steam ID  
`{player.name}` Player Name  
`{discord.id}` - Discord User ID  
`{discord.name}` - Discord User Name  
`{discord.discriminator}` - Discord Discriminator  
`{discord.nickname}` - Discord Nickname  
`{role.id}` - Discord Role ID  
`{role.name}` Discord Role Name  
`{group.name}` - Oxide Group Name

## Localization

```json
{
  "Chat": "[#BEBEBE][[#de8732]Discord Roles[/#]] {0}[/#]",
  "ServerMessageGroupAdded": "{player.name} has been added to oxide group {group.name}",
  "ServerMessageGroupRemoved": "{player.name} has been removed to oxide group {group.name}",
  "ServerMessageRoleAdded": "{player.name} has been added to discord role {role.name}",
  "ServerMessageRoleRemoved": "{player.name} has been removed to discord role {role.name}",
  "DiscordMessageGroupAdded": "{discord.name} has been added to oxide group {group.name}",
  "DiscordMessageGroupRemoved": "{discord.name} has been removed to oxide group {group.name}",
  "DiscordMessageRoleAdded": "{discord.name} has been added to discord role {role.name}",
  "DiscordMessageRoleRemoved": "{discord.name} has been removed to discord role {role.name}"
}
```