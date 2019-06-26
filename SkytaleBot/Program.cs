using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtils;
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

        public DateTime StartTime { private set; get; }
        public static Program P { private set; get; }
        public static Random Rand { private set; get; }

        private Tuple<string, string> websiteCredentials;

        private Program()
        {
            P = this;
            Rand = new Random();
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
            });
            client.Log += Utils.Log;
            commands.Log += Utils.LogError;
        }

        private async Task MainAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            client.Connected += Connected;

            await commands.AddModuleAsync<Modules.Communication>(null);

            if (!File.Exists("Keys/Credentials.json"))
                throw new FileNotFoundException("You must have a Credentials.json located in a 'Keys' folder near your executable.\nIt must contains a KeyValue botToken containing the token of your bot");
            dynamic json = JsonConvert.DeserializeObject(File.ReadAllText("Keys/Credentials.json"));
            if (json.botToken == null)
                throw new FileNotFoundException("You must have a Credentials.json located in a 'Keys' folder near your executable.\nIt must contains a KeyValue botToken containing the token of your bot");
            if (json.websiteUrl != null && json.websiteToken != null)
                websiteCredentials = new Tuple<string, string>((string)json.websiteUrl, (string)json.websiteToken);
            else
                websiteCredentials = null;
            await client.LoginAsync(TokenType.Bot, (string)json.botToken);
            StartTime = DateTime.Now;
            await client.StartAsync();

            await Task.Delay(-1);
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
                SocketCommandContext context = new SocketCommandContext(client, msg);
                if ((await commands.ExecuteAsync(context, pos, null)).IsSuccess && websiteCredentials != null)
                {
                    await Utils.WebsiteUpdate("SkytaleBot", websiteCredentials.Item1, websiteCredentials.Item2, "nbMsgs", "1");
                }
            }
        }
    }
}
