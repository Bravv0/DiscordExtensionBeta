## Features

* Creates a secure link between an in game player and a Discord user in your Discord server. 
* This link can be accessed by any Discord Link compatible plugin.

**Note:** This plugin only performs discord link functionality and should not be loaded otherwise.

## Discord Link
This plugin supports Discord Link provided by the Discord Extension. 
All plugins that support Discord Link can use this plugin as the discord linking plugin.

## Permissions

* `discordcore.use` - Allows players to use the `/dc` chat command

## Linking

**Note:**
* In order for the bot to work the user who is trying to link must be in the Discord server the bot is apart of.

### Starting In-Game

* Option 1: Type /dc join code in game and write the command in a private message to the bot
* Option 2: Type /dc join username#discrimiator and respond to the private message to the bot

### Starting in Discord

* Option 1: Type /dc join and paste the command in game chat.
* Option 2: If enabled click on the link button in the guild channel and paste the command in game chat.

## Commands

`/dc join` - to start the link process  
`/dc leave` - to to unlink yourself from discord  
`/dc` to see this message again  

## Configuration

```json
{
  "Discord Bot Token": "",
  "Discord Server ID (Optional if bot only in 1 guild)": "",
  "Discord Server Name Override": "",
  "Discord Server Join Code": "",
  "Enable Discord Server Welcome DM Message": true,
  "Link Settings": {
    "Link Code Generator Characters": "123456789",
    "Link Code Length": 6,
    "Allow Commands To Be Used In Guild Channels": false,
    "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)": [],
    "Link / Unlink Announcement Channel Id": "",
    "Guild Link Message Settings": {
      "Enable Guild Link Message": false,
      "Message Channel ID": ""
    }
  },
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

### Guild Link Message
Guild link message will place a message in a guild discord channel with a button users can use to start the linking process.
The players will be sent a message in the same channel that only they can see and isn't displayed to any other users.

![](https://i.postimg.cc/fbky50pw/link-example.png)

## Localization
```json
{
  "NoPermission": "You do not have permission to use this command",
  "ChatFormat": "[#BEBEBE][[#de8732]Discord Core[/#]] {0}[/#]",
  "DiscordFormat": "[Discord Core] {0}",
  "DiscordCoreOffline": "Discord Core is not online. Please try again later",
  "Commands.Unknown": "Unknown Command",
  "GenericError": "We have encountered an error trying. PLease try again later.",
  "ConsolePlayerNotSupported": "You cannot use this command from the server console.",
  "Commands.LeaveErrorsNotLinked": "We were unable to unlink your account as you do not appear to have been linked.",
  "Commands.Join.Modes": "Please select which which mode you would like to use to link with discord.\nIf you wish to join using a code please type [#de8732]/{0} {1} {2}[/#]\nIf you wish to join by your discord username please type [#de8732]/{0} {1} {{Username}}#{{Discriminator}}[/#]",
  "Commands.Join.Errors.AlreadySignedUp": "You have already linked your discord and game accounts. If you wish to remove this link type [#de8732]{0}{1} {2}[/#]",
  "Commands.Join.Errors.UnableToFindUser": "Unable to find user '{0}' in the {1} discord server. Have you joined the {1} discord server @ [#de8732]discord.gg/{2}[/#]?",
  "Commands.Join.Errors.FoundMultipleUsers": "Found multiple users with username '{0}' in the {1} discord server. Please include more of the username and discriminator if possible.",
  "Commands.Join.Errors.UsernameSearchError": "An error occured while trying to search by username. Please try a different username or try again later.",
  "Commands.Join.Errors.InvalidSyntax": "Invalid syntax. Type [#de8732]{0}{1} code 123456[/#] where 123456 is the code you got from discord",
  "Commands.Join.Errors.NoPendingActivations": "There is no link currently in progress with the code '{0}'. Please confirm your code and try again.",
  "Commands.Join.Errors.LinkInProgress": "You already have an existing link in process. Please continue from that link.",
  "Commands.Join.Errors.Banned": "You have been banned from any more link attempts.",
  "Commands.Join.Complete.Info": "To completed your activate please use the following command: \"{0}{1} {2} {3}\".\n",
  "Commands.Join.Complete.InfoGuildAny": "This command can be used in any guild channel.\n",
  "Commands.Join.Complete.InfoGuildChannel": "This command can only used in the following guild channels / categories {0}.\n",
  "Commands.Join.Complete.InfoAlsoDm": "This command can also be used in a direct message to guild bot {0}",
  "Commands.Join.Complete.InfoDmOnly": "This command can only be used in a direct message to the guild bot {0}",
  "Commands.Join.Messages.Discord.Accept": "Accept",
  "Commands.Join.Messages.Discord.Decline": "Decline",
  "Commands.Join.Messages.Discord.LinkAccounts": "Link Accounts",
  "Commands.Join.Messages.Discord.Username": "The player '{0}' is trying to link their game account to this discord.\nIf you could like to accept please click on the {1} button.\nIf you did not initiate this link please click on the {2} button",
  "Commands.Join.Messages.Chat.UsernameDmSent": "Our bot {0} has sent you a discord direct message. Please finish your setup there.",
  "Commands.Join.Messages.Discord.CompletedInGame": "To completed your activation please use the following command: **{0}{1} {2} {3}** in game.",
  "Commands.Join.Messages.Discord.CompleteInGameResponse": "Please check your DM's for steps on how to complete the link process.",
  "Commands.Join.Messages.Discord.Declined": "We have successfully declined the link request. We're sorry for the inconvenience.",
  "Commands.Join.Messages.Chat.Declined": "Your join request was declined by the discord user. Repeated declined attempts will result in a link ban.",
  "Linking.Chat.Linked": "You have successfully linked with discord user {0}#{1}.",
  "Linking.Discord.Linked": "You have successfully linked your discord {0}#{1} with in game player {2}",
  "Linking.Chat.Unlinked": "You have successfully unlinked player {0} with your discord account.",
  "Linking.Discord.Unlinked": "You have successfully unlinked your discord {0}#{1} with in game player {2}",
  "Notifications.Link": "Player {0}({1}) has linked with discord {2}({3})",
  "Notifications.Rejoin": "Player {0}({1}) has rejoined and was linked with discord {2}({3})",
  "Notifications.Unlink": "Player {0}({1}) has unlinked their discord {2}({3})",
  "Guild.WelcomeMessage": "Welcome to the {0} discord server. If you would like to link your discord account to {1} game server please respond to this message with {2}{3} {4}.",
  "Guild.LinkMessage": "Welcome to the {0} discord server. This server supports linking your discord and in game accounts. If you would like to begin this process please click on the {1} button below this message.\n__Note: You must be in game to complete the link.__",
  "Emoji.Accept": "✅",
  "Emoji.Decline": "❌",
  "ChatHelpText": "Allows players to link their player and discord accounts together. Players must first join the {0} Discord @ [#de8732]discord.gg/{1}[/#]\nType [#de8732]/{2} {3} [/#] to start the link process\nType [#de8732]/{2} {4}[/#] to to unlink yourself from discord\nType [#de8732]/{2}[/#] to see this message again",
  "DiscordHelpText": "Allows players to link their in game player and discord accounts together.\nType {0}{1} {2} to start the link process\nType {0}{1} {3} to to unlink yourself from discord\nType {0}{1} to see this message again",
  "DiscordCoreChatCommand": "dc",
  "DiscordCoreChatCommand.Join": "join",
  "DiscordCoreChatCommand.Leave": "leave",
  "DiscordCoreChatCommand.Join.Code": "code",
  "DiscordCoreMessageCommand": "dc",
  "DiscordCoreMessageCommand.Join": "join",
  "DiscordCoreMessageCommand.Leave": "leave"
}
```