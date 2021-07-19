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
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.AllowedMentions;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Libraries.Subscription;
using Oxide.Ext.Discord.Logging;
using UnityEngine;
#if RUST
using ConVar;
#endif

namespace Oxide.Plugins
{
    [Info("Discord Chat", "MJSU", "2.0.0")]
    [Description("Allows chatting through discord")]
    internal class DiscordChat : CovalencePlugin
    {
        #region Class Fields
        [PluginReference] private Plugin AdminChat, AdminDeepCover, AntiSpamNames, BetterChat, BetterChatMute, Clans, ChatTranslator, TranslationAPI;

        private PluginConfig _pluginConfig;
        
        private DiscordTimedSend _chatSend;
        private DiscordTimedSend _joinLeaveSend;
        private DiscordTimedSend _adminChatSend;

#if RUST
        private DiscordTimedSend _teamSend;
#endif

        [DiscordClient] private DiscordClient _client;
        private DiscordGuild _guild;
        private Snowflake _guildId;
        
        private readonly DiscordSettings _discordSettings = new DiscordSettings
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.Guilds
        };

        private readonly AllowedMention _allowedMention = new AllowedMention
        {
            AllowedTypes = new List<AllowedMentionTypes>(),
            Roles = new List<Snowflake>(),
            Users = new List<Snowflake>(),
            RepliedUser = true
        };

        private readonly DiscordSubscriptions _subscriptions = Interface.Oxide.GetLibrary<DiscordSubscriptions>();

        private static DiscordChat _ins;
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _ins = this;
            
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Discord.JoinLeave.ConnectedMessage] = "({0:HH:mm}) {1} has joined.",
                [LangKeys.Discord.JoinLeave.DisconnectedMessage] = "({0:HH:mm}) {1} has disconnected. Reason: {2}",
                [LangKeys.Discord.ChatChannel.ChatMessage] = "({0:HH:mm}) {1}: {2}",
                [LangKeys.Discord.ChatChannel.BetterChatMessage] = "({0:HH:mm}) {1}",
                [LangKeys.Discord.ChatChannel.UnlinkedMessage] = "({0:HH:mm}) {1}#{2}: {3}",
                [LangKeys.Discord.ChatChannel.NotLinked] = "You're not allowed to chat with the server unless you are linked.",
                [LangKeys.Discord.TeamChannel.TeamChatMessage] = "({0:HH:mm}) {1}: {2}",
                [LangKeys.Discord.TeamChannel.BetterChatTeamMessage] = "({0:HH:mm}) {1}",
                [LangKeys.Discord.AdminChat.ChannelMessage] = "({0:HH:mm}) {1} {2}",
                [LangKeys.Discord.AdminChat.NotLinked] = "You're not allowed to use Admin Chat Channel unless you are linked.",
                [LangKeys.InGame.DiscordTag] = "[#5f79d6][Discord][/#]",
                [LangKeys.InGame.UnlinkedChat] = "{0} [#5f79d6]{1}#{2}[/#]: {3}",
                [LangKeys.InGame.InGameMessage] = "{0} [#5f79d6]{1}[/#]: {2}",
                [LangKeys.InGame.ClanTag] = "[{0}] ",
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.PluginSupport.ChatTranslator.DiscordServerLanguage = config.PluginSupport.ChatTranslator.DiscordServerLanguage ?? lang.GetServerLanguage();
            return config;
        }

        private void OnServerInitialized()
        {
            if (_pluginConfig.EnableServerChatTag)
            {
                BetterChat?.Call("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(GetDiscordTag));
            }

            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }
            
            if (_pluginConfig.PluginSupport.AntiSpamNames.ValidateNicknames
                || _pluginConfig.PluginSupport.AntiSpamNames.ChatMessage
                #if RUST
                || _pluginConfig.PluginSupport.AntiSpamNames.TeamMessage
                #endif
            )
            {
                if (AntiSpamNames == null)
                {
                    PrintWarning("AntiSpamNames is enabled in the config but is not loaded. " +
                                 "Please disable the setting in the config or load AntiSpamNames: https://umod.org/plugins/anti-spam-names");
                    return;
                }

                if (AntiSpamNames.Version < new VersionNumber(1, 3, 0))
                {
                    PrintError("AntiSpamNames plugin must be version 1.3.0 or higher");
                }
            }

            _client.Connect(_discordSettings);
        }

        private void Unload()
        {
            _ins = null;
        }
        #endregion

        #region Discord Setup
        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
                return;
            }

            DiscordGuild guild = null;
            if (ready.Guilds.Count == 1 && !_pluginConfig.GuildId.IsValid())
            {
                guild = ready.Guilds.Values.FirstOrDefault();
            }

            if (guild == null)
            {
                guild = ready.Guilds[_pluginConfig.GuildId];
            }

            if (guild == null)
            {
                PrintError("Failed to find a matching guild for the Discord Server Id. " +
                           "Please make sure your guild Id is correct and the bot is in the discord server.");
                return;
            }
            
            _guildId = guild.Id;
            Puts("Discord Chat Ready");
        }
        
        [HookMethod(DiscordHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild guild)
        {
            if (guild.Id != _guildId)
            {
                return;
            }

            _guild = guild;
            if (_pluginConfig.ChannelSettings.ChatChannel.IsValid())
            {
                DiscordChannel channel = _guild.Channels[_pluginConfig.ChannelSettings.ChatChannel];
                if (channel == null)
                {
                    PrintWarning($"Chat Channel '{_pluginConfig.ChannelSettings.ChatChannel}' not found in guild");
                    return;
                }

                _subscriptions.AddChannelSubscription(this, _pluginConfig.ChannelSettings.ChatChannel, HandleDiscordChatChannelMessage);
                if (_pluginConfig.MessageSettings.UseBotMessageDisplay)
                {
                    channel.GetChannelMessages(_client, null, messages => OnGetChannelMessages(messages, channel));
                }

                _chatSend = new DiscordTimedSend(channel.Id);
            }

#if RUST
            if (_pluginConfig.ChannelSettings.TeamChannel.IsValid())
            {
                DiscordChannel channel = _guild.Channels[_pluginConfig.ChannelSettings.TeamChannel];
                if (channel == null)
                {
                    PrintWarning($"Team Chat Channel '{_pluginConfig.ChannelSettings.TeamChannel}' not found in guild");
                    return;
                }

                _teamSend = new DiscordTimedSend(channel.Id);
            }
#endif

            if (_pluginConfig.ChannelSettings.JoinLeaveChannel.IsValid())
            {
                DiscordChannel channel = _guild.Channels[_pluginConfig.ChannelSettings.JoinLeaveChannel];
                if (channel == null)
                {
                    PrintWarning($"Join Leave Channel '{_pluginConfig.ChannelSettings.JoinLeaveChannel}' not found in guild");
                    return;
                }

                _joinLeaveSend = new DiscordTimedSend(channel.Id);
            }

            Snowflake adminChannel = _pluginConfig.PluginSupport.AdminChat.ChatChannel;
            if (adminChannel.IsValid())
            {
                DiscordChannel channel = _guild.Channels[adminChannel];
                if (channel == null)
                {
                    PrintWarning($"Admin Chat Channel '{adminChannel}' not found in guild");
                    return;
                }

                _subscriptions.AddChannelSubscription(this, adminChannel, HandleAdminChatMessage);
                if (_pluginConfig.MessageSettings.UseBotMessageDisplay)
                {
                    channel.GetChannelMessages(_client, null, messages => OnGetChannelMessages(messages, channel));
                }

                _adminChatSend = new DiscordTimedSend(channel.Id);
            }
        }
        
        private void OnGetChannelMessages(List<DiscordMessage> messages, DiscordChannel channel)
        {
            if (messages.Count == 0)
            {
                return;
            }

            DiscordMessage[] messagesToDelete = messages
                .Where(m => !ShouldIgnoreUser(m.Author))
                .ToArray();

            if (messagesToDelete.Length == 0)
            {
                return;
            }

            if (messagesToDelete.Length == 1)
            {
                messagesToDelete[0]?.DeleteMessage(_client);
                return;
            }

            channel.BulkDeleteMessages(_client, messagesToDelete.Select(m => m.Id).ToArray());
        }
        #endregion

        #region Oxide Hook
#if RUST
        private void OnPlayerChat(BasePlayer rustPlayer, string message, Chat.ChatChannel chatChannel)
        {
            IPlayer player = rustPlayer.IPlayer;
            int channel = (int) chatChannel;

#else
        private void OnUserChat(IPlayer player, string message)
        {
            int channel = 0;
#endif
            if (_chatSend == null)
            {
                return;
            }

            if (BetterChatMute?.Call<bool>("API_IsMuted", player) ?? false)
            {
                return;
            }

            StringBuilder sb = new StringBuilder(message);

            FilterText(sb);

            if (HandlePluginSupport(player, sb, channel))
            {
                return;
            }

            if (!HandleTranslate(sb, _pluginConfig.PluginSupport.ChatTranslator.DiscordServerLanguage, lang.GetLanguage(player.Id), translatedMessage => { HandleChatMessage(player, translatedMessage, channel); }))
            {
                HandleChatMessage(player, sb, channel);
            }
        }

        private void HandleChatMessage(IPlayer player, StringBuilder message, int channel)
        {
            if (!_pluginConfig.MessageSettings.ServerToDiscord)
            {
                return;
            }

            #if RUST
            if (channel == (int) Chat.ChatChannel.Team)
            {
                SendMessageToDiscordTeamChannel(player, message);
                return;
            }

            if (_pluginConfig.MessageSettings.ServerToDiscord && (channel == (int) Chat.ChatChannel.Global || channel == (int) Chat.ChatChannel.Server))
            {
                SendMessageToDiscordChatChannel(player, message);
            }
#else
            SendMessageToDiscordChatChannel(player, message);
#endif
        }
        
#if RUST
        private void SendMessageToDiscordTeamChannel(IPlayer player, StringBuilder message)
        {
            if (_teamSend == null)
            {
                return;
            }

            string content = message.ToString();
            if (ShouldCleanTeamMessage())
            {
                content = GetClearMessage(content);
            }

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            if (BetterChat != null)
            {
                _teamSend.QueueMessage(Lang(LangKeys.Discord.TeamChannel.BetterChatTeamMessage, player, GetServerTime(), GetBetterChatConsoleMessage(player, content)));
            }
            else
            {
                _teamSend.QueueMessage(Lang(LangKeys.Discord.TeamChannel.TeamChatMessage, player, GetServerTime(), GetPlayerName(player), message.ToString()));
            }
        }
#endif
        #endregion

        #region Join Leave Handling
        private void OnUserConnected(IPlayer player)
        {
            SendMessageToJoinLeaveChannel(player, LangKeys.Discord.JoinLeave.ConnectedMessage, string.Empty);
        }

        private void OnUserDisconnected(IPlayer player, string reason)
        {
            SendMessageToJoinLeaveChannel(player, LangKeys.Discord.JoinLeave.DisconnectedMessage, reason);
        }
        
        private void SendMessageToJoinLeaveChannel(IPlayer player, string langKey, string reason)
        {
            _joinLeaveSend?.QueueMessage(Lang(langKey, player, GetServerTime(), GetPlayerName(player), reason));
        }
        #endregion

        #region Discord Chat Channel Handling
        private void HandleDiscordChatChannelMessage(DiscordMessage message)
        {
            if (!_pluginConfig.MessageSettings.DiscordToServer)
            {
                return;
            }

            if (ShouldIgnoreUser(message.Author))
            {
                return;
            }

            if (_pluginConfig.MessageSettings.Filter.IgnoredPrefixes.Any(c => message.Content.StartsWith(c, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            StringBuilder sb = new StringBuilder(message.Content);
            FilterText(sb);
            
            IPlayer player = message.Author.Player;
            if (player == null)
            {
                if (!_pluginConfig.MessageSettings.UnlinkedSettings.AllowedUnlinked)
                {
                    message.Reply(_client, Lang(LangKeys.Discord.ChatChannel.NotLinked), notLinked =>
                    {
                        timer.In(1f, () =>
                        {
                            notLinked.DeleteMessage(_client);
                        });
                    });
                    return;
                }

                HandleUnlinkedPlayerMessage(message.Author, sb, message);
                return;
            }

            if (IsInAdminDeepCover(player))
            {
                return;
            }
            
            if (ShouldCleanChatMessage())
            {
                message.Content = GetClearMessage(message.Content);
            }

            if (string.IsNullOrEmpty(message.Content))
            {
                return;
            }

            HandleLinkedPlayerMessage(player, sb, message);
        }

        private void HandleLinkedPlayerMessage(IPlayer player, StringBuilder content, DiscordMessage message)
        {
            //Handle Displaying the message in discord from the bot
            if (_pluginConfig.MessageSettings.UseBotMessageDisplay)
            {
                if (!HandleTranslate(content, _pluginConfig.PluginSupport.ChatTranslator.DiscordServerLanguage, lang.GetLanguage(player.Id), translatedText => SendMessageToDiscordChatChannel(player, translatedText)))
                {
                    SendMessageToDiscordChatChannel(player, content);
                }

                timer.In(.1f, () =>
                {
                    message.DeleteMessage(_client);
                });
            }

            bool playerReturn = false;
#if RUST
            //Let other chat plugins process first
            if (player.Object != null)
            {
                Unsubscribe(nameof(OnPlayerChat));
                playerReturn = Interface.Call(nameof(OnPlayerChat), player.Object, content, Chat.ChatChannel.Global) != null;
                Subscribe(nameof(OnPlayerChat));
            }
#endif

            //Let other chat plugins process first
            Unsubscribe("OnUserChat");
            bool userReturn = Interface.Call("OnUserChat", player, content) != null;
            Subscribe("OnUserChat");

            if (playerReturn || userReturn)
            {
                return;
            }

            string parsedMessage = content.ToString();
            //We need to process here because player.Object can be null
            if (BetterChat != null)
            {
                server.Broadcast(GetBetterChatMessage(player, parsedMessage));
                Puts(GetBetterChatConsoleMessage(player, parsedMessage));
                return;
            }

            string discordTag = string.Empty;
            if (_pluginConfig.EnableServerChatTag)
            {
                discordTag = Lang(LangKeys.InGame.DiscordTag, player);
            }

            parsedMessage = Lang(LangKeys.InGame.InGameMessage, player, discordTag, player.Name, parsedMessage);
            server.Broadcast(parsedMessage);
            Puts(Formatter.ToPlaintext(parsedMessage));
        }

        private void HandleUnlinkedPlayerMessage(DiscordUser user, StringBuilder content, DiscordMessage message)
        {
            //Handle Displaying the message in discord from the bot
            if (_pluginConfig.MessageSettings.UseBotMessageDisplay)
            {
                if (!HandleTranslate(content, _pluginConfig.PluginSupport.ChatTranslator.DiscordServerLanguage, "auto", translated => SendMessageToDiscordChatChannel(user,translated)))
                {
                    SendMessageToDiscordChatChannel(user, content);
                }
                
                timer.In(.1f, () =>
                {
                    message.DeleteMessage(_client);
                });
            }

            string parsedMessage = Lang(LangKeys.InGame.UnlinkedChat, null, Lang(LangKeys.InGame.DiscordTag), user.Username, user.Discriminator, content.ToString());
#if RUST
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0,  _pluginConfig.MessageSettings.UnlinkedSettings.SteamIcon, Formatter.ToUnity(parsedMessage));
#else
            server.Broadcast(parsedMessage);
#endif
            Puts(Formatter.ToPlaintext(parsedMessage));
        }
        
        private void SendMessageToDiscordChatChannel(DiscordUser user, StringBuilder message)
        {
            _chatSend?.QueueMessage(Lang(LangKeys.Discord.ChatChannel.UnlinkedMessage, null, GetServerTime(), user.Username, user.Discriminator, message.ToString()));
        }
        #endregion

        #region Plugin Support
        private bool HandlePluginSupport(IPlayer player, StringBuilder message, int channel = 0)
        {
            if (IsInAdminDeepCover(player) && (channel == 0 || channel == 2))
            {
                return true;
            }
            
            if (_pluginConfig.PluginSupport.AdminChat.Enabled)
            {
                string adminChatPrefix = _pluginConfig.PluginSupport.AdminChat.AdminChatPrefix;
                if (StartsWith(message, adminChatPrefix) || IsInAdminChat(player))
                {
                    if (message.Length < adminChatPrefix.Length)
                    {
                        message.Replace(adminChatPrefix, string.Empty, 0, adminChatPrefix.Length);
                    }
                   
                    _adminChatSend.QueueMessage(Lang(LangKeys.Discord.AdminChat.ChannelMessage, player, GetServerTime(), player.Name, message));
                    
                    SendMessageToChannel(_pluginConfig.PluginSupport.AdminChat.ChatChannel, message);
                    return _pluginConfig.PluginSupport.AdminChat.ExcludeDefault;
                }
            }

            return false;
        }

        #region BetterChat Tag
        private string GetDiscordTag(IPlayer player)
        {
            if (player.IsConnected)
            {
                return null;
            }

            return Lang(LangKeys.InGame.DiscordTag, player);
        }
        #endregion
        
        #region Admin Chat
        private void HandleAdminChatMessage(DiscordMessage message)
        {
            if (ShouldIgnoreUser(message.Author))
            {
                return;
            }
            
            if (_pluginConfig.MessageSettings.Filter.IgnoredPrefixes.Any(c => message.Content.StartsWith(c, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            IPlayer player = message.Author.Player;
            if (player == null)
            {
                message.Reply(_client, LangKeys.Discord.AdminChat.NotLinked, notlinked =>
                {
                    timer.In(1f, () =>
                    {
                        notlinked.DeleteMessage(_client);
                    });
                });
                return;
            }

            AdminChat.Call("SendAdminMessage", player, message);

            timer.In(.25f, () => { message.DeleteMessage(_client); });
        }
        #endregion

        #region Translation API
        private bool HandleTranslate(StringBuilder message, string to, string from, Action<StringBuilder> callback)
        {
            if (ChatTranslator != null && ChatTranslator.IsLoaded)
            {
                TranslationAPI.Call("Translate", message.ToString(), to, from, new Action<string>(translatedText => { callback.Invoke(new StringBuilder(translatedText)); }));
                return true;
            }
            
            return false;
        }
        #endregion

        #region AntiSpamNames

        private string GetClearMessage(string message)
        {
            return AntiSpamNames.Call<string>("GetClearText", message);
        }

        private string GetClearName(IPlayer player)
        {
            if (!ShouldCleanPlayerNames())
            {
                return player.Name;
            }
            
            return AntiSpamNames.Call<string>("GetClearName", player);
        }
        #endregion
        #endregion

        #region Channel Message Sending Helpers
        private void SendMessageToDiscordChatChannel(IPlayer player, StringBuilder message)
        {
            string content = message.ToString();
            if (ShouldCleanChatMessage())
            {
                content = GetClearMessage(content);
            }

            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            
            if (BetterChat != null)
            {
                _chatSend?.QueueMessage(Lang(LangKeys.Discord.ChatChannel.BetterChatMessage, player, GetServerTime(), GetBetterChatConsoleMessage(player, content)));
            }
            else
            {
                _chatSend?.QueueMessage(Lang(LangKeys.Discord.ChatChannel.ChatMessage, player, GetServerTime(), GetPlayerName(player), message.ToString()));
            }
        }
        #endregion

        #region Helpers
        private DateTime GetServerTime()
        {
            return DateTime.Now + TimeSpan.FromHours(_pluginConfig.MessageSettings.ServerTimeOffset);
        }

        private void FilterText(StringBuilder message)
        {
            foreach (KeyValuePair<string, string> replacement in _pluginConfig.MessageSettings.TextReplacements)
            {
                message.Replace(replacement.Key, replacement.Value);
            }
        }

        public bool StartsWith(StringBuilder sb, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (sb[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        private void SendMessageToChannel(Snowflake channelId, StringBuilder message)
        {
            DiscordMessage.CreateMessage(_client, channelId, new MessageCreate
            {
                Content = message.ToString(),
                AllowedMention = _allowedMention
            });
        }

        private string GetPlayerName(IPlayer player)
        {
            if (Clans != null && Clans.IsLoaded)
            {
                string clanTag = Clans.Call<string>("GetClanOf", player.Id);
                if (!string.IsNullOrEmpty(clanTag))
                {
                    return $"{Lang(LangKeys.InGame.ClanTag, player, clanTag)}{GetClearName(player)}";
                }
            }
            
            return GetClearName(player);
        }

        public bool ShouldIgnoreUser(DiscordUser user)
        {
            if (user.Bot ?? false)
            {
                return true;
            }

            return _pluginConfig.MessageSettings.Filter.IgnoreUsers.Contains(user.Id);
        }

        public bool IsLoaded(Plugin plugin) => plugin != null && plugin.IsLoaded;

        private bool IsInAdminChat(IPlayer player) => IsLoaded(AdminChat) && AdminChat.Call<bool>("HasAdminChatEnabled", player);
        
        public bool IsInAdminDeepCover(IPlayer player) => IsLoaded(AdminDeepCover) && player.Object != null && AdminDeepCover.Call<bool>("API_IsDeepCovered", player.Object);

        public bool ShouldCleanNames() => AntiSpamNames != null && AntiSpamNames.IsLoaded;
        public bool ShouldCleanPlayerNames() => ShouldCleanNames() && _pluginConfig.PluginSupport.AntiSpamNames.ValidateNicknames;
        public bool ShouldCleanChatMessage() => ShouldCleanNames() && _pluginConfig.PluginSupport.AntiSpamNames.ChatMessage;
        #if RUST
        public bool ShouldCleanTeamMessage() => ShouldCleanNames() && _pluginConfig.PluginSupport.AntiSpamNames.TeamMessage;
        #endif

        public string GetBetterChatConsoleMessage(IPlayer player, string message)
        {
            return BetterChat.Call<string>("API_GetFormattedMessage", player, message, true);
        }

        public string GetBetterChatMessage(IPlayer player, string message)
        {
            return BetterChat.Call<string>("API_GetFormattedMessage", player, message, false);
        }

        public string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        #endregion

        #region Classes
        public class PluginConfig
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "Discord Server ID")]
            public Snowflake GuildId { get; set; }

            [DefaultValue(true)]
            [JsonProperty("Enable Adding Discord Tag To In Game Messages When Sent From Discord")]
            public bool EnableServerChatTag { get; set; } = true;
            
            [JsonProperty("Channel Settings")]
            public ChannelSettings ChannelSettings { get; set; } = new ChannelSettings();

            [JsonProperty("Message Settings")]
            public MessageSettings MessageSettings { get; set; } = new MessageSettings();
            
            [JsonProperty("Plugin Support")]
            public PluginSupport PluginSupport { get; set; } = new PluginSupport();

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; } = LogLevel.Info;
        }

        public class ChannelSettings
        {
            [JsonProperty("Chat Channel ID")]
            public Snowflake ChatChannel { get; set; }

#if RUST
            [JsonProperty("Team Channel ID")]
            public Snowflake TeamChannel { get; set; }
#endif

            [JsonProperty("Join / Leave Channel ID")]
            public Snowflake JoinLeaveChannel { get; set; }
        }

        public class MessageSettings
        {
            [JsonProperty("Replace Discord User Message With Bot Message")]
            public bool UseBotMessageDisplay { get; set; } = true;

            [JsonProperty("Send Messages From Server Chat To Discord Channel")]
            public bool ServerToDiscord { get; set; } = true;

            [JsonProperty("Send Messages From Discord Channel To Server Chat")]
            public bool DiscordToServer { get; set; } = true;

            [JsonProperty("Discord Message Server Time Offset (Hours)")]
            public float ServerTimeOffset { get; set; }

            [JsonProperty("Text Replacements")]
            public Hash<string, string> TextReplacements { get; set; } = new Hash<string, string> {["TextToBeReplaced"] = "ReplacedText"};

            [JsonProperty("Unlinked Settings")]
            public UnlinkedSettings UnlinkedSettings { get; set; } = new UnlinkedSettings();

            [JsonProperty("Message Filter Settings")]
            public MessageFilterSettings Filter { get; set; } = new MessageFilterSettings();
        }

        public class UnlinkedSettings
        {
            [JsonProperty("Allow Unlinked Players To Chat With Server")]
            public bool AllowedUnlinked { get; set; } = true;

#if RUST
            [JsonProperty("Steam Icon ID")]
            public ulong SteamIcon { get; set; } = 76561199144296099;
#endif
        }

        public class MessageFilterSettings
        {
            [JsonProperty("Ignore messages from users in this list (Discord ID)")]
            public List<Snowflake> IgnoreUsers { get; set; } = new List<Snowflake>();

            [JsonProperty("Ignored Prefixes")]
            public List<string> IgnoredPrefixes { get; set; } = new List<string>();
        }
        
        public class PluginSupport
        {
            [JsonProperty("AdminChat Settings")]
            public AdminChatSettings AdminChat { get; set; } = new AdminChatSettings();

            [JsonProperty("ChatTranslator Settings")]
            public ChatTranslatorSettings ChatTranslator { get; set; } = new ChatTranslatorSettings();

            [JsonProperty("AntiSpamNames Settings")]
            public AntiSpamNamesSettings AntiSpamNames { get; set; } = new AntiSpamNamesSettings();
        }

        public class AdminChatSettings
        {
            [JsonProperty("Enable AdminChat Plugin Support")]
            public bool Enabled { get; set; }

            [JsonProperty("Exclude From Chat Channel")]
            public bool ExcludeDefault { get; set; } = true;

            [JsonProperty("Admin Chat Channel ID")]
            public Snowflake ChatChannel { get; set; }

            [JsonProperty("Admin Chat Prefix")]
            public string AdminChatPrefix { get; set; } = "@";
        }

        public class ChatTranslatorSettings
        {
            [JsonProperty("Discord Server Chat Language")]
            public string DiscordServerLanguage { get; set; }
        }
        
        public class AntiSpamNamesSettings
        {
            [JsonProperty("Use AntiSpamNames On Player Names")]
            public bool ValidateNicknames { get; set; } = false;

            [JsonProperty("Use AntiSpamNames On Chat Messages")]
            public bool ChatMessage { get; set; } = false;
            #if RUST
            [JsonProperty("Use AntiSpamNames On Team Messages")]
            public bool TeamMessage { get; set; } = false;
            #endif
        }

        public class DiscordTimedSend
        {
            private readonly StringBuilder _message = new StringBuilder();
            private Timer _sendTimer;
            private readonly Snowflake _channelId;

            public DiscordTimedSend(Snowflake channelId)
            {
                _channelId = channelId;
            }

            public void QueueMessage(string message)
            {
                if (_sendTimer == null)
                {
                    _sendTimer = _ins.timer.In(1f, () =>
                    {
                        _ins.SendMessageToChannel(_channelId, _message);
                        _message.Length = 0;
                        _sendTimer = null;
                    });
                }
                
                _message.AppendLine(message);
            }
        }

        private static class LangKeys
        {
            public static class Discord
            {
                private const string Base = nameof(Discord) + ".";
                
                public static class ChatChannel
                {
                    private const string Base = Discord.Base + nameof(ChatChannel) + ".";
                        
                    public const string ChatMessage = Base + nameof(ChatMessage);
                    public const string UnlinkedMessage = Base + nameof(UnlinkedMessage);
                    public const string BetterChatMessage = Base + nameof(BetterChatMessage);
                    public const string NotLinked = Base + nameof(NotLinked);
                }

                public static class TeamChannel
                {
                    private const string Base = Discord.Base + nameof(TeamChannel) + ".";
                    
                    public const string TeamChatMessage = Base + nameof(TeamChatMessage);
                    public const string BetterChatTeamMessage = Base + nameof(BetterChatTeamMessage);
                }
                
                public static class JoinLeave
                {
                    private const string Base = Discord.Base + nameof(JoinLeave) + ".";
                        
                    public const string ConnectedMessage = Base + nameof(ConnectedMessage);
                    public const string DisconnectedMessage = Base + nameof(DisconnectedMessage);
                }
                
                public static class AdminChat
                {
                    private const string Base = Discord.Base + nameof(AdminChat) + ".";
                        
                    public const string ChannelMessage = Base + nameof(ChannelMessage);
                    public const string NotLinked = Base + nameof(NotLinked);
                }
            }
            
            public static class InGame
            {
                private const string Base = nameof(InGame) + ".";
                    
                public const string InGameMessage = Base + nameof(InGameMessage);
                public const string DiscordTag = Base + nameof(DiscordTag);
                public const string UnlinkedChat = Base + nameof(UnlinkedChat);
                public const string ClanTag = Base + nameof(ClanTag);
            }
        }
        #endregion
    }
}