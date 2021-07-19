﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Roles;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries.Linking;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Discord Group", "MJSU", "2.0.0")]
    [Description("Grants players rewards for linking their game and discord accounts")]
    internal class DiscordGroup : CovalencePlugin
    {
        #region Class Fields
        [DiscordClient]
        private DiscordClient _client;

        private PluginConfig _pluginConfig;
        private StoredData _storedData;
        private DiscordRole _role;
        private DiscordGuild _guild;
        
        private readonly DiscordSettings  _discordSettings = new DiscordSettings
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
        };
        
        private readonly List<string> _processQueue = new List<string>();
        
        private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();
        #endregion

        #region Setup & Loading

        private void Init()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
        }
        
        private void OnServerInitialized()
        {
            _client.Connect(_discordSettings);
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
            config.Commands = config.Commands ?? new List<string>
            {
                "inventory.giveto {steamid} wood 100",
            };
            return config;
        }
        
        private void OnNewSave(string filename)
        {
            if (_pluginConfig.ResetRewardsOnWipe)
            {
                _storedData = new StoredData();
                SaveData();
            }
        }

        private void Unload()
        {
            SaveData();
        }
        #endregion

        #region Discord Hooks
        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
                return;
            }

            _guild = null;
            if (ready.Guilds.Count == 1 && !_pluginConfig.GuildId.IsValid())
            {
                _guild = ready.Guilds.Values.FirstOrDefault();
            }

            if (_guild == null)
            {
                _guild = ready.Guilds[_pluginConfig.GuildId];
            }

            if (_guild == null)
            {
                PrintError("Failed to find a matching guild for the Discord Server Id. " +
                           "Please make sure your guild Id is correct and the bot is in the discord server.");
            }
        }
        
        [HookMethod(DiscordHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild guild)
        {
            if (guild.Id != _guild.Id)
            {
                return;
            }
            
            if (_pluginConfig.DiscordRole.IsValid())
            {
                _role = _guild.Roles[_pluginConfig.DiscordRole];
                if (_role == null)
                {
                    PrintWarning($"Discord Role '{_pluginConfig.DiscordRole}' does not exist. Please set the role name or id in the config.");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(_pluginConfig.OxideGroup) && !permission.GroupExists(_pluginConfig.OxideGroup))
            {
                PrintWarning($"Oxide group '{_pluginConfig.OxideGroup}' does not exist. Please add the oxide group or set the correct group in the config.");
                return;
            }
            
            foreach (string userId in _link.GetSteamIds())
            {
                _processQueue.Add(userId);
            }

            timer.In(1f, ProcessNext);
            
            Puts("Discord Group Ready");
        }
        
        private void OnUserConnected(IPlayer player)
        {
            if (player.IsLinked())
            {
                HandlePlayerLinked(player, player.GetDiscordUser());
            }
        }

        [HookMethod(DiscordHooks.OnDiscordPlayerLinked)]
        private void OnDiscordPlayerLinked(IPlayer player, DiscordUser user)
        {
            HandlePlayerLinked(player, user);
        }

        [HookMethod(DiscordHooks.OnDiscordPlayerUnlinked)]
        private void OnDiscordPlayerUnlinked(IPlayer player, DiscordUser user)
        {
            HandlePlayerUnlinked(player, user);
        }
        #endregion

        #region Helpers
        public void ProcessNext()
        {
            if (_processQueue.Count == 0)
            {
                return;
            }

            string userId = _processQueue[0];
            _processQueue.RemoveAt(0);

            IPlayer player = players.FindPlayerById(userId);
            if (player != null)
            {
                HandlePlayerLinked(player, player.GetDiscordUser());
            }

            timer.In(1f, ProcessNext);
        }
        
        private void HandlePlayerLinked(IPlayer player, DiscordUser user)
        {
            if (!string.IsNullOrEmpty(_pluginConfig.OxideGroup))
            {
                AddToOxideGroup(player);
            }

            if (_role != null)
            {
                AddToDiscordRole(player, user);
            }

            if (_pluginConfig.RunCommands && player.IsConnected)
            {
                RunCommands(player);
            }
        }

        private void AddToOxideGroup(IPlayer player)
        {
            if (!permission.UserHasGroup(player.Id, _pluginConfig.OxideGroup))
            {
                Puts($"Adding player {player.Name}({player.Id}) to oxide group {_pluginConfig.OxideGroup}");
                permission.AddUserGroup(player.Id, _pluginConfig.OxideGroup);
            }
        }

        private void AddToDiscordRole(IPlayer player, DiscordUser user)
        {
            if (user == null)
            {
                return;
            }
            
            _guild.GetGuildMember(_client, user.Id, member =>
            {
                if (!member.Roles.Contains(_role.Id))
                {
                    _guild.AddGuildMemberRole(_client, user.Id, _role.Id);
                    Puts($"Adding player {player.Name}({player.Id}) to discord role {_role.Name}");
                }
            });
        }

        private void HandlePlayerUnlinked(IPlayer player, DiscordUser user)
        {
            if (!string.IsNullOrEmpty(_pluginConfig.OxideGroup))
            {
                RemoveFromOxide(player);
            }

            if (_role != null)
            {
                RemoveFromDiscord(player, user);
            }
        }

        private void RemoveFromOxide(IPlayer player)
        {
            Puts($"Removing player {player.Name}({player.Id}) from oxide group {_pluginConfig.OxideGroup}");
            permission.RemoveUserGroup(player.Id, _pluginConfig.OxideGroup);
        }

        private void RemoveFromDiscord(IPlayer player, DiscordUser user)
        {
            if (user == null)
            {
                return;
            }

            _guild.RemoveGuildMemberRole(_client, user.Id, _role.Id);

            Puts($"Removing player {player.Name}({player.Id}) from discord role {_role.Name}");
        }

        private void RunCommands(IPlayer player)
        {
            if (_storedData.RewardedPlayers.Contains(player.Id))
            {
                return;
            }

            foreach (string command in _pluginConfig.Commands)
            {
                string execCommand = command.Replace("{steamId}", player.Id)
                    .Replace("{name}", player.Name);
                
                server.Command(execCommand);
            }

            _storedData.RewardedPlayers.Add(player.Id);
            NextTick(SaveData);
        }
        
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Discord Server ID")]
            public Snowflake GuildId { get; set; }
            
            [JsonProperty("Add To Discord Role (Role ID)")]
            public Snowflake DiscordRole { get; set; }
            
            [DefaultValue("")]
            [JsonProperty("Add To Server Group")]
            public string OxideGroup { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty("Run Commands On Link")]
            public bool RunCommands { get; set; }
            
            [JsonProperty("Commands To Run")]
            public List<string> Commands { get; set; }

            [DefaultValue(false)]
            [JsonProperty("Reset Rewards On Wipe")]
            public bool ResetRewardsOnWipe { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }

        private class StoredData
        {
            public HashSet<string> RewardedPlayers = new HashSet<string>();
        }
        #endregion
    }
}
