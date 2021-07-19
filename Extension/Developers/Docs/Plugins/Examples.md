# Discord Extension Plugin Examples

Here you will find some examples of plugins using the discord extension. Some of the examples will leave out parts that do not related to the discord extension in order to keep the examples short.

## TOC
- [Discord Extension Plugin Examples](#discord-extension-plugin-examples)
    * [Basic Connecting Example](#basic-connecting-example)
    * [Early Connecting Example](#early-connecting-example)
    * [Early Connecting Example Option 2](#early-connecting-example-option-2)
    * [Advanced Connecting Example](#advanced-connecting-example)
    * [Registering Plugin for Discord Link](#registering-plugin-for-discord-link)
    * [Discord Command Example](#discord-command-example)
    * [Advanced command example](#advanced-command-example)
    * [Discord Channel Subscriptions](#discord-channel-subscriptions)
    
## Basic Connecting Example

```c#
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;

namespace Oxide.Plugins
{
    [Info("Connect Example", "MJSU", "1.0.0")]
    [Description("Example on how to connect to the discord extension")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        #endregion

        private void OnServerInitialized()
        {
           _client.Connect("YourBotToken", GatewayIntents.Guilds | GatewayIntents.GuildMembers);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }
    }
}
```

## Early Connecting Example
This example is if you're wanting to connect to the discord before the OnServerInitialized hook is called
```c#
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;

namespace Oxide.Plugins
{
    [Info("Connect Example", "MJSU", "1.0.0")]
    [Description("Example on how to connect to the discord extension")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        #endregion

        private void OnDiscordClientCreated()
        {
           _client.Connect("YourBotToken", GatewayIntents.Guilds | GatewayIntents.GuildMembers);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }
    }
}
```

## Early Connecting Example Option 2
This example is if you're wanting to connect to the discord during Init or Loaded Hooks
```c#
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;

namespace Oxide.Plugins
{
    [Info("Connect Example", "MJSU", "1.0.0")]
    [Description("Example on how to connect to the discord extension")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        #endregion

        private void Init() 
        {
            DiscordClient.CreateClient(this);
            _client.Connect("YourBotToken", GatewayIntents.Guilds | GatewayIntents.GuildMembers);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }
    }
}
```

## Advanced Connecting Example

```c#
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Advanced Connect Example", "MJSU", "1.0.0")]
    [Description("Example on how to do an advanced connect to the discord extension")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;

        private readonly DiscordSettings _discordSettings = new DiscordSettings();
        #endregion

        private void OnServerInitialized()
        {
            _discordSettings.ApiToken = "YourBotToken";
            _discordSettings.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers;
            _discordSettings.LogLevel = LogLevel.Info;
            _discordSettings.CloseOnUnload = true;
            
           _client.Connect(_discordSettings);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }
    }
}
```

## Registering Plugin for Discord Link
```c#
using System;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Libraries.Linking;

namespace Oxide.Plugins
{
    [Info("Discord Link Example", "MJSU", "1.0.0")]
    [Description("Example on how to use Discord Link")]
    internal class ConnectExample : CovalencePlugin, IDiscordLinkPlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        
        private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();
        
        private Action<IPlayer, DiscordUser> _onLinked;
        private Action<IPlayer, DiscordUser> _onUnlinked;
        #endregion

        private void OnServerInitialized()
        {
            //Register your plugin as a discord link plugin
            _link.AddLinkPlugin(this);
            
            _client.Connect("YourBotToken", GatewayIntents.Guilds | GatewayIntents.GuildMembers);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }

        //Return your saved discord to server links.
        //Note this is only called once while registering plugin to link
        public Hash<string, Snowflake> GetSteamToDiscordIds()
        {
            return new Hash<string, Snowflake>
            {
                ["SavedSteamId"] = new Snowflake(12321321)
            };
        }

        public void RegisterEvents(Action<IPlayer, DiscordUser> onLinked, Action<IPlayer, DiscordUser> onUnlinked)
        {
            //Saves these actions which are to be called when a user links or unlinks.
            //Failure to call these will cause the DiscordLink data to become outdated.
            _onLinked = onLinked;
            _onUnlinked = onUnlinked;
        }
    }
}
```

## Discord Command Example
```c#
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Libraries.Command;

namespace Oxide.Plugins
{
    [Info("Command Example", "MJSU", "1.0.0")]
    [Description("Example on how to use discord commands")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();
        #endregion

        private void OnServerInitialized()
        {
            _client.Connect("YourBotToken", GatewayIntents.Guilds | GatewayIntents.GuildMembers);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }

        //By passing true instead of false it will use the lang key with the given string as the command
        [GuildCommand("myguildcommand", false)]
        private void MyGuildCommand(DiscordMessage message, string cmd, string[] args)
        {
            message.Reply(_client,"You used the guild command");
        }
        
        //By passing true instead of false it will use the lang key with the given string as the command
        [DirectMessageCommand("mydirectcommand", false)]
        private void MyDirectCommand(DiscordMessage message, string cmd, string[] args)
        {
            message.Reply(_client,"You used the direct message command command");
        }
    }
}
```

## Advanced command example

```c#
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Libraries.Command;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Advanced Command Example", "MJSU", "1.0.0")]
    [Description("Example on how to do advanced command setup")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient]
        private DiscordClient _client;

        private PluginConfig _pluginConfig;

        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();

        private readonly DiscordSettings _discordSettings = new DiscordSettings();
        
        #endregion

        private void OnServerInitialized()
        {
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMembers;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
            
            RegisterDiscordCommand(nameof(MyDiscordCommand), "myDiscordCommand", _pluginConfig.AllowInDm, _pluginConfig.AllowInGuild, _pluginConfig.AllowedChannels);
            RegisterDiscordLangCommand(nameof(MyDiscordCommand), "lang key", _pluginConfig.AllowInDm, _pluginConfig.AllowInGuild, _pluginConfig.AllowedChannels);
            
            _client.Connect(_discordSettings);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }

        private void MyDiscordCommand(DiscordMessage message, string cmd, string[] args)
        {
            message.Reply(_client, "You used my discord command");
        }

        //Registers a command by name
        public void RegisterDiscordCommand(string command, string name, bool direct, bool guild, List<Snowflake> allowedChannels)
        {
            if (direct)
            {
                _dcCommands.AddDirectMessageCommand(name, this, command);
            }

            if (guild)
            {
                _dcCommands.AddGuildCommand(name, this, allowedChannels, command);
            }
        }
        
        //Registers a command by lang key
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

        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }

            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Direct Messages")]
            public bool AllowInDm { get; set; }

            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Guild")]
            public bool AllowInGuild { get; set; }

            [JsonProperty(PropertyName = "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)")]
            public List<Snowflake> AllowedChannels { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }
    }
}
```

## Discord Channel Subscriptions
```c#
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Libraries.Subscription;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Channels Subscriptions Example", "MJSU", "1.0.0")]
    [Description("Example on how to subscribe to a channel")]
    internal class ConnectExample : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient]
        private DiscordClient _client;

        private PluginConfig _pluginConfig;

        private readonly DiscordSubscriptions _subscriptions = Interface.Oxide.GetLibrary<DiscordSubscriptions>();

        private readonly DiscordSettings _discordSettings = new DiscordSettings();
        
        #endregion

        private void OnServerInitialized()
        {
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMembers;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;

            _subscriptions.AddChannelSubscription(this, _pluginConfig.SubscribedChannelId, OnChannelMessage);
            _client.Connect(_discordSettings);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }
        
        private void OnChannelMessage(DiscordMessage message)
        {
            message.Reply(_client, "I am subscribed to listen to messages in this channel");
        }

        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Subscribed Channel ID")]
            public Snowflake SubscribedChannelId { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }
    }
}
```