using Discord;
using Discord.Commands;
using DiscordUtils;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Information : ModuleBase
    {
        [Command("Info")]
        public async Task Info()
        {
            await ReplyAsync("", false, Utils.GetBotInfo(Program.P.StartTime, "SkytaleBot", Program.P.client.CurrentUser));
        }

        [Command("Status")]
        public async Task Status()
        {
            if (!await Settings.IsStaff((IGuildUser)Context.User))
            {
                await ReplyAsync("Only a staff can do this command.");
                return;
            }
            await Context.User.SendMessageAsync("My status:");
            await Program.P.BotDb.GetGuild(Context.Guild.Id);
        }
    }
}
