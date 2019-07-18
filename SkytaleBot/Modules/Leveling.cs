using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Leveling : ModuleBase
    {
        [Command("Profile")]
        public async Task Profile(string args = null)
        {
            IUser user = null;
            if (args != null)
                user = await DiscordUtils.Utils.GetUser(args, Context.Guild);
            if (user == null)
                user = Context.User;
            await ReplyAsync(user.ToString() + "'s profile:\nXP: " + Program.P.BotDb.GetXp(user.Id));
        }
    }
}
