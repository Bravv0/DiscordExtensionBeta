## Features

* Adds the `/players` command
* Returns a list of all the connected players

## Permissions

* `discordplayers.use` - Allows players to use the `/players` discord command

## Discord Commands

* `/players` -- returns a list of all the connected players

## Configuration

```json
{
  "Discord Bot Token": "",
  "Allow Discord Commands In Direct Messages": true,
  "Require User To Be Linked To Use Command": false,
  "Allow Discord Commands In Guild": false,
  "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)": [],
  "Allow Guild Commands for members having role (Role ID)": [],
  "Player name separator": "\n",
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

## Localization
```json
{
  "NoPermission": "You do not have permission to use this command",
  "DiscordFormat": "[Discord Players] {0}",
  "ListFormat": "{0} Online: \n{1}",
  "PlayerFormat": "{clan}{name} ({steamid})",
  "PlayersCommand": "players"
}
```

### Format Arguments
`{clan}` - displays a players clan tag  
`{name}` - displays a players name  
`{steamid}` - displays a players steam ID

