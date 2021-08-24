## Features

* Allows players to private message each other even if they're not connected to the rust server
* The players must have linked their discord and game accounts first
* All discord private messages will be send by the bot in a private message

## Discord Link
This plugin supports Discord Link provided by the Discord Extension.
This plugin will work with any plugin that provides linked player data through Discord Link.

## Chat Commands

* `/pm MJSU Hi` -- will send a private message to MJSU wit the text Hi
* `/r Hi` -- Will reply to the last received message with the text Hi

## Discord Commands

* `/pm MJSU Hi` -- will send a private message to MJSU wit the text Hi
* `/r Hi` -- Will reply to the last received message with the text Hi

## Configuration

```json
{
  "Discord Bot Token": "",
  "Allow Discord Commands In Direct Messages": true,
  "Allow Discord Commands In Guild": false,
  "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)": [],
  "Enable Effect Notification": true,
  "Notification Effect": "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab",
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

## Localization
```json
{
  "Chat": "[#BEBEBE][[#de8732]Discord PM[/#]] {0}[/#]",
  "DiscordChatPrefixV1": "[#BEBEBE][#de8732]PM {0} {1}:[/#] {2}[/#]",
  "InvalidPmSyntaxV1": "Invalid Syntax. Type [#de8732]{0}{1} MJSU Hi![/#]",
  "InvalidReplySyntaxV1": "Invalid Syntax. Ex: [#de8732]{0}{1} Hi![/#]",
  "NoPreviousPm": "You do not have any previous discord PM's. Please use /pm to be able to use this command.",
  "NoPlayersFound": "No players found with the name '{0}'",
  "MultiplePlayersFound": "Multiple players found with the name '{0}'.",
  "From": "from",
  "To": "to",
  "Commands.Chat.PM": "pm",
  "Commands.Chat.Reply": "r",
  "Commands.Discord.PM": "pm",
  "Commands.Discord.Reply": "r"
}
```