using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtils;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Newtonsoft.Json;
using System;
using System.IO;
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
                    await SendReport(await Features.Moderation.MessageCheck.CheckMessageText(TClient.TranslateText(msg.Content, "en").TranslatedText), ((ITextChannel)msg.Channel).Guild, msg.Author, msg.Content);
            }
            else
                await SendReport(await Features.Moderation.MessageCheck.CheckMessageText(TClient.TranslateText(msg.Content, "en").TranslatedText), ((ITextChannel)msg.Channel).Guild, msg.Author, msg.Content);
        }

        private async Task SendReport(Features.Moderation.MessageCheck.MessageError? res, IGuild guild, IUser author, string message)
        {
            if (res == null)
                return;
            string id = await BotDb.GetReportChanId(guild.Id.ToString());
            if (id == "None")
                return;
            ITextChannel chan = await guild.GetTextChannelAsync(ulong.Parse(id));
            if (chan == null)
                return;
            await chan.SendMessageAsync("", false, new EmbedBuilder
            {
                Title = "A message from " + author.ToString() + " was reported",
                Description = message,
                Color = Color.Red,
                Footer = new EmbedFooterBuilder()
                {
                    Text = "This message triggered the flag " + res.Value.flag + ", breaking the limit of " + res.Value.maxValue.ToString("0.00") + " with a score of " + res.Value.currValue.ToString("0.00") + "."
                }
            }.Build());
        }
    }
}
