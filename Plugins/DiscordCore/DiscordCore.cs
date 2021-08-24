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
using Oxide.Ext.Discord.Entities.Applications;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Emojis;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.ApplicationCommands;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Helpers;
using Oxide.Ext.Discord.Libraries.Command;
using Oxide.Ext.Discord.Libraries.Linking;
using Oxide.Ext.Discord.Logging;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("Discord Core", "MJSU", "2.0.1")]
    [Description("Creates a link between a player and discord")]
    internal class DiscordCore : CovalencePlugin, IDiscordLinkPlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config
        
        private const string AccentColor = "de8732";
        private const string UsePermission = "discordcore.use";

        private char[] _linkChars;

        private DiscordUser _bot;
        private DiscordGuild _guild;
        private Snowflake _guildId = default(Snowflake);
        
        private bool _initialized;
        private readonly DiscordSettings _discordSettings = new DiscordSettings
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages
        };

        private readonly Hash<string, int> _badLinkAttempts = new Hash<string, int>();
        private readonly List<LinkActivation> _activations = new List<LinkActivation>();
        private readonly List<Snowflake> _allowedCommandChannels = new List<Snowflake>();
        private readonly List<string> _allowedCommandChannelNames = new List<string>();
        private char _cmdPrefix;
        
        private readonly DiscordLink _link = Interface.Oxide.GetLibrary<DiscordLink>();
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();

        private Action<IPlayer, DiscordUser> _onLinked;
        private Action<IPlayer, DiscordUser> _onUnlinked;

        private const string AcceptEmoji = "✅";
        private const string DeclineEmoji = "❌";
        private const string LinkAccountsButtonId = nameof(DiscordCore) + "_LinkAccounts";
        private const string AcceptLinkButtonId = nameof(DiscordCore) + "_AcceptLink";
        private const string DeclineLinkButtonId = nameof(DiscordCore) + "_DeclineLink";
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);

            permission.RegisterPermission(UsePermission, this);

            _linkChars = _pluginConfig.LinkSettings.LinkCodeCharacters.ToCharArray();
            _cmdPrefix = _dcCommands.CommandPrefixes[0];

            _discordSettings.ApiToken = _pluginConfig.ApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
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

        public PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.LinkSettings = new DiscordLinkingSettings(config.LinkSettings);
            return config;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.NoPermission] = "You do not have permission to use this command",
                [LangKeys.ChatFormat] = $"[#BEBEBE][[#{AccentColor}]{Title}[/#]] {{0}}[/#]",
                [LangKeys.DiscordFormat] = $"[{Title}] {{0}}",
                [LangKeys.DiscordCoreOffline] = "Discord Core is not online. Please try again later",
                [LangKeys.Commands.Unknown] = "Unknown Command",
                [LangKeys.GenericError] = "We have encountered an error trying. PLease try again later.",
                [LangKeys.ConsolePlayerNotSupported] = "You cannot use this command from the server console.",
                [LangKeys.Commands.Leave.Errors.NotLinked] = "We were unable to unlink your account as you do not appear to have been linked.",
                
                [LangKeys.Commands.Join.Modes] = "Please select which which mode you would like to use to link with discord.\n" +
                                              $"If you wish to join using a code please type [#{AccentColor}]/{{0}} {{1}} {{2}}[/#]\n" +
                                              $"If you wish to join by your discord username please type [#{AccentColor}]/{{0}} {{1}} {{{{Username}}}}#{{{{Discriminator}}}}[/#]",
                
                [LangKeys.Commands.Join.Errors.AlreadySignedUp] = $"You have already linked your discord and game accounts. If you wish to remove this link type [#{AccentColor}]{{0}}{{1}} {{2}}[/#]",
                [LangKeys.Commands.Join.Errors.UnableToFindUser] = $"Unable to find user '{{0}}' in the {{1}} discord server. Have you joined the {{1}} discord server @ [#{AccentColor}]discord.gg/{{2}}[/#]?",
                [LangKeys.Commands.Join.Errors.FoundMultipleUsers] = "Found multiple users with username '{0}' in the {1} discord server. Please include more of the username and discriminator if possible.",
                [LangKeys.Commands.Join.Errors.UsernameSearchError] = "An error occured while trying to search by username. Please try a different username or try again later.",
                [LangKeys.Commands.Join.Errors.InvalidSyntax] = $"Invalid syntax. Type [#{AccentColor}]{{0}}{{1}} code 123456[/#] where 123456 is the code you got from discord",
                [LangKeys.Commands.Join.Errors.NoPendingActivations] = "There is no link currently in progress with the code '{0}'. Please confirm your code and try again.",
                [LangKeys.Commands.Join.Errors.LinkInProgress] = "You already have an existing link in process. Please continue from that link.",
                [LangKeys.Commands.Join.Errors.Banned] = "You have been banned from any more link attempts.",
                [LangKeys.Commands.Join.Complete.Info] = "To completed your activate please use the following command: \"{0}{1} {2} {3}\".\n",
                [LangKeys.Commands.Join.Complete.InfoGuildAny] = "This command can be used in any guild channel.\n",
                [LangKeys.Commands.Join.Complete.InfoGuildChannel] = "This command can only used in the following guild channels / categories {0}.\n",
                [LangKeys.Commands.Join.Complete.InfoAlsoDm] = "This command can also be used in a direct message to guild bot {0}",
                [LangKeys.Commands.Join.Complete.InfoDmOnly] = "This command can only be used in a direct message to the guild bot {0}",
                [LangKeys.Commands.Join.Messages.Discord.Accept] = "Accept",
                [LangKeys.Commands.Join.Messages.Discord.Decline] = "Decline",
                [LangKeys.Commands.Join.Messages.Discord.LinkAccounts] = "Link Accounts",
                [LangKeys.Commands.Join.Messages.Discord.Username] = "The player '{0}' is trying to link their game account to this discord.\n" +
                                                                     "If you could like to accept please click on the {1} button.\n" +
                                                                     "If you did not initiate this link please click on the {2} button",
                [LangKeys.Commands.Join.Messages.Chat.UsernameDmSent] = "Our bot {0} has sent you a discord direct message. Please finish your setup there.",
                [LangKeys.Commands.Join.Messages.Discord.CompletedInGame] = $"To completed your activation please use the following command: {DiscordFormatting.Bold("{0}{1} {2} {3}")} in game.",
                [LangKeys.Commands.Join.Messages.Discord.CompleteInGameResponse] = "Please check your DM's for steps on how to complete the link process.",
                [LangKeys.Commands.Join.Messages.Discord.Declined] = "We have successfully declined the link request. We're sorry for the inconvenience.",
                [LangKeys.Commands.Join.Messages.Chat.Declined] = "Your join request was declined by the discord user. Repeated declined attempts will result in a link ban.",
                [LangKeys.Linking.Chat.Linked] = "You have successfully linked with discord user {0}#{1}.",
                [LangKeys.Linking.Discord.Linked] = "You have successfully linked your discord {0}#{1} with in game player {2}",
                [LangKeys.Linking.Chat.Unlinked] = "You have successfully unlinked player {0} with your discord account.",
                [LangKeys.Linking.Discord.Unlinked] = "You have successfully unlinked your discord {0}#{1} with in game player {2}",

                [LangKeys.Notifications.Link] = "Player {0}({1}) has linked with discord {2}({3})",
                [LangKeys.Notifications.Rejoin] = "Player {0}({1}) has rejoined and was linked with discord {2}({3})",
                [LangKeys.Notifications.Unlink] = "Player {0}({1}) has unlinked their discord {2}({3})",

                [LangKeys.Guild.WelcomeMessage] = "Welcome to the {0} discord server. If you would like to link your discord account to {1} game server please respond to this message with {2}{3} {4}.",
                [LangKeys.Guild.LinkMessage] = "Welcome to the {0} discord server. " +
                                                      "This server supports linking your discord and in game accounts. " +
                                                      "If you would like to begin this process please click on the {1} button below this message.\n" +
                                                      $"{DiscordFormatting.Underline("Note: You must be in game to complete the link.")}",
                
                [LangKeys.Emoji.Accept] = AcceptEmoji,
                [LangKeys.Emoji.Decline] = DeclineEmoji,

                [LangKeys.Commands.ChatHelpText] = $"Allows players to link their player and discord accounts together. Players must first join the {{0}} Discord @ [#{AccentColor}]discord.gg/{{1}}[/#]\n" +
                                          $"Type [#{AccentColor}]/{{2}} {{3}} [/#] to start the link process\n" +
                                          $"Type [#{AccentColor}]/{{2}} {{4}}[/#] to to unlink yourself from discord\n" +
                                          $"Type [#{AccentColor}]/{{2}}[/#] to see this message again",
                
                [LangKeys.Commands.DiscordHelpText] = "Allows players to link their in game player and discord accounts together.\n" + 
                                             "Type {0}{1} {2} to start the link process\n" +
                                             "Type {0}{1} {3} to to unlink yourself from discord\n" +
                                             "Type {0}{1} to see this message again",

                //Commands
                [CommandKeys.ChatCommand] = "dc",
                [CommandKeys.ChatJoinCommand] = "join",
                [CommandKeys.ChatLeaveCommand] = "leave",
                [CommandKeys.ChatJoinCodeCommand] = "code",
                [CommandKeys.DiscordCommand] = "dc",
                [CommandKeys.DiscordJoinCommand] = "join",
                [CommandKeys.DiscordLeaveCommand] = "leave"
            }, this);
        }
        
        private void OnServerInitialized()
        {
            RegisterChatLangCommand(nameof(DiscordCoreChatCommand), CommandKeys.ChatCommand);

            _link.AddLinkPlugin(this);

            if (string.IsNullOrEmpty(_pluginConfig.ApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }

            ConnectClient();
        }
        
        private void OnPluginLoaded(Plugin plugin)
        {
            if (ReferenceEquals(this, plugin))
            {
                return;
            }
            
            DiscordCoreReady(plugin);
        }

        private void Unload()
        {
            Interface.Oxide.CallHook("OnDiscordCoreClose");
            SaveData();
        }
        #endregion
        
        #region Client Connection
        public void ConnectClient()
        {
            if (string.IsNullOrEmpty(_pluginConfig.ApiKey))
            {
                PrintWarning("Please enter your discord bot API key and reload the plugin");
                return;
            }

            _client.Connect(_discordSettings);
        }
        
        public void DiscordCoreReady(Plugin plugin)
        {
            NextTick(() =>
            {
                if (plugin == null)
                {
                    Interface.CallHook("OnDiscordCoreReady", _client, _bot, _guild);
                }
                else
                {
                    plugin.CallHook("OnDiscordCoreReady", _client, _bot, _guild);
                }
            });
        }
        #endregion

        #region Chat Commands
        private void DiscordCoreChatCommand(IPlayer player, string cmd, string[] args)
        {
            if (player.Id == "server_console")
            {
                Chat(player, LangKeys.ConsolePlayerNotSupported);
                return;
            }
            
            if (!player.HasPermission(UsePermission))
            {
                Chat(player, LangKeys.NoPermission);
                return;
            }

            if (!IsDiscordCoreOnline())
            {
                Chat(player, LangKeys.DiscordCoreOffline);
                return;
            }

            if (args.Length == 0)
            {
                DisplayHelp(player);
                return;
            }

            string option = args[0];
            if (option.Equals(Lang(CommandKeys.ChatJoinCommand, player), StringComparison.OrdinalIgnoreCase))
            {
                HandleChatJoin(player, args);
                return;
            }

            if (option.Equals(Lang(CommandKeys.ChatLeaveCommand, player), StringComparison.OrdinalIgnoreCase))
            {
                GuildMember discord = player.GetGuildMember(_guild);
                if (discord != null)
                {
                    HandleLeave(player, discord.User, false);
                }
                else
                {
                    Chat(player, LangKeys.Commands.Leave.Errors.NotLinked);
                }

                return;
            }

            if (option.Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                HandleChatCompleteLink(player, args);
                return;
            }

            DisplayHelp(player);
        }

        public void DisplayHelp(IPlayer player)
        {
            Chat(player, LangKeys.Commands.ChatHelpText, GetDiscordServerName(), _pluginConfig.JoinCode, Lang(CommandKeys.ChatCommand, player), Lang(CommandKeys.ChatJoinCommand, player), Lang(CommandKeys.ChatLeaveCommand, player));
        }

        public void HandleChatJoin(IPlayer player, string[] args)
        {
            if (_link.IsLinked(player.Id))
            {
                Chat(player, LangKeys.Commands.Join.Errors.AlreadySignedUp, "/", Lang(CommandKeys.ChatCommand, player), Lang(CommandKeys.ChatLeaveCommand, player));
                return;
            }
            
            LinkActivation existing = _activations.FirstOrDefault(a => a.Player?.Id == player.Id);
            if (existing != null)
            {
                Chat(player,LangKeys.Commands.Join.Errors.LinkInProgress);
                return;
            }

            if (args.Length == 1)
            {
                Chat(player, LangKeys.Commands.Join.Modes, Lang(CommandKeys.ChatCommand, player), Lang(CommandKeys.ChatJoinCommand, player), Lang(CommandKeys.ChatJoinCodeCommand, player));
                return;
            }

            if (args[1].Equals(Lang(CommandKeys.ChatJoinCodeCommand, player), StringComparison.OrdinalIgnoreCase))
            {
                HandleChatJoinWithCode(player);
            }
            else
            {
                HandleChatJoinWithUserName(player, args[1]);
            }
        }

        public void HandleChatJoinWithCode(IPlayer player)
        {
            string code = GenerateCode();
            _activations.Add(new LinkActivation
            {
                Player = player,
                Code = code
            });

            StringBuilder message = new StringBuilder();

            message.Append(Lang(LangKeys.Commands.Join.Complete.Info, player,_cmdPrefix, Lang(CommandKeys.DiscordCommand, player), "code", code ));
            
            if (_pluginConfig.LinkSettings.AllowCommandsInGuild)
            {
                if (_allowedCommandChannels.Count == 0)
                {
                    message.Append(Lang(LangKeys.Commands.Join.Complete.InfoGuildAny, player));
                }
                else
                {
                    message.Append(Lang(LangKeys.Commands.Join.Complete.InfoGuildChannel, player, string.Join(", ", _allowedCommandChannelNames)));
                }

                message.Append(Lang(LangKeys.Commands.Join.Complete.InfoAlsoDm, player, _bot.Username));
            }
            else
            {
                message.Append(Lang(LangKeys.Commands.Join.Complete.InfoDmOnly, player, _bot.Username));
            }
            
            Chat(player, message.ToString());
        }

        private void HandleChatJoinWithUserName(IPlayer player, string userName)
        {
            if (_badLinkAttempts[player.Id] >= 3)
            {
                Chat(player, LangKeys.Commands.Join.Errors.Banned);
                return;
            }
            
            string[] userInfo = userName.Split('#');
            
            _guild.SearchGuildMembers(_client, userInfo[0], 1000, members =>
            {
                HandleChatJoinUserNameResults(player, userInfo, members);
            }, error =>
            {
                player.Message(Lang(LangKeys.Commands.Join.Errors.UsernameSearchError, player));
            });
        }

        private void HandleChatJoinUserNameResults(IPlayer player, string[] userInfo, List<GuildMember> members)
        {
            if (members.Count == 0)
            {
                Chat(player, LangKeys.Commands.Join.Errors.UnableToFindUser, userInfo[0], GetDiscordServerName(), _pluginConfig.JoinCode);
                return;
            }

            DiscordUser user = null;

            int count = 0;
            string userName = userInfo[0];
            string discriminator = userInfo.Length > 1 ? userInfo[1] : null;
            foreach (GuildMember member in members)
            {
                DiscordUser searchUser = member.User;
                if (discriminator == null)
                {
                    if (searchUser.Username.StartsWith(userName, StringComparison.OrdinalIgnoreCase))
                    {
                        user = searchUser;
                        count++;
                        if (count > 1)
                        {
                            break;
                        }
                    }
                }
                else if (searchUser.Username.Equals(userName, StringComparison.OrdinalIgnoreCase) && searchUser.Discriminator.Equals(discriminator))
                {
                    user = searchUser;
                    break;
                }
            }

            if (user == null || count > 1)
            {
                Chat(player, LangKeys.Commands.Join.Errors.FoundMultipleUsers, userName, GetDiscordServerName());
                return;
            }

            ActionRowComponent row = new ActionRowComponent();
            row.AddComponent(new ButtonComponent {CustomId = $"{AcceptLinkButtonId} {user.Id}", Emoji = DiscordEmoji.FromCharacter(Lang(LangKeys.Emoji.Accept, player)), Style = ButtonStyle.Success, Label = Lang(LangKeys.Commands.Join.Messages.Discord.Accept, player)});
            row.AddComponent(new ButtonComponent {CustomId = $"{DeclineLinkButtonId} {user.Id}", Emoji = DiscordEmoji.FromCharacter(Lang(LangKeys.Emoji.Decline, player)), Style = ButtonStyle.Danger, Label = Lang(LangKeys.Commands.Join.Messages.Discord.Decline, player)});
            MessageCreate create = new MessageCreate
            {
                Content = GetDiscordFormattedMessage(LangKeys.Commands.Join.Messages.Discord.Username, player, player.Name, Lang(LangKeys.Commands.Join.Messages.Discord.Accept, player), Lang(LangKeys.Commands.Join.Messages.Discord.Decline, player)),
                Components = new List<ActionRowComponent> {row}
            };

            user.SendDirectMessage(_client, create, message =>
            {
                _activations.Add(new LinkActivation
                {
                    Player = player,
                    Discord = user,
                    Channel = message.ChannelId
                });
                
                Chat(player, LangKeys.Commands.Join.Messages.Chat.UsernameDmSent, _bot.Username);
            }, error =>
            {
                Chat(player, LangKeys.GenericError);
            });
        }

        public void HandleChatCompleteLink(IPlayer player, string[] args)
        {
            if (_link.IsLinked(player.Id))
            {
                Chat(player, Lang(LangKeys.Commands.Join.Errors.AlreadySignedUp, player, Lang(CommandKeys.ChatCommand, player), Lang(CommandKeys.ChatLeaveCommand, player)));
                return;
            }

            if (args.Length < 2)
            {
                Chat(player, Lang(LangKeys.Commands.Join.Errors.InvalidSyntax, player, Lang(CommandKeys.ChatCommand, player)));
                return;
            }

            LinkActivation act = _activations.FirstOrDefault(a => a.Code == args[1]);
            if (act == null)
            {
                Chat(player, Lang(LangKeys.Commands.Join.Errors.NoPendingActivations, player, args[1]));
                return;
            }

            act.Player = player;

            CompletedLink(act);
        }
        #endregion
        
        #region Discord Commands
        private void DiscordCoreMessageCommand(DiscordMessage message, string cmd, string[] args)
        {
            IPlayer player = message.Author.Player;
            if (args.Length == 0)
            {
                DisplayDiscordHelp(message, player);
                return;
            }
            
            string option = args[0];
            if (option.Equals(Lang(CommandKeys.DiscordJoinCommand,player), StringComparison.OrdinalIgnoreCase))
            {
                HandleDiscordJoin(message, player);
                return;
            }

            if (option.Equals(Lang(CommandKeys.DiscordLeaveCommand, player), StringComparison.OrdinalIgnoreCase))
            {
                if (player != null)
                {
                    HandleLeave(player, message.Author, false);
                }
                else
                {
                    message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Leave.Errors.NotLinked));
                }

                return;
            }

            if (option.Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                HandleDiscordCompleteLink(message, args);
                return;
            }

            DisplayDiscordHelp(message, player);
        }

        public void DisplayDiscordHelp(DiscordMessage message, IPlayer player)
        {
            message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.DiscordHelpText, player, _cmdPrefix, Lang(CommandKeys.DiscordCommand, player), Lang(CommandKeys.DiscordJoinCommand, player), Lang(CommandKeys.DiscordLeaveCommand, player)));
        }
        
        public void HandleDiscordJoin(DiscordMessage message, IPlayer player)
        {
            if (player != null)
            {
                message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Join.Errors.AlreadySignedUp, player, "/", Lang(CommandKeys.DiscordCommand, player), Lang(CommandKeys.DiscordLeaveCommand, player)));
                return;
            }

            LinkActivation existing = _activations.FirstOrDefault(a => a.Discord?.Id == message.Author.Id);
            if (existing != null)
            {
                message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Join.Errors.LinkInProgress));
                return;
            }

            string code = GenerateCode();
            _activations.Add(new LinkActivation
            {
                Discord = message.Author,
                Code = code
            });

            string linkMessage = GetDiscordFormattedMessage(LangKeys.Commands.Join.Messages.Discord.CompletedInGame, null, "/", Lang(CommandKeys.ChatCommand), "code", code);
            if (message.GuildId.HasValue)
            {
                message.Author.SendDirectMessage(_client, linkMessage);
                message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Join.Messages.Discord.CompleteInGameResponse));
            }
            else
            {
                message.Reply(_client, linkMessage);
            }
        }
        
        public void HandleDiscordCompleteLink(DiscordMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Join.Errors.InvalidSyntax, null, _cmdPrefix, Lang(CommandKeys.DiscordCommand), "code"));
                return;
            }

            LinkActivation act = _activations.FirstOrDefault(a => a.Code.Equals(args[1], StringComparison.OrdinalIgnoreCase));
            if (act == null)
            {
                message.Reply(_client, GetDiscordFormattedMessage(LangKeys.Commands.Join.Errors.NoPendingActivations, null, args[1]));
                return;
            }

            act.Discord = message.Author;
            
            CompletedLink(act);
        }
        #endregion
        
        #region Discord Hooks
        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            try
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
                
                if (_client.Bot.Application.Flags.HasValue && !_client.Bot.Application.Flags.Value.HasFlag(ApplicationFlags.GatewayGuildMembersLimited))
                {
                    PrintError($"You need to enable \"Server Members Intent\" for {_bot.Username} @ https://discord.com/developers/applications\n" +
                               "DiscordCore will not function correctly until that is fixed. Once updated please reload DiscordCore.");
                    return;
                }

                _bot = ready.User;
                _guildId = guild.Id;
                Puts($"Connected to bot: {_bot.Username}");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load DiscordCore: {ex}");
            }
        }

        [HookMethod(DiscordHooks.OnDiscordGuildMembersLoaded)]
        private void OnDiscordGuildMembersLoaded(DiscordGuild guild)
        {
            if (guild.Id != _guildId)
            {
                return;
            }
            
            try
            {
                _guild = guild;
                Puts($"Discord connected to server: {GetDiscordServerName()}");

                if (!_initialized)
                {
                    _initialized = true;
                }
                
                Puts($"Loaded {_guild.Members.Count} Discord Members");

                HandleLeaveRejoin();
                SetupGuildLinkMessage();

                foreach (Snowflake id in _pluginConfig.LinkSettings.AllowCommandInChannels)
                {
                    DiscordChannel channel = _guild.Channels[id];
                    if (channel != null)
                    {
                        _allowedCommandChannels.Add(channel.Id);
                        _allowedCommandChannelNames.Add(channel.Name);
                    }
                }
                
                RegisterDiscordLangCommand(nameof(DiscordCoreMessageCommand), CommandKeys.DiscordCommand, true, _pluginConfig.LinkSettings.AllowCommandsInGuild, _allowedCommandChannels);
                
                DiscordCoreReady(null);
                Puts("Discord Core Ready");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to connect to guild: {ex}");
            }
        }

        [HookMethod(DiscordHooks.OnDiscordGuildMemberAdded)]
        private void OnDiscordGuildMemberAdded(GuildMember member)
        {
            HandleRejoin(member.User);
            
            if (_pluginConfig.EnableJoinMessage)
            {
                member.User.SendDirectMessage(_client, Lang(LangKeys.Guild.WelcomeMessage, null, GetDiscordServerName(), server.Name, _cmdPrefix, Lang(CommandKeys.DiscordCommand),  Lang(CommandKeys.DiscordJoinCommand)));
            }
        }
        
        [HookMethod(DiscordHooks.OnDiscordGuildMemberRemoved)]
        private void OnDiscordGuildMemberRemoved(GuildMember member)
        {
            if (member?.User == null)
            {
                return;
            }

            IPlayer player = _link.GetPlayer(member.User.Id);
            if (player == null)
            {
                return;
            }

            HandleLeave(player, member.User, true);
        }
        #endregion

        #region Link Message Handling
        [HookMethod(DiscordHooks.OnDiscordInteractionCreated)]
        private void OnDiscordInteractionCreated(DiscordInteraction interaction)
        {
            if (interaction.Type != InteractionType.MessageComponent)
            {
                return;
            }
            
            if (!interaction.Data.ComponentType.HasValue || interaction.Data.ComponentType.Value != MessageComponentType.Button)
            {
                return;
            }

            string[] args = interaction.Data.CustomId.Split(' ');
            
            switch (args[0])
            {
                case LinkAccountsButtonId:
                    HandleLinkAccountsButton(interaction);
                    break;
                
                case AcceptLinkButtonId:
                    HandleAcceptLinkButton(interaction, args);
                    break;
                
                case DeclineLinkButtonId:
                    HandleDeclineLinkButton(interaction, args);
                    break;
            }
        }

        public void HandleLinkAccountsButton(DiscordInteraction interaction)
        {
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.DeferredUpdateMessage
            });
            
            IPlayer player = interaction.Member.User.Player;
            if (player != null)
            {
               interaction.CreateFollowUpMessage(_client, new CommandFollowupCreate
               {
                   Content = GetDiscordFormattedMessage(LangKeys.Commands.Join.Errors.AlreadySignedUp, player, _cmdPrefix, Lang(CommandKeys.DiscordCommand, player), Lang(CommandKeys.DiscordLeaveCommand, player)),
                   Flags = MessageFlags.Ephemeral
               });
               return;
            }
                    
            string code = GenerateCode();
            _activations.Add(new LinkActivation
            {
                Discord = interaction.Member.User,
                Code = code
            });
                    
            string linkMessage = GetDiscordFormattedMessage(LangKeys.Commands.Join.Messages.Discord.CompletedInGame, null, "/", Lang(CommandKeys.ChatCommand), "code", code);
            interaction.CreateFollowUpMessage(_client, new CommandFollowupCreate
            {
                Content = linkMessage,
                Flags = MessageFlags.Ephemeral
            });
        }
        
        private void HandleAcceptLinkButton(DiscordInteraction interaction, string[] args)
        {
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.DeferredUpdateMessage
            });
            
            Snowflake userId = (Snowflake) args[1];
            GuildMember member = _guild.Members[userId];
            if (member == null || (member.User.Bot ?? false))
            {
                return;
            }
            
            LinkActivation act = _activations.FirstOrDefault(a => a.Discord?.Id == userId);
            if (act != null)
            {
                CompletedLink(act);
            }
        }
        
        private void HandleDeclineLinkButton(DiscordInteraction interaction, string[] args)
        {
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.DeferredUpdateMessage
            });

            Snowflake userId = (Snowflake) args[1];
            GuildMember member = _guild.Members[userId];
            if (member == null || (member.User.Bot ?? false))
            {
                return;
            }
            
            LinkActivation act = _activations.FirstOrDefault(a => a.Discord?.Id == userId);
            if (act != null)
            {
                _activations.Remove(act);
                interaction.CreateFollowUpMessage(_client, new CommandFollowupCreate
                {
                    Content = GetDiscordFormattedMessage(LangKeys.Commands.Join.Messages.Discord.Declined)
                });
                Chat(act.Player, LangKeys.Commands.Join.Messages.Chat.Declined);
                _badLinkAttempts[act.Player.Id] += 1;
            }
        }


        public void SetupGuildLinkMessage()
        {
            DiscordLinkingSettings link = _pluginConfig.LinkSettings;
            LinkMessageSettings settings = link.LinkMessageSettings;
            if (!settings.Enabled)
            {
                return;
            }
            
            if (!settings.ChannelId.IsValid())
            {
                PrintWarning("Link message is enabled but link message channel ID is not valid");
                return;
            }

            DiscordChannel channel = _guild.Channels[settings.ChannelId];
            if (channel == null)
            {
                PrintWarning($"Link message failed to find channel with ID {settings.ChannelId}");
                return;
            }

            string content = Lang(LangKeys.Guild.LinkMessage, null, GetDiscordServerName(), "Link Accounts");
                
            if (_storedData.MessageData == null)
            {
                MessageCreate message = CreateGuildLinkMessage(content);
                channel.CreateMessage(_client, message, SaveGuildLinkMessageInfo);
            }
            else
            {
                channel.GetChannelMessage(_client, _storedData.MessageData.MessageId, message =>
                    {
                        message.Content = content;
                        message.Components.Clear();
                        message.Components.Add(CreateGuildLinkActions());
                        message.EditMessage(_client);
                    }, 
                    error =>
                    {
                        if (error.HttpStatusCode == 404)
                        {
                            PrintWarning("The previous link message has been removed. Recreating the message.");
                            MessageCreate message = CreateGuildLinkMessage(content);
                            channel.CreateMessage(_client, message, SaveGuildLinkMessageInfo);
                        }
                    });
            }
        }

        public MessageCreate CreateGuildLinkMessage(string content)
        {
            MessageCreate message = new MessageCreate
            {
                Content = content,
                Components = new List<ActionRowComponent> {CreateGuildLinkActions()}
            };

            return message;
        }

        public ActionRowComponent CreateGuildLinkActions()
        {
            ActionRowComponent row = new ActionRowComponent();
            row.AddComponent(new ButtonComponent
            {
                Label = Lang(LangKeys.Commands.Join.Messages.Discord.LinkAccounts),
                CustomId = LinkAccountsButtonId,
                Emoji = DiscordEmoji.FromCharacter(Lang(LangKeys.Emoji.Accept)),
                Style = ButtonStyle.Success
            });
            return row;
        }

        public void SaveGuildLinkMessageInfo(DiscordMessage message)
        {
            _storedData.MessageData = new LinkMessageData
            {
                ChannelId = message.ChannelId,
                MessageId = message.Id
            };
            
            SaveData();
        }
        #endregion

        #region Linking
        public void CompletedLink(LinkActivation activation)
        {
            IPlayer player = activation.Player;
            DiscordUser user = activation.Discord;

            _storedData.PlayerDiscordInfo[player.Id] = new DiscordInfo
            {
                PlayerId = player.Id,
                DiscordId = user.Id
            };

            _activations.Remove(activation);

            Chat(player, LangKeys.Linking.Chat.Linked, user.Username, user.Discriminator);
            activation.Discord.SendDirectMessage(_client, GetDiscordFormattedMessage(LangKeys.Linking.Discord.Linked, player, user.Username, user.Discriminator, player.Name));

            Snowflake channelId = _pluginConfig.LinkSettings.AnnouncementChannel;
            if (channelId.IsValid())
            {
                DiscordChannel channel = _guild.Channels[channelId];
                channel.CreateMessage(_client, Lang(LangKeys.Notifications.Link, null, player.Name, player.Id, user.Username, user.Id));
            }
            
            _onLinked.Invoke(activation.Player, activation.Discord);
            SaveData();
        }
        
        public void HandleLeave(IPlayer player, DiscordUser user, bool backup)
        {
            if (backup)
            {
                _storedData.LeftPlayerInfo[user.Id] = _storedData.PlayerDiscordInfo[player.Id];
            }
            
            _storedData.PlayerDiscordInfo.Remove(player.Id);

            if (!backup)
            {
                Chat(player, LangKeys.Linking.Chat.Unlinked, player.Name);
                user.SendDirectMessage(_client,  GetDiscordFormattedMessage(LangKeys.Linking.Discord.Unlinked, player, user.Username, user.Discriminator, player.Name));
            }

            _onUnlinked.Invoke(player, user);
            
            Snowflake channelId = _pluginConfig.LinkSettings.AnnouncementChannel;
            if (channelId.IsValid())
            {
                DiscordChannel channel = _guild.Channels[channelId];
                channel.CreateMessage(_client, Lang(LangKeys.Notifications.Unlink, null, player.Name, player.Id, user.Username, user.Id));
            }

            SaveData();
        }

        public void HandleRejoin(DiscordUser user)
        {
            DiscordInfo existing = _storedData.LeftPlayerInfo[user.Id];
            if (existing == null)
            {
                return;
            }

            _storedData.PlayerDiscordInfo[existing.PlayerId] = existing;
            _storedData.LeftPlayerInfo.Remove(user.Id);

            IPlayer player = players.FindPlayerById(existing.PlayerId);
            
            _onLinked.Invoke(player, user);
            
            Snowflake channelId = _pluginConfig.LinkSettings.AnnouncementChannel;
            if (channelId.IsValid())
            {
                DiscordChannel channel = _guild.Channels[channelId];
                channel.CreateMessage(_client, Lang(LangKeys.Notifications.Rejoin, null, player.Name, player.Id, user.Username, user.Id));
            }
            
            SaveData();
        }

        public void HandleLeaveRejoin()
        {
            foreach (DiscordInfo info in _storedData.PlayerDiscordInfo.Values.ToList())
            {
                if (!_guild.Members.ContainsKey(info.DiscordId))
                {
                    IPlayer player = players.FindPlayerById(info.PlayerId);
                    if (player != null)
                    {
                        DiscordUser user = player.GetDiscordUser();
                        HandleLeave(player, user, true);
                        Puts($"Player {player.Name}({player.Id}) Discord {user.Id} is no longer in the guild and has been unlinked.");
                    }
                }
            }

            foreach (DiscordInfo info in _storedData.LeftPlayerInfo.Values.ToList())
            {
                GuildMember member = _guild.Members[info.DiscordId];
                if (member != null)
                {
                    HandleRejoin(member.User);
                }
            }
        }
        #endregion

        #region Discord Link
        public Hash<string, Snowflake> GetSteamToDiscordIds()
        {
            Hash<string, Snowflake> data = new Hash<string, Snowflake>();
            foreach (DiscordInfo info in _storedData.PlayerDiscordInfo.Values)
            {
                data[info.PlayerId] = info.DiscordId;
            }

            return data;
        }

        public void RegisterEvents(Action<IPlayer, DiscordUser> onLinked, Action<IPlayer, DiscordUser> onUnlinked)
        {
            _onLinked = onLinked;
            _onUnlinked = onUnlinked;
        }
        #endregion
        
        #region API
        [HookMethod(nameof(GetDiscordServerName))]
        public string GetDiscordServerName()
        {
            if (!string.IsNullOrEmpty(_pluginConfig.ServerNameOverride))
            {
                return _pluginConfig.ServerNameOverride;
            }

            return _guild?.Name ?? "Not Connected";
        }
        #endregion

        #region Discord Helpers
        public string GetDiscordFormattedMessage(string key, IPlayer player = null, params object[] args)
        {
            return Formatter.ToPlaintext(Lang(LangKeys.DiscordFormat, player, Lang(key, player, args)));
        }

        private bool IsDiscordCoreOnline() => _initialized && _guild != null;
        #endregion
        
        #region Helper Methods
        public string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _pluginConfig.LinkSettings.LinkCodeLength; i++)
            {
                sb.Append(_linkChars[Random.Range(0, _pluginConfig.LinkSettings.LinkCodeLength)]);
            }

            return sb.ToString();
        }

        public void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);

        public void Chat(IPlayer player, string key, params object[] args)
        {
            if (player.IsConnected)
            {
                player.Reply(Lang(LangKeys.ChatFormat, player, Lang(key, player, args)));
            }
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

        public void RegisterChatLangCommand(string command, string langKey)
        {
            foreach (string langType in lang.GetLanguages(this))
            {
                Dictionary<string, string> langKeys = lang.GetMessages(langType, this);
                string commandValue;
                if (langKeys.TryGetValue(langKey, out commandValue) && !string.IsNullOrEmpty(commandValue))
                {
                    AddCovalenceCommand(commandValue, command);
                }
            }
        }

        /// <summary>
        /// Registers commands with discord using lang keys
        /// </summary>
        /// <param name="command">Name of the method to use in callback</param>
        /// <param name="langKey">The name of the lang key dictionary</param>
        /// <param name="direct">Should we register this command for direct messages</param>
        /// <param name="guild">Should we register this command for guilds</param>
        /// <param name="allowedChannels">If registering guilds the allowed channels / categories this command can be used in</param>
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
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string ApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Discord Server ID (Optional if bot only in 1 guild)")]
            public Snowflake GuildId { get; set; }

            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Server Name Override")]
            public string ServerNameOverride { get; set; }
            
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Server Join Code")]
            public string JoinCode { get; set; }

            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enable Discord Server Welcome DM Message")]
            public bool EnableJoinMessage { get; set; }
            
            [JsonProperty(PropertyName = "Link Settings")]
            public DiscordLinkingSettings LinkSettings { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(LogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public LogLevel ExtensionDebugging { get; set; }
        }

        public class DiscordLinkingSettings
        {
            [JsonProperty(PropertyName = "Link Code Generator Characters")]
            public string LinkCodeCharacters { get; set; }

            [JsonProperty(PropertyName = "Link Code Length")]
            public int LinkCodeLength { get; set; }
            
            [JsonProperty(PropertyName = "Allow Commands To Be Used In Guild Channels")]
            public bool AllowCommandsInGuild { get; set; }
            
            [JsonProperty(PropertyName = "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)")]
            public List<Snowflake> AllowCommandInChannels { get; set; }

            [JsonProperty(PropertyName = "Link / Unlink Announcement Channel Id")]
            public Snowflake AnnouncementChannel { get; set; }
            
            [JsonProperty(PropertyName = "Guild Link Message Settings")]
            public LinkMessageSettings LinkMessageSettings { get; set; }

            public DiscordLinkingSettings(DiscordLinkingSettings settings)
            {
                LinkCodeCharacters = settings?.LinkCodeCharacters ?? "123456789";
                LinkCodeLength = settings?.LinkCodeLength ?? 6;
                AllowCommandsInGuild = settings?.AllowCommandsInGuild ?? false;
                AllowCommandInChannels = settings?.AllowCommandInChannels ?? new List<Snowflake>();
                AnnouncementChannel = settings?.AnnouncementChannel ?? default(Snowflake);
                LinkMessageSettings = new LinkMessageSettings(settings?.LinkMessageSettings);
            }
        }

        public class LinkMessageSettings
        {
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Enable Guild Link Message")]
            public bool Enabled { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Message Channel ID")]
            public Snowflake ChannelId { get; set; }

            public LinkMessageSettings(LinkMessageSettings settings)
            {
                Enabled = settings?.Enabled ?? false;
                ChannelId = settings?.ChannelId ?? default(Snowflake);
            }
        }

        public class StoredData
        {
            public Hash<string, DiscordInfo> PlayerDiscordInfo = new Hash<string, DiscordInfo>();
            public Hash<Snowflake, DiscordInfo> LeftPlayerInfo = new Hash<Snowflake, DiscordInfo>();
            public LinkMessageData MessageData;
        }

        public class DiscordInfo
        {
            public Snowflake DiscordId { get; set; }
            public string PlayerId { get; set; }
        }

        public class LinkActivation
        {
            public IPlayer Player { get; set; }
            public DiscordUser Discord { get; set; }
            public string Code { get; set; }
            public Snowflake Channel { get; set; }
        }

        public class LinkMessageData
        {
            public Snowflake ChannelId { get; set; }
            public Snowflake MessageId { get; set; }
        }
        
        public class JoinSearchRequest
        {
            public IPlayer Player { get; set; }
            public string Username { get; set; }
        }

        public static class LangKeys
        {
            public const string NoPermission = nameof(NoPermission);
            public const string ChatFormat = nameof(ChatFormat);
            public const string DiscordFormat = nameof(DiscordFormat);
            public const string DiscordCoreOffline = nameof(DiscordCoreOffline);
            public const string GenericError = nameof(GenericError);
            public const string ConsolePlayerNotSupported = nameof(ConsolePlayerNotSupported);
            
            public static class Commands
            {
                private const string Base = nameof(Commands) + ".";
                
                public const string Unknown = Base + nameof(Unknown);
                
                public const string ChatHelpText = nameof(ChatHelpText);
                public const string DiscordHelpText = nameof(DiscordHelpText);

                public static class Leave
                {
                    private const string Base = Commands.Base + nameof(Leave);

                    public static class Errors
                    {
                        private const string Base = Leave.Base + nameof(Errors);

                        public const string NotLinked = Base + nameof(NotLinked);
                    }
                }

                public static class Join
                {
                    private const string Base = Commands.Base + nameof(Join) + ".";
                    
                    public const string Modes = Base + nameof(Modes);

                    public static class Messages
                    {
                        private const string Base = Join.Base  + nameof(Messages) + ".";

                        public static class Discord
                        {
                            private const string Base = Messages.Base + nameof(Discord) + ".";
                            
                            public const string Username = Base + nameof(Username);
                            public const string CompletedInGame = Base + nameof(CompletedInGame);
                            public const string CompleteInGameResponse = Base + nameof(CompleteInGameResponse);
                            public const string Declined = Base + nameof(Declined);
                            public const string Accept = Base + nameof(Accept);
                            public const string Decline = Base + nameof(Decline);
                            public const string LinkAccounts = Base + nameof(LinkAccounts);
                        }
                        
                        public static class Chat
                        {
                            private const string Base = Messages.Base + nameof(Chat) + ".";
                            
                            public const string UsernameDmSent = Base + nameof(UsernameDmSent);
                            public const string Declined = Base + nameof(Declined);
                        }
                    }
                    
                    public static class Complete
                    {
                        private const string Base = Join.Base + nameof(Complete) + ".";
                        
                        public const string Info = Base + nameof(Info);
                        public const string InfoGuildAny = Base + nameof(InfoGuildAny);
                        public const string InfoGuildChannel = Base + nameof(InfoGuildChannel);
                        public const string InfoAlsoDm = Base + nameof(InfoAlsoDm);
                        public const string InfoDmOnly = Base + nameof(InfoDmOnly);
                    }

                    public static class Errors
                    {
                        private const string Base = Join.Base + nameof(Errors) + ".";
                        
                        public const string AlreadySignedUp = Base + nameof(AlreadySignedUp);
                        public const string UnableToFindUser = Base + nameof(UnableToFindUser);
                        public const string FoundMultipleUsers = Base + nameof(FoundMultipleUsers);
                        public const string UsernameSearchError = Base + nameof(UsernameSearchError);
                        public const string InvalidSyntax = Base + nameof(InvalidSyntax);
                        public const string NoPendingActivations = Base + nameof(NoPendingActivations);
                        public const string LinkInProgress = Base + nameof(LinkInProgress);
                        public const string Banned = Base + nameof(Banned);
                    }
                }
            }

            public static class Linking
            {
                private const string Base = nameof(Linking) + ".";

                public static class Chat
                {
                    private const string Base = Linking.Base + nameof(Chat) + ".";
                    
                    public const string Linked = Base + nameof(Linked);
                    public const string Unlinked = Base + nameof(Unlinked);
                }
                
                public static class Discord
                {
                    private const string Base = Linking.Base + nameof(Discord) + ".";
                    
                    public const string Linked = Base + nameof(Linked);
                    public const string Unlinked = Base + nameof(Unlinked);
                }
            }

            public static class Guild
            {
                private const string Base = nameof(Guild) + ".";
                
                public const string WelcomeMessage = Base + nameof(WelcomeMessage);
                public const string LinkMessage = Base + nameof(LinkMessage);
            }

            public static class Notifications
            {
                private const string Base = nameof(Notifications) + ".";
                
                public const string Link = Base + nameof(Link);
                public const string Rejoin = Base + nameof(Rejoin);
                public const string Unlink = Base + nameof(Unlink);
            }
            
            public static class Emoji
            {
                private const string Base = nameof(Emoji) + ".";
                        
                public const string Accept = Base + nameof(Accept);
                public const string Decline = Base +  nameof(Decline);
            }
        }

        public static class CommandKeys
        {
            public const string ChatCommand = nameof(DiscordCoreChatCommand);
            public const string ChatJoinCommand = ChatCommand + ".Join";
            public const string ChatJoinCodeCommand = ChatJoinCommand + ".Code";
            public const string ChatLeaveCommand = ChatCommand + ".Leave";
            
            public const string DiscordCommand = nameof(DiscordCoreMessageCommand);
            public const string DiscordJoinCommand = DiscordCommand + ".Join";
            public const string DiscordLeaveCommand = DiscordCommand + ".Leave";
        }
        #endregion
    }
}
