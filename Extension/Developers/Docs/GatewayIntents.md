# Gateway Intents

Gateway intents control which events your bot will receive from the websocket. 
It is recommended that you only request intents that your plugin needs.
If there are multiple plugins used by the same bot with different intents the Discord Extension will handle updating the bot with the new intents it needs to function.


## Available Intents

This is a list of all intents and which hooks become available when using a specific one.

* Guilds (`GatewayIntents.Guilds`)
    * [OnDiscordGuildCreated](Hooks.md#ondiscordguildcreated)
    * [OnDiscordGuildUpdated](Hooks.md#ondiscordguildupdated)
    * [OnDiscordGuildUnavailable](Hooks.md#ondiscordguildunavailable)
    * [OnDiscordGuildDeleted](Hooks.md#ondiscordguildchanneldeleted)
    * [OnDiscordGuildChannelCreated](Hooks.md#ondiscordguildchannelcreated)
    * [OnDiscordGuildChannelUpdated](Hooks.md#ondiscordguildchannelupdated)
    * [OnDiscordDirectChannelDeleted](Hooks.md#ondiscordguildchanneldeleted)
    * [OnDiscordGuildChannelPinsUpdated](Hooks.md#ondiscordguildchannelpinsupdated)
    * [OnDiscordGuildRoleCreated](Hooks.md#ondiscordguildrolecreated)
    * [OnDiscordGuildRoleUpdated](Hooks.md#ondiscordguildroleupdated)
    * [OnDiscordGuildRoleDeleted](Hooks.md#ondiscordguildroledeleted)

* Guild Members (`GatewayIntents.GuildMembers`)
    * [OnDiscordGuildMemberAdded](Hooks.md#ondiscordguildmemberadded)
    * [OnDiscordGuildMemberUpdated](Hooks.md#ondiscordguildmemberupdated)
    * [OnDiscordGuildMemberRemoved](Hooks.md#ondiscordguildmemberremoved)
    
* Guild Bans (`GatewayIntents.GuildBans`)
    * [OnDiscordGuildMemberBanned](Hooks.md#ondiscordguildmemberbanned)
    * [OnDiscordGuildMemberUnbanned](Hooks.md#ondiscordguildmemberunbanned)
    
* Guild Emojis (`GatewayIntents.GuildEmojis`)
    * [OnDiscordGuildEmojisUpdated](Hooks.md#ondiscordguildemojisupdated)
    
* Guild Integrations (`GatewayIntents.GuildIntegrations`)
    * [OnDiscordGuildIntegrationsUpdated](Hooks.md#ondiscordguildintegrationsupdated)

* Guild Webhooks (`GatewayIntents.GuildWebhooks`)
    * [OnDiscordGuildWebhookUpdated](Hooks.md#ondiscordguildwebhooksupdated)

* Guild Invites (`GatewayIntents.GuildInvites`)
    * [OnDiscordGuildInviteCreated](Hooks.md#ondiscordguildinvitecreated)
    * [OnDiscordGuildInviteDeleted](Hooks.md#ondiscordguildinvitedeleted)
    
* Guild Voice State (`GatewayIntents.GuildVoiceStates`)
    * [OnDiscordGuildVoiceStateUpdated](Hooks.md#ondiscordguildvoicestateupdated)

* Guild Presences (`GatewayIntents.GuildPresences`)
    * [OnDiscordGuildMemberPresenceUpdated](Hooks.md#ondiscordguildmemberpresenceupdated)

* Guild Messages (`GatewayIntents.GuildMessages`)
  * [OnDiscordGuildMessageCreated](Hooks.md#ondiscordguildmessagecreated)
  * [OnDiscordGuildMessageUpdated](Hooks.md#ondiscordguildmessageupdated)
  * [OnDiscordGuildMessageDeleted](Hooks.md#ondiscordguildmessagedeleted)
  * [OnDiscordGuildMessagesBulkDeleted](Hooks.md#ondiscordguildmessagebulkdeleted)

* Guild Message Reactions (`GatewayIntents.GuildMessageReactions`)
  * [OnDiscordGuildMessageReactionAdded](Hooks.md#ondiscordguildmessagereactionadded)
  * [OnDiscordGuildMessageReactionRemoved](Hooks.md#ondiscordguildmessagereactionremoved)
  * [OnDiscordGuildMessageReactionRemovedAll](Hooks.md#ondiscordguildmessagereactionremovedall)
  * [OnDiscordGuildMessageReactionEmojiRemoved](Hooks.md#ondiscordguildmessagereactionemojiremoved)

* Guild Message Typing (`GatewayIntents.GuildMessageTyping`)
  * [OnDiscordGuildTypingStarted](Hooks.md#ondiscordguildtypingstarted)

* Direct Messages (`GatewayIntents.DirectMessages`)
  * [OnDiscordDirectMessageCreated](Hooks.md#ondiscorddirectmessagecreated)
  * [OnDiscordDirectMessageUpdated](Hooks.md#ondiscorddirectmessageupdated)
  * [OnDiscordDirectMessageDeleted](Hooks.md#ondiscorddirectmessagedeleted)
  * [OnDiscordDirectChannelPinsUpdated](Hooks.md#ondiscorddirectmessagechannelpinsupdated)

* Direct Message Reactions (`GatewayIntents.DirectMessageReactions`)
  * [OnDiscordDirectMessageReactionAdded](Hooks.md#ondiscorddirectmessagereactionadded)
  * [OnDiscordDirectMessageReactionRemoved](Hooks.md#ondiscorddirectmessagereactionremoved)
  * [OnDiscordDirectMessageReactionRemovedAll](Hooks.md#ondiscorddirectmessagereactionremovedall)
  * [OnDiscordDirectMessageReactionEmojiRemoved](Hooks.md#ondiscorddirectmessagereactionemojiremoved)

* Direct Message Typing (`GatewayIntents.DirectMessageTyping`)
  * [OnDiscordDirectTypingStarted](Hooks.md#ondiscorddirecttypingstarted)


## Building Intents
You can build your intents in the following manner

```c#
GatewayIntents intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessages
```