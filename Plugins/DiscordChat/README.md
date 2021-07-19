## Features

* Allows for bi direction chatting between a game server and discord channel.
* Any Discord linked players who chat in that discord chat channel will have their chat displayed on the game server
* All chat plugins that use the OnUserChat hook are supported as well as default game chat

## Configuration

```json
{
  "Discord Bot Token": "",
  "Discord Server ID": "",
  "Enable Adding Discord Tag To In Game Messages When Sent From Discord": true,
  "Channel Settings": {
    "Chat Channel ID": "",
    "Team Channel ID": "",
    "Join / Leave Channel ID": ""
  },
  "Message Settings": {
    "Replace Discord User Message With Bot Message": true,
    "Send Messages From Server Chat To Discord Channel": true,
    "Send Messages From Discord Channel To Server Chat": true,
    "Discord Message Server Time Offset (Hours)": 0.0,
    "Text Replacements": {
      "TextToBeReplaced": "ReplacedText"
    },
    "Unlinked Settings": {
      "Allow Unlinked Players To Chat With Server": true,
      "Steam Icon ID": 76561199144296099
    },
    "Message Filter Settings": {
      "Ignore messages from users in this list (Discord ID)": [],
      "Ignored Prefixes": []
    }
  },
  "Plugin Support": {
    "AdminChat Settings": {
      "Enable AdminChat Plugin Support": false,
      "Exclude From Chat Channel": true,
      "Admin Chat Channel ID": "",
      "Admin Chat Prefix": "@"
    },
    "ChatTranslator Settings": {
      "Discord Server Chat Language": "en"
    },
    "AntiSpamNames Settings": {
      "Use AntiSpamNames On Player Names": false,
      "Use AntiSpamNames On Chat Messages": false,
      "Use AntiSpamNames On Team Messages": false
    }
  },
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

### Note:
To disable channel sending functionality leave that channel blank.

## Localization
```json
{
  "Discord.JoinLeave.ConnectedMessage": "({0:HH:mm}) {1} has joined.",
  "Discord.JoinLeave.DisconnectedMessage": "({0:HH:mm}) {1} has disconnected. Reason: {2}",
  "Discord.ChatChannel.ChatMessage": "({0:HH:mm}) {1}: {2}",
  "Discord.ChatChannel.BetterChatMessage": "({0:HH:mm}) {1}",
  "Discord.ChatChannel.UnlinkedMessage": "({0:HH:mm}) {1}#{2}: {3}",
  "Discord.ChatChannel.NotLinked": "You're not allowed to chat with the server unless you are linked.",
  "Discord.TeamChannel.TeamChatMessage": "({0:HH:mm}) {1}: {2}",
  "Discord.TeamChannel.BetterChatTeamMessage": "({0:HH:mm}) {1}",
  "Discord.AdminChat.ChannelMessage": "({0:HH:mm}) {1} {2}",
  "Discord.AdminChat.NotLinked": "You're not allowed to use Admin Chat Channel unless you are linked.",
  "InGame.DiscordTag": "[#5f79d6][Discord][/#]",
  "InGame.UnlinkedChat": "{0} [#5f79d6]{1}#{2}[/#]: {3}",
  "InGame.InGameMessage": "{0} [#5f79d6]{1}[/#]: {2}",
  "InGame.ClanTag": "[{0}] "
}
```