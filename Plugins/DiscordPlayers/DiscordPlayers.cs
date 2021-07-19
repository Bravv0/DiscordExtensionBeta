using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries.Command;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Discord Players", "MJSU", "2.0.0")]
    [Description("Displays online players in discord")]
    internal class DiscordPlayers : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        
        [PluginReference] private Plugin Clans;

        private PluginConfig _pluginConfig; //Plugin Config
        
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();

        private readonly DiscordSettings _discordSettings = new DiscordSettings();

        private const string UsePermission = "discordplayers.use";
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;

            if (_pluginConfig.AllowInDm)
            {
                _discordSettings.Intents |= GatewayIntents.DirectMessages;
            }
            
            if (_pluginConfig.AllowInGuild)
            {
                _discordSettings.Intents |= GatewayIntents.GuildMessages;
            }
            
            permission.RegisterPermission(UsePermission, this);
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.NoPermission] = "You do not have permission to use this command",
                [LangKeys.DiscordFormat] = $"[{Title}] {{0}}",
                [LangKeys.ListFormat] = "{0} Online: \n{1}",
                [LangKeys.PlayerFormat] = "{clan}{name} ({steamid})",
                [LangKeys.PlayersCommand] = "players",
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.AllowedChannels = config.AllowedChannels ?? new List<Snowflake>();
            config.AllowedRoles = config.AllowedRoles ?? new List<Snowflake>();
            return config;
        }

        private void OnServerInitialized()
        {
            RegisterDiscordLangCommand(nameof(DiscordPlayersMessageCommand), LangKeys.PlayersCommand, _pluginConfig.AllowInDm, _pluginConfig.AllowInGuild, _pluginConfig.AllowedChannels);
            
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }
            
            _client.Connect(_discordSettings);
        }

        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts("Discord Players Ready");
        }
        #endregion

        #region Discord Chat Command
        private void DiscordPlayersMessageCommand(DiscordMessage message, string cmd, string[] args)
        {
            if (_pluginConfig.RequiredLink)
            {
                IPlayer player = message.Author.Player;
                if (!message.GuildId.HasValue && player == null || !player.HasPermission(UsePermission))
                {
                    message.Reply(_client, Lang(LangKeys.NoPermission, message.Author.Player));
                    return;
                }
            
                if (message.GuildId.HasValue && !player.HasPermission(UsePermission) && (message.Member == null || message.Member.Roles.All(r => !_pluginConfig.AllowedRoles.Contains(r))))
                {
                    message.Reply(_client, Lang(LangKeys.NoPermission, message.Author.Player));
                    return;
                }
            }

            foreach (string list in GetPlayers())
            {
                message.Reply(_client, list);
            }
        }
        #endregion

        #region Helper Methods
        private List<string> GetPlayers()
        {
            StringBuilder sb = new StringBuilder();
            List<string> playerLists = new List<string>();
            int index = 0;
            foreach (IPlayer connectPlayer in players.Connected)
            {
                index++;
                string name = GetDisplayName(connectPlayer);
                if (sb.Length + name.Length + (_pluginConfig.Separator.Length * index) >= 1950)
                {
                    playerLists.Add(sb.ToString());
                    sb.Length = 0;
                }

                sb.Append(name);
                sb.Append(_pluginConfig.Separator);
            }
            
            playerLists.Add(sb.ToString());

            return playerLists;
        }

        private string GetDisplayName(IPlayer player)
        {
            string clanTag = Clans?.Call<string>("GetClanOf", player);
            if (!string.IsNullOrEmpty(clanTag))
            {
                clanTag = $"[{clanTag}] ";
            }

            return lang.GetMessage(LangKeys.PlayerFormat, this, player.Id)
                .Replace("{clan}", clanTag)
                .Replace("{name}", player.Name)
                .Replace("{steamid}", player.Id);
        }

        public string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch(Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }

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
        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Direct Messages")]
            public bool AllowInDm { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Require User To Be Linked To Use Command")]
            public bool RequiredLink { get; set; }

            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Guild")]
            public bool AllowInGuild { get; set; }
            
            [JsonProperty(PropertyName = "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)")]
            public List<Snowflake> AllowedChannels { get; set; }
            
            [JsonProperty(PropertyName = "Allow Guild Commands for members having role (Role ID)")]
            public List<Snowflake> AllowedRoles { get; set; }

            [DefaultValue("\n")]
            [JsonProperty(PropertyName = "Player name separator")]
            public string Separator { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }


        private static class LangKeys
        {
            public const string NoPermission = nameof(NoPermission);
            public const string DiscordFormat = nameof(DiscordFormat);
            public const string ListFormat = nameof(ListFormat);
            public const string PlayerFormat = nameof(PlayerFormat);
            public const string PlayersCommand = nameof(PlayersCommand);
        }

        #endregion
    }
}
