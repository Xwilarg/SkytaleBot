using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtils;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkytaleBot
{
    class Program
    {
        public static async Task Main()
            => await new Program().MainAsync();

        public readonly DiscordSocketClient client;
        private readonly CommandService commands = new CommandService();

        public static Program P { private set; get; }
        public DateTime StartTime { private set; get; }
        public Random Rand { private set; get; }
        public string PerspectiveApi { private set; get; }
        public Db.Db BotDb { private set; get; }
        public TranslationClient TClient { private set; get; }

        private Tuple<string, string> websiteCredentials;

        private Program()
        {
            P = this;
            Rand = new Random();
            BotDb = new Db.Db();
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
            });
            client.Log += Utils.Log;
            commands.Log += Utils.LogError;
        }

        private async Task MainAsync()
        {
            await BotDb.InitAsync("SkytaleBot");

            client.MessageReceived += HandleCommandAsync;
            client.Connected += Connected;
            client.GuildAvailable += GuildUpdate;
            client.JoinedGuild += GuildUpdate;
            client.ReactionAdded += ReactionAdded;

            await commands.AddModuleAsync<Modules.Settings>(null);
            await commands.AddModuleAsync<Modules.Information>(null);
            await commands.AddModuleAsync<Modules.Moderation>(null);

            if (!File.Exists("Keys/Credentials.json"))
                throw new FileNotFoundException("You must have a Credentials.json located in a 'Keys' folder near your executable.\nIt must contains a KeyValue botToken containing the token of your bot");
            dynamic json = JsonConvert.DeserializeObject(File.ReadAllText("Keys/Credentials.json"));
            if (json.botToken == null)
                throw new FileNotFoundException("You must have a Credentials.json located in a 'Keys' folder near your executable.\nIt must contains a KeyValue botToken containing the token of your bot");
            if (json.websiteUrl != null && json.websiteToken != null)
                websiteCredentials = new Tuple<string, string>((string)json.websiteUrl, (string)json.websiteToken);
            else
                websiteCredentials = null;
            PerspectiveApi = json.perspectiveToken;
            if (json.googleAPIJson != null)
                TClient = TranslationClient.Create(GoogleCredential.FromFile((string)json.googleAPIJson));
            else
                TClient = null;

            await client.LoginAsync(TokenType.Bot, (string)json.botToken);
            StartTime = DateTime.Now;
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachMsg, ISocketMessageChannel chan, SocketReaction reaction)
        {
            try
            {
                if (await Modules.Settings.IsStaff((IGuildUser)reaction.User.Value))
                {
                    var msg = await cachMsg.GetOrDownloadAsync();
                    if (msg.Embeds.Count == 1 && msg.Author.Id == client.CurrentUser.Id)
                    {
                        var embed = msg.Embeds.ToArray()[0];
                        if (embed.Footer.Value.Text.StartsWith("Bot Technical Information:"))
                        {
                            dynamic json = JsonConvert.DeserializeObject(string.Join(":", embed.Footer.Value.Text.Split(':').Skip(1)));
                            IGuild guild = ((ITextChannel)chan).Guild;
                            IGuildUser reportedUser = await guild.GetUserAsync((ulong)json.UserId);
                            if (reaction.Emote.Name == "♻")
                            {
                                await msg.RemoveAllReactionsAsync();
                                await msg.ModifyAsync(x => x.Embed = new EmbedBuilder
                                {
                                    Title = "Message from " + reportedUser.ToString() + " (" + reportedUser.Id + ") was deleted",
                                    Color = Color.Red,
                                    Fields = embed.Fields.Select(y => new EmbedFieldBuilder()
                                    {
                                        IsInline = y.Inline,
                                        Name = y.Name,
                                        Value = y.Value
                                    }).ToList()
                                }.Build());
                                var deleteMsg = await (await guild.GetTextChannelAsync((ulong)json.ChannelId)).GetMessageAsync((ulong)json.MessageId);
                                if (deleteMsg != null) await deleteMsg.DeleteAsync();
                            }
                            if (reaction.Emote.Name == "⚠")
                            {
                                if (reportedUser == null)
                                    await reaction.User.Value.SendMessageAsync("I wasn't able to warn user " + reportedUser.ToString() + " because he is no longer in the guild.");
                                else
                                {
                                    await reportedUser.SendMessageAsync("One of your message was reported for the following reason: " + (string)json.Flag);
                                    await msg.RemoveAllReactionsAsync();
                                    await msg.ModifyAsync(x => x.Embed = new EmbedBuilder
                                    {
                                        Title = reportedUser.ToString() + " (" + reportedUser.Id + ") got a warning",
                                        Color = Color.Red,
                                        Fields = embed.Fields.Select(y => new EmbedFieldBuilder()
                                        {
                                            IsInline = y.Inline,
                                            Name = y.Name,
                                            Value = y.Value
                                        }).ToList()
                                    }.Build());
                                    var deleteMsg = await (await guild.GetTextChannelAsync((ulong)json.ChannelId)).GetMessageAsync((ulong)json.MessageId);
                                    if (deleteMsg != null) await deleteMsg.DeleteAsync();
                                }
                            }
                            else if (reaction.Emote.Name == "👢")
                            {
                                if (reportedUser == null)
                                    await reaction.User.Value.SendMessageAsync("I wasn't able to kick user " + reportedUser.ToString() + " because he is no longer in the guild.");
                                else if (await Features.Moderation.KickBanCheck.CanKickAsync(guild) && Features.Moderation.KickBanCheck.HaveHighestRole((IGuildUser)reaction.User.Value, reportedUser))
                                {
                                    await reportedUser.KickAsync(embed.Footer.Value.Text + Environment.NewLine + embed.Description);
                                    await msg.RemoveAllReactionsAsync();
                                    await msg.ModifyAsync(x => x.Embed = new EmbedBuilder
                                    {
                                        Title = reportedUser.ToString() + " (" + reportedUser.Id + ") was kicked",
                                        Color = Color.Red,
                                        Fields = embed.Fields.Select(y => new EmbedFieldBuilder()
                                        {
                                            IsInline = y.Inline,
                                            Name = y.Name,
                                            Value = y.Value
                                        }).ToList()
                                    }.Build());
                                    var deleteMsg = await (await guild.GetTextChannelAsync((ulong)json.ChannelId)).GetMessageAsync((ulong)json.MessageId);
                                    if (deleteMsg != null) await deleteMsg.DeleteAsync();
                                }
                                else
                                    await reaction.User.Value.SendMessageAsync("I wasn't able to kick user " + reportedUser.ToString() + ", I'm lacking the proper authorisations.");
                            }
                            else if (reaction.Emote.Name == "🔨")
                            {
                                if (reportedUser == null)
                                    await reaction.User.Value.SendMessageAsync("I wasn't able to ban user " + reportedUser.ToString() + " because he is no longer in the guild.");
                                else
                                {
                                    await reportedUser.BanAsync(0, embed.Footer.Value.Text + Environment.NewLine + embed.Description);
                                    await msg.RemoveAllReactionsAsync();
                                    await msg.ModifyAsync(x => x.Embed = new EmbedBuilder
                                    {
                                        Title = reportedUser.ToString() + " (" + reportedUser.Id + ") was banned",
                                        Color = Color.Red,
                                        Fields = embed.Fields.Select(y => new EmbedFieldBuilder()
                                        {
                                            IsInline = y.Inline,
                                            Name = y.Name,
                                            Value = y.Value
                                        }).ToList()
                                    }.Build());
                                    var deleteMsg = await (await guild.GetTextChannelAsync((ulong)json.ChannelId)).GetMessageAsync((ulong)json.MessageId);
                                    if (deleteMsg != null) await deleteMsg.DeleteAsync();
                                }
                            }
                            else if (reaction.Emote.Name == "❌")
                            {
                                await msg.DeleteAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await reaction.User.Value.SendMessageAsync("An internal error happened while doing moderation using emotes." + Environment.NewLine +
                    "Here are the technical information: " + e.ToString());
            }
        }

        private async Task GuildUpdate(SocketGuild arg)
        {
            await BotDb.InitGuild(arg.Id.ToString());
        }

        private async Task Connected()
        {
            if (websiteCredentials != null)
            {
                var task = Task.Run(async () =>
                {
                    for (;;)
                    {
                        await Task.Delay(60000);
                        if (client.ConnectionState == ConnectionState.Connected)
                            await Utils.WebsiteUpdate("SkytaleBot", websiteCredentials.Item1, websiteCredentials.Item2, "serverCount", client.Guilds.Count.ToString());
                    }
                });
            }

        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage msg = arg as SocketUserMessage;
            if (msg == null || arg.Author.IsBot) return;
            int pos = 0;
            if (msg.HasMentionPrefix(client.CurrentUser, ref pos) || msg.HasStringPrefix("s.", ref pos))
            {
                if (arg.Channel as ITextChannel == null)
                {
                    await arg.Channel.SendMessageAsync("I can't answer to commands in private message.");
                    return;
                }
                SocketCommandContext context = new SocketCommandContext(client, msg);
                IResult res = await commands.ExecuteAsync(context, pos, null);
                if (res.IsSuccess && websiteCredentials != null)
                    await Utils.WebsiteUpdate("SkytaleBot", websiteCredentials.Item1, websiteCredentials.Item2, "nbMsgs", "1");
                else if (!res.IsSuccess)
                    await SendReport(await Features.Moderation.MessageCheck.CheckMessageText(TClient.TranslateText(msg.Content, "en").TranslatedText), ((ITextChannel)msg.Channel).Guild, msg.Author, msg.Content, msg.Id, msg.Channel.Id);
            }
            else
                await SendReport(await Features.Moderation.MessageCheck.CheckMessageText(TClient.TranslateText(msg.Content, "en").TranslatedText), ((ITextChannel)msg.Channel).Guild, msg.Author, msg.Content, msg.Id, msg.Channel.Id);
        }

        private async Task SendReport(Features.Moderation.MessageCheck.MessageError? res, IGuild guild, IUser author, string message, ulong msgId, ulong chanId)
        {
            if (res == null)
                return;
            string id = await BotDb.GetReportChanId(guild.Id.ToString());
            if (id == "None")
                return;
            ITextChannel chan = await guild.GetTextChannelAsync(ulong.Parse(id));
            if (chan == null)
                return;
            var msg = await chan.SendMessageAsync("", false, new EmbedBuilder
            {
                Title = "A message from " + author.ToString() + " (" + author.Id + ") was reported",
                Description =
                    "♻: Delete message" + Environment.NewLine +
                    "⚠: Delete message and warn user" + Environment.NewLine +
                    "👢: Delete message and kick user" + Environment.NewLine +
                    "🔨: Delete message and ban user" + Environment.NewLine +
                    "❌: Delete report" + Environment.NewLine,
                Color = Color.Red,
                Fields = new List<EmbedFieldBuilder>()
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Flag triggered",
                        Value = res.Value.flag
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Flag value",
                        Value = res.Value.currValue.ToString("0.00") + " / " + res.Value.maxValue.ToString("0.00")
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Message",
                        Value = message
                    }
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Bot Technical Information: {\"UserId\":" + author.Id + ", \"MessageId\":" + msgId + ", \"ChannelId\":" + chanId + ", \"Flag\":\"" + res.Value.flag + "\"}"
                }
            }.Build());
            await msg.AddReactionsAsync(new IEmote[] { new Emoji("♻"), new Emoji("⚠"), new Emoji("👢"), new Emoji("🔨"), new Emoji("❌") });
        }
    }
}
