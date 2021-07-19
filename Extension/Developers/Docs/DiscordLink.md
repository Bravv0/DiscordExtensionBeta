# Discord Link

Discord Link is a library provided by the Discord Extension.
This library allows plugin to access a standardized API to get information about a players who have linked their steam and discord accounts.
In order for Discord Link to work a plugin must be registered with Discord Link as the link plugin.
When the Discord Link plugin is unloaded then DiscordLink will remove that plugin as the link plugin.

## Getting Started

To gain access to the Discord Link API you add the following line of code in your plugin.
```c#
private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();
```

## Registering Link Plugin

To register your plugin you first need to inherit the `IDiscordLinkPlugin` interface on your plugin.
The interface can be seen below and you must implement those methods on your plugin.
Whenever a player links or unlinks their account you're to call the action callbacks given to your plugin in the `RegisterEvents` method

```c#
public interface IDiscordLinkPlugin
{
    Hash<string, Snowflake> GetSteamToDiscordIds();
    void RegisterEvents(Action<IPlayer, DiscordUser> onLinked, Action<IPlayer, DiscordUser> onUnlinked);
}
```

Once your plugin has inherited from this interface you need to register your plugin in the following way

```c#
private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();

private void Init()
{
    if (//Linking Is Enabled)
    {
        _link.AddLinkPlugin(this);
    }
}
```

The plugin will automatically be removed as the link plugin when unloaded

## Accessing Information

Once you have a reference to discord link you gain access to all the API's that are on it.
Before accessing information from DiscordLink it is recommended to call IsEnabled() to make sure a link plugin has been registered for the server.

```c#
// Returns if Discord Link has a registered link plugin
bool IsEnabled()

// Registeres the passed in plugin as the link plugin for the server
// Only 1 link plugin can be registered at a time
// If your plugin provides more than just Link functionality it is recommended to have a configuartion option to enable linking for your pluguin
void AddLinkPlugin(Plugin plugin)

// Returns if the given ID is linked.
// Valid types are String SteamId or Snowflake DisordId
bool IsLinked(object id)

// Returns true if the steam Id is linked with a discord account
bool IsLinked(string steamId)

// Returns true if the discord Id is linked with a steam account
bool IsLinked(Snowflake discordId)

// Returns a Steam ID for a given Discord ID
// If the Discord ID is not linked then the result will be null
string GetSteamId(Snowflake discordId)

// Returns an IPlayer for the given Discord ID
// If the Discord ID is not linked then the result will be null
IPlayer GetPlayer(Snowflake discordId)

// Returns a Discord ID for a given steam ID
// If the Steam ID is not linked then the result will be null
Snowflake? GetDiscordId(string steamId)

// Returns a Discord ID for a given player
// If the player is not linked then the result will be null
Snowflake? GetDiscordId(IPlayer player)

// Returns a DiscordUser for the given Steam ID.
// Note: This use will just contain the ID field and can be used in API calls.
DiscordUser GetDiscordUser(string steamId)

// Returns a DiscordUser for the given player.
// Note: This use will just contain the ID field and can be used in API calls.
DiscordUser GetDiscordUser(IPlayer player)

// Returns a guild member for a given Steam ID 
// If the Steam ID is not link then the result will be null
GuildMember GetLinkedMember(string steamId, Guild guild)

// Returns a guild member for a given player
// If the Steam ID is not link then the result will be null
GuildMember GetLinkedMember(IPlayer player, Guild guild)

// Returns the number of linked accounts
int GetLinkedCount()

// Returns a list of all Steam Ids that are linked
List<string> GetLinkedSteamIds()

// Returns a list of all snowflakes that are linked
List<Snowflake> GetLinkedDiscordIds()

// Returns a Hash of key SteamId and value Discord ID
Hash<string, Snowflake> GetSteamToDiscordIds()

// Returns a Hash of key Discord ID and value Steam ID
Hash<Snowflake, string> GetDiscordToSteamIds()
```

## Extension Methods

Using Discord Link also provides a bunch of extension methods that can be used.
These methods are available on both IPLayer and DiscordUser objects.

### IPlayer Methods

There are extension methods that can be called from an IPlayer object in the following way.

```c#
void MyMethod(IPlayer player) 
{
    player.SendDiscordMessage(_client, "Hello Discord User! How do you do?");
}
```

#### Available IPlayer Extension Methods

```c#
// Sends a DM message to a discord user in a from the bot connected to the client 
void SendDiscordMessage(DiscordClient client, string message)
void SendDiscordMessage(DiscordClient client, Embed embed)
void SendDiscordMessage(DiscordClient client, List<Embed> embeds)
void SendDiscordMessage(this IPlayer player, DiscordClient client, MessageCreate message

// Returns true if the player is linked
bool IsLinked()

// Returns the Discord ID for the player.
// Value will be null if not linked
Snowflake? GetDiscordUserId()

// Returns a Discord User for the player
// The value will be null if not linked
// This is a minimal player with just the ID field set
DiscordUser GetDiscordUser()

// Returns the Guild Member of the linked player for the given guild.
// If the player is not linked or found in the guild null will be returned
GuildMember GetGuildMember(Guild guild)
```

### DiscordUser Methods

There are extension methods that can be called from an DiscordUser object in the following way.

```c#
void MyMethod(DiscordUser user) 
{
    user.SendChatMessage("Hello");
}
```

#### Available DiscordUser Extension Methods

```c#
//DiscordUser has a field called Player which returns the IPlayer for the linked Discord User.
Player

// Sends a chat message to the player on the server if they're connected
void SendChatMessage(string message)
void SendChatMessage(string message, string prefix, params object[] args)

// Returns true if the DiscordUser is linked
bool IsLinked()

// Returns true if the DiscordUser is linked and that linked player has permission
bool HasPermission(string permission)
```