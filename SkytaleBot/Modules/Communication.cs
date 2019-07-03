using Discord;
using Discord.Commands;
using DiscordUtils;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Communication : ModuleBase
    {
        [Command("Info")]
        public async Task Info()
        {
            await ReplyAsync("", false, Utils.GetBotInfo(Program.P.StartTime, "SkytaleBot", Program.P.client.CurrentUser));
        }

        [Command("GDPR"), Summary("Show infos the bot have about the user and the guild")]
        public async Task GDPR(params string[] command)
        {
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "Data saved about " + Context.Guild.Name,
                Description = await Program.P.BotDb.GetGuild(Context.Guild.Id)
            }.Build());
        }
    }
}
