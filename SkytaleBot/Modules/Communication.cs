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
    }
}
