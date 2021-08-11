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
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Roles;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries.Linking;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
    [Info("Discord Roles", "MJSU", "2.0.1")]
    [Description("Syncs players oxide group with discord roles")]
    class DiscordRoles : CovalencePlugin
    {
        #region Class Fields

        [PluginReference] private Plugin AntiSpamNames;
        [DiscordClient] private DiscordClient _client;
        
        private PluginConfig _pluginConfig; //Plugin Config

        private readonly List<PlayerSync> _processIds = new List<PlayerSync>();
        
        private Timer _playerChecker;
        
        private DiscordGuild _guild;
        private DiscordSettings _discordSettings;

        private const string AccentColor = "#de8732";
        
        private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();

        public enum DebugEnum
        {
            Message,
            None,
            Error,
            Warning,
            Info
        }

        public enum Source
        {
            Server,
            Discord
        }
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _discordSettings = new DiscordSettings
            {
                ApiToken = _pluginConfig.DiscordApiKey,
                LogLevel = _pluginConfig.ExtensionDebugging,
                Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
            };
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"[#BEBEBE][[{AccentColor}]{Title}[/#]] {{0}}[/#]",
                [LangKeys.ServerMessageGroupAdded] = "{player.name} has been added to oxide group {group.name}",
                [LangKeys.ServerMessageGroupRemoved] = "{player.name} has been removed to oxide group {group.name}",
                [LangKeys.ServerMessageRoleAdded] = "{player.name} has been added to discord role {role.name}",
                [LangKeys.ServerMessageRoleRemoved] = "{player.name} has been removed to discord role {role.name}",

                [LangKeys.DiscordMessageGroupAdded] = "{discord.name} has been added to oxide group {group.name}",
                [LangKeys.DiscordMessageGroupRemoved] = "{discord.name} has been removed to oxide group {group.name}",
                [LangKeys.DiscordMessageRoleAdded] = "{discord.name} has been added to discord role {role.name}",
                [LangKeys.DiscordMessageRoleRemoved] = "{discord.name} has been removed to discord role {role.name}",
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
            config.SyncData = config.SyncData ?? new List<SyncData>
            {
                new SyncData
                {
                    ServerGroup = "Default",
                    DiscordRole = default(Snowflake),
                    Source = Source.Server
                },
                new SyncData
                {
                    ServerGroup = "VIP",
                    DiscordRole = default(Snowflake),
                    Source = Source.Discord
                }
            };

            foreach (SyncData data in config.SyncData)
            {
                //Add new field to old data
                if (data.Notifications == null)
                {
                    data.Notifications = new NotificationSettings();
                }
            }

            return config;
        }

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please enter your bot token in the config and reload the plugin.");
                return;
            }
            
            if (_pluginConfig.UseAntiSpamNames && AntiSpamNames == null)
            {
                PrintWarning("AntiSpamNames is enabled in the config but is not loaded. " +
                             "Please disable the setting in the config or load AntiSpamNames: https://umod.org/plugins/anti-spam-names");
            }
            
            _client.Connect(_discordSettings);
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

        [HookMethod(DiscordHooks.OnDiscordGuildMembersLoaded)]
        private void OnDiscordGuildMembersLoaded(DiscordGuild guild)
        {
            if (guild.Id != _guild.Id)
            {
                return;
            }

            _guild = guild;

            HandleMembersLoaded();
            Puts("Discord Roles Ready");
        }

        private void HandleMembersLoaded()
        {
            foreach (SyncData data in _pluginConfig.SyncData.ToList())
            {
                bool remove = false;
                if (!permission.GroupExists(data.ServerGroup))
                {
                    PrintWarning($"Oxide group does not exist: '{data.ServerGroup}'. Please create the group or correct the name");
                    remove = true;
                }

                DiscordRole role = _guild.Roles[data.DiscordRole];
                if (role == null)
                {
                    PrintWarning($"Discord role ID does not exist: '{data.DiscordRole}'.\n" +
                                 "Please add the discord role or fix the role ID.");
                    remove = true;
                }

                if (remove)
                {
                    _pluginConfig.SyncData.Remove(data);
                    continue;
                }
            }

            timer.In(5f, CheckAllPlayers);
        }

        private void CheckAllPlayers()
        {
            Hash<string, Snowflake> links = _link.GetSteamToDiscordIds();

            if (links == null)
            {
                PrintWarning("No Discord Link plugin registered. Please add a Discord Link plugin and reload this plugin.");
                return;
            }
            
            foreach (KeyValuePair<string, Snowflake> link in links)
            {
                IPlayer player = players.FindPlayerById(link.Key);
                if (player == null)
                {
                    continue;
                }
                
                GuildMember member = _guild.Members[link.Value];
                if (member == null)
                {
                    continue;
                }
                
                _processIds.Add(new PlayerSync(player, member, false));
            }

            Debug(DebugEnum.Message, $"Starting sync for {_processIds.Count} linked players");

            if (_playerChecker == null)
            {
                _playerChecker = timer.Every(_pluginConfig.UpdateRate, ProcessNextStartupId);
            }
        }

        private void ProcessNextStartupId()
        {
            if (_processIds.Count == 0)
            {
                _playerChecker?.Destroy();
                _playerChecker = null;
                return;
            }

            PlayerSync id = _processIds[0];
            _processIds.RemoveAt(0);

            Debug(DebugEnum.Info, $"Start processing: Player Id: {id.Player.Name}({id.Player.Id}) Discord Id: {GetMemberDisplayName(id.Member)}({id.Member.User.Id}) Is Leaving: {id.IsLeaving}");

            ProcessUser(id);
        }
        #endregion

        #region Commands
        [Command("dcr.forcecheck")]
        private void HandleCommand(IPlayer player, string cmd, string[] args)
        {
            Debug(DebugEnum.Message, "Begin checking all players");
            CheckAllPlayers();
        }
        #endregion

        #region Hooks
        private void OnUserConnected(IPlayer player)
        {
            Debug(DebugEnum.Info, $"{nameof(OnUserConnected)} Added {player.Name}({player.Id}) to be processed");
            ProcessChange(player.Id, false);
        }

        private void OnUserGroupAdded(string id, string groupName)
        {
            Debug(DebugEnum.Info, $"{nameof(OnUserGroupAdded)} Added ({id}) to be processed because added to group {groupName}");
            ProcessChange(id, false);
        }

        private void OnUserGroupRemoved(string id, string groupName)
        {
            Debug(DebugEnum.Info, $"{nameof(OnUserGroupRemoved)} Added ({id}) to be processed because removed from group {groupName}");
            ProcessChange(id, false);
        }

        [HookMethod(DiscordHooks.OnDiscordPlayerLinked)]
        private void OnDiscordPlayerLinked(IPlayer player, DiscordUser user)
        {
            Debug(DebugEnum.Info, $"{nameof(OnDiscordPlayerLinked)} Added Player {player.Name}({player.Id}) Discord: {user.Username}#{user.Discriminator}({user.Id}) to be processed");
            ProcessChange(player.Id, false);
        }

        [HookMethod(DiscordHooks.OnDiscordPlayerUnlinked)]
        private void OnDiscordPlayerUnlinked(IPlayer player, DiscordUser user)
        {
            Debug(DebugEnum.Info, $"{nameof(OnDiscordPlayerUnlinked)} Added Player {player.Name}({player.Id}) Discord: {user.Username}#{user.Discriminator}({user.Id}) to be processed");
            ProcessChange(player.Id, true);
        }

        [HookMethod(DiscordHooks.OnDiscordGuildMemberAdded)]
        private void OnDiscordGuildMemberAdded(GuildMemberAddedEvent member)
        {
            if (member.GuildId != _guild.Id)
            {
                return;
            }
            
            Debug(DebugEnum.Info, $"{nameof(OnDiscordGuildMemberAdded)} Added {GetMemberDisplayName(member)}({member.User.Id}) to be processed");
            HandleDiscordChange(member.User.Id, false);
        }

        [HookMethod(DiscordHooks.OnDiscordGuildMemberRemoved)]
        private void OnDiscordGuildMemberRemoved(GuildMemberRemovedEvent member)
        {
            if (member.GuildId != _guild.Id)
            {
                return;
            }
            
            Debug(DebugEnum.Info, $"{nameof(OnDiscordGuildMemberRemoved)} Added {member.User.GetFullUserName}({member.User.Id}) to be processed");
            HandleDiscordChange(member.User.Id, true);
        }

        [HookMethod(DiscordHooks.OnDiscordGuildMemberUpdated)]
        private void OnDiscordGuildMemberUpdated(GuildMemberUpdatedEvent update, GuildMember oldMember, DiscordGuild guild)
        {
            if (guild.Id != _guild.Id)
            {
                return;
            }
            
            if (update.Roles.All(r => oldMember.Roles.Contains(r)) 
                && oldMember.Roles.All(r => update.Roles.Contains(r))
                && update.Nickname == oldMember.Nickname)
            {
                return;
            }

            Debug(DebugEnum.Info, $"{nameof(OnDiscordGuildMemberUpdated)} Added {update.Nickname}({update.User.Id}) to be processed");
            HandleDiscordChange(update.User.Id, false);
        }

        public void HandleDiscordChange(Snowflake userId, bool isLeaving)
        {
            string playerId = _link.GetSteamId(userId);
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }

            ProcessChange(playerId, isLeaving);
        }

        private void ProcessChange(string playerId, bool isLeaving)
        {
            _processIds.RemoveAll(p => p.Player.Id == playerId);

            IPlayer player = players.FindPlayerById(playerId);
            if (player == null)
            {
                return;
            }

            Snowflake discordId = player.GetDiscordUserId() ?? default(Snowflake);
            if (!discordId.IsValid())
            {
                return;
            }

            GuildMember member = _guild.Members[discordId];
            
            _processIds.Insert(0, new PlayerSync(player, member, isLeaving));

            if (_playerChecker == null)
            {
                _playerChecker = timer.Every(_pluginConfig.UpdateRate, ProcessNextStartupId);
            }
        }
        #endregion

        #region Role Handling
        public void ProcessUser(PlayerSync sync)
        {
            try
            {
                UnsubscribeAll();
                
                HandleServerGroups(sync);
                HandleDiscordRoles(sync);
                HandleUserNick(sync);
            }
            finally
            {
                SubscribeAll();
            }
        }

        public void HandleServerGroups(PlayerSync playerSync)
        {
            IPlayer player = playerSync.Player;
            GuildMember member = playerSync.Member;
            
            string playerName = $"{player.Name}({playerSync.Player.Id}) {GetMemberDisplayName(member)}({member.User.Id})";
            
            Debug(DebugEnum.Info, $"Processing Server for {player.Name}({player.Id}) Discord {GetMemberDisplayName(member)}({member.User.Id}) Is Leaving {playerSync.IsLeaving}");
            
            foreach (IGrouping<Snowflake, SyncData> data in _pluginConfig.SyncData.Where(s => s.Source == Source.Server).GroupBy(s => s.DiscordRole))
            {
                bool isInGroup = !playerSync.IsLeaving && data.Any(d => permission.UserHasGroup(player.Id, d.ServerGroup));
                bool isInDiscord = member.Roles.Contains(data.Key);
                if (isInDiscord == isInGroup)
                {
                    Debug(DebugEnum.Info, $"{playerSync.Player.Name} skipping Server Sync: [{string.Join(", ", data.Select(d => d.ServerGroup).ToArray())}] -> {_guild.Roles[data.Key]?.Name} {(isInGroup ? "Already Synced" : "Not in group")}");
                    continue;
                }

                string roleName = _guild.Roles[data.Key]?.Name;
                
                if (isInGroup)
                {
                    Debug(DebugEnum.Message, $"Adding player {playerName} to discord role {roleName}");
                    _guild.AddGuildMemberRole(_client, member.User.Id, data.Key, () =>
                    {
                        Debug(DebugEnum.Message, $"Successfully added {playerName} to {roleName}");
                    }, error =>
                    {
                        Debug(DebugEnum.Error, $"An error has occured adding {playerName} to {roleName}. Please check above this message for the error.");
                    });
                }
                else
                {
                    _guild.RemoveGuildMemberRole(_client, playerSync.Member.User.Id, data.Key, () =>
                    {
                        Debug(DebugEnum.Message, $"Successfully removed {playerName} from {roleName}");
                    }, error =>
                    {
                        Debug(DebugEnum.Error, $"An error has occured removing {playerName} from {roleName}. Please check above this message for the error.");
                    });
                }

                SyncData sync = data.FirstOrDefault(d => permission.UserHasGroup(player.Id, d.ServerGroup)) ?? data.FirstOrDefault();
                SendSyncNotification(playerSync, sync, isInGroup);
            }
        }

        public void HandleDiscordRoles(PlayerSync playerSync)
        {
            IPlayer player = playerSync.Player;
            GuildMember member = playerSync.Member;
            
            string playerName = $"{player.Name}({playerSync.Player.Id}) {GetMemberDisplayName(member)}({member.User.Id})";
            
            Debug(DebugEnum.Info, $"Processing Discord for {player.Name}({player.Id}) Discord {GetMemberDisplayName(member)}({member.User.Id}) Is Leaving {playerSync.IsLeaving}");
            
            foreach (IGrouping<string, SyncData> data in _pluginConfig.SyncData.Where(s => s.Source == Source.Discord).GroupBy(s => s.ServerGroup))
            {
                bool isInGroup = permission.UserHasGroup(player.Id, data.Key);
                bool isInDiscord = false;
                SyncData sync = null;
                if (!playerSync.IsLeaving)
                {
                    foreach (SyncData syncData in data)
                    {
                        if (member.Roles.Contains(syncData.DiscordRole))
                        {
                            sync = syncData;
                            isInDiscord = true;
                            break;
                        }
                    }
                }

                if (isInDiscord == isInGroup)
                {
                    Debug(DebugEnum.Info, $"{player?.Name} skipping Discord Sync: [{string.Join(", ", data.Select(d => _guild.Roles[d.DiscordRole]?.Name ?? string.Empty).ToArray())}] -> {data.Key} {(isInDiscord ? "Already Synced" : "Doesn't have role")}");
                    continue;
                }


                if (isInDiscord)
                {
                    Debug(DebugEnum.Message, $"Adding player {playerName} to server group {data.Key}");
                    permission.AddUserGroup(player.Id, data.Key);
                }
                else
                {
                    Debug(DebugEnum.Message, $"Removing player {playerName} from server group {data.Key}");
                    permission.RemoveUserGroup(player.Id, data.Key);
                }
                
                sync = sync ?? data.FirstOrDefault();
                SendSyncNotification(playerSync, sync, isInDiscord);
            }
        }

        public void HandleUserNick(PlayerSync sync)
        {
            IPlayer player = sync.Player;
            if (!_pluginConfig.SyncNicknames || sync.IsLeaving)
            {
                return;
            }

            if (sync.Member.User.Id == _guild.OwnerId)
            {
                return;
            }

            Debug(DebugEnum.Info, $"Updating {GetMemberDisplayName(sync.Member)}'s discord server nickname to {player.Name}");

            string playerName = GetPlayerName(player);
            
            if (playerName.Equals(sync.Member.Nickname))
            {
                return;
            }
            
            _guild.ModifyUsersNick(_client, sync.Member.User.Id, playerName, member =>
            {
                Debug(DebugEnum.Info, $"Successfully updated {GetMemberDisplayName(sync.Member)}'s discord server nickname to {player.Name}");
            }, error =>
            {
                Debug(DebugEnum.Error, $"An error has occured updating {GetMemberDisplayName(sync.Member)}'s discord server nickname to {player.Name}");
            });
        }
        
        private string GetPlayerName(IPlayer player)
        {
            string playerName = player.Name;
            if (_pluginConfig.UseAntiSpamNames && AntiSpamNames != null && AntiSpamNames.IsLoaded)
            {
                playerName = AntiSpamNames.Call<string>("GetClearName", player);
                if (string.IsNullOrEmpty(playerName))
                {
                    Debug(DebugEnum.Warning, $"AntiSpamNames returned an empty string for '{player.Name}'");
                    playerName = player.Name;
                }
                else if (!playerName.Equals(player.Name))
                {
                    Debug(DebugEnum.Info, $"Nickname '{player.Name}' was filtered by AntiSpamNames: '{playerName}'");
                }
            }
            
            return playerName;
        }
        #endregion

        #region Message Handling
        private void SendSyncNotification(PlayerSync sync, SyncData data, bool wasAdded)
        {
            NotificationSettings settings = data.Notifications;
            if (!settings.SendMessageToServer && !settings.SendMessageToDiscord)
            {
                return;
            }

            if (wasAdded && !settings.SendMessageOnAdd)
            {
                return;
            }

            if (!wasAdded && !settings.SendMessageOnRemove)
            {
                return;
            }

            if (settings.SendMessageToServer)
            {
                StringBuilder message = GetServerMessage(data, wasAdded);
                ProcessMessage(message, sync, data);
                Chat(message.ToString());
            }

            if (settings.SendMessageToDiscord)
            {
                if (!settings.DiscordMessageChannelId.IsValid())
                {
                    return;
                }

                StringBuilder message = GetDiscordMessage(data, wasAdded);
                ProcessMessage(message, sync, data);
                DiscordMessage.CreateMessage(_client, settings.DiscordMessageChannelId, message.ToString());
            }
        }

        private StringBuilder GetServerMessage(SyncData sync, bool wasAdded)
        {
            StringBuilder message = new StringBuilder();
            if (wasAdded && !string.IsNullOrEmpty(sync.Notifications.ServerMessageAddedOverride))
            {
                message.Append(sync.Notifications.ServerMessageAddedOverride);
            }
            else if (!wasAdded && !string.IsNullOrEmpty(sync.Notifications.ServerMessageRemovedOverride))
            {
                message.Append(sync.Notifications.ServerMessageRemovedOverride);
            }
            else
            {
                switch (sync.Source)
                {
                    case Source.Server:
                        message.Append(wasAdded ? LangNoFormat(LangKeys.ServerMessageRoleAdded) : LangNoFormat(LangKeys.ServerMessageRoleRemoved));
                        break;

                    case Source.Discord:
                        message.Append(wasAdded ? LangNoFormat(LangKeys.ServerMessageGroupAdded) : LangNoFormat(LangKeys.ServerMessageGroupRemoved));
                        break;
                }
            }

            return message;
        }

        private StringBuilder GetDiscordMessage(SyncData sync, bool wasAdded)
        {
            StringBuilder message = new StringBuilder();
            if (wasAdded && !string.IsNullOrEmpty(sync.Notifications.DiscordMessageAddedOverride))
            {
                message.Append(sync.Notifications.DiscordMessageAddedOverride);
            }
            else if (!wasAdded && !string.IsNullOrEmpty(sync.Notifications.DiscordMessageRemovedOverride))
            {
                message.Append(sync.Notifications.DiscordMessageRemovedOverride);
            }
            else
            {
                switch (sync.Source)
                {
                    case Source.Server:
                        message.Append(wasAdded ? LangNoFormat(LangKeys.DiscordMessageRoleAdded) : LangNoFormat(LangKeys.DiscordMessageRoleRemoved));
                        break;

                    case Source.Discord:
                        message.Append(wasAdded ? LangNoFormat(LangKeys.DiscordMessageGroupAdded) : LangNoFormat(LangKeys.DiscordMessageGroupRemoved));
                        break;
                }
            }

            return message;
        }

        private void ProcessMessage(StringBuilder message, PlayerSync sync, SyncData data)
        {
            IPlayer player = sync.Player;
            GuildMember member = sync.Member;

            if (player != null)
            {
                message.Replace("{player.id}", player.Id);
                message.Replace("{player.name}", player.Name);
            }

            if (member != null)
            {
                message.Replace("{discord.id}", member.User.Id.ToString());
                message.Replace("{discord.name}", member.User.Username);
                message.Replace("{discord.discriminator}", member.User.Discriminator);
                message.Replace("{discord.nickname}", member.Nickname);
            }

            DiscordRole role = _guild.Roles[data.DiscordRole];
            if (role != null)
            {
                message.Replace("{role.id}", role.Id.ToString());
                message.Replace("{role.name}", role.Name);
            }

            message.Replace("{group.name}", data.ServerGroup);
        }
        #endregion

        #region Subscription Handling
        public void UnsubscribeAll()
        {
            Unsubscribe(nameof(OnUserGroupAdded));
            Unsubscribe(nameof(OnUserGroupRemoved));
        }

        public void SubscribeAll()
        {
            Subscribe(nameof(OnUserGroupAdded));
            Subscribe(nameof(OnUserGroupRemoved));
        }
        #endregion

        #region Helper Methods
        public string GetMemberDisplayName(GuildMember member)
        {
            return string.IsNullOrEmpty(member.Nickname) ? $"{member.User.Username}#{member.User.Discriminator}" : member.Nickname;
        }

        public void Debug(DebugEnum level, string message)
        {
            if (level <= _pluginConfig.DebugLevel)
            {
                Puts($"{level}: {message}");
            }
        }

        public void Chat(string message)
        {
            server.Broadcast(Lang(LangKeys.Chat, null, message));
        }

        public string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception:\n{ex}");
                throw;
            }
        }
        
        public string LangNoFormat(string key, IPlayer player = null) => lang.GetMessage(key, this, player?.Id);
        #endregion

        #region Classes
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Discord Server ID")]
            public Snowflake GuildId { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Sync Nicknames")]
            public bool SyncNicknames { get; set; }

            [DefaultValue(2f)]
            [JsonProperty(PropertyName = "Update Rate (Seconds)")]
            public float UpdateRate { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Use AntiSpamNames On Discord Nickname")]
            public bool UseAntiSpamNames { get; set; }

            [JsonProperty(PropertyName = "Sync Data")]
            public List<SyncData> SyncData { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DebugEnum.Warning)]
            [JsonProperty(PropertyName = "Plugin Log Level (None, Error, Warning, Info)")]
            public DebugEnum DebugLevel { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }

        public class SyncData
        {
            [JsonProperty(PropertyName = "Server Group")]
            public string ServerGroup { get; set; }

            [JsonProperty(PropertyName = "Discord Role ID")]
            public Snowflake DiscordRole { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Source (Server or Discord)")]
            public Source Source { get; set; }

            [JsonProperty(PropertyName = "Sync Notification Settings")]
            public NotificationSettings Notifications { get; set; }
        }

        public class NotificationSettings
        {
            [JsonProperty(PropertyName = "Send message to Server")]
            public bool SendMessageToServer { get; set; }

            [JsonProperty(PropertyName = "Send Message To Discord")]
            public bool SendMessageToDiscord { get; set; }

            [JsonProperty(PropertyName = "Discord Message Channel (Name or ID)")]
            public Snowflake DiscordMessageChannelId { get; set; }

            [JsonProperty(PropertyName = "Send Message When Added")]
            public bool SendMessageOnAdd { get; set; }

            [JsonProperty(PropertyName = "Send Message When Removed")]
            public bool SendMessageOnRemove { get; set; }

            [JsonProperty(PropertyName = "Server Message Added Override Message")]
            public string ServerMessageAddedOverride { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "Server Message Removed Override Message")]
            public string ServerMessageRemovedOverride { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "Discord Message Added Override Message")]
            public string DiscordMessageAddedOverride { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "Discord Message Removed Override Message")]
            public string DiscordMessageRemovedOverride { get; set; } = string.Empty;
        }

        public class PlayerSync
        {
            public IPlayer Player { get; set; }
            public GuildMember Member { get; set; }
            public bool IsLeaving { get; set; }

            public PlayerSync(IPlayer player, GuildMember member, bool isLeaving)
            {
                Player = player;
                Member = member;
                IsLeaving = isLeaving;
            }
        }

        public class LangKeys
        {
            public const string Chat = nameof(Chat);

            public const string ServerMessageGroupAdded = nameof(ServerMessageGroupAdded);
            public const string ServerMessageGroupRemoved = nameof(ServerMessageGroupRemoved);
            public const string ServerMessageRoleAdded = nameof(ServerMessageRoleAdded);
            public const string ServerMessageRoleRemoved = nameof(ServerMessageRoleRemoved);

            public const string DiscordMessageGroupAdded = nameof(DiscordMessageGroupAdded);
            public const string DiscordMessageGroupRemoved = nameof(DiscordMessageGroupRemoved);
            public const string DiscordMessageRoleAdded = nameof(DiscordMessageRoleAdded);
            public const string DiscordMessageRoleRemoved = nameof(DiscordMessageRoleRemoved);
        }
        #endregion
    }
}