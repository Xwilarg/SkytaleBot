using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Leveling : ModuleBase
    {
        public static readonly int xpPerLevel = 20;

        [Command("Profile")]
        public async Task Profile(string args = null)
        {
            IUser user = null;
            if (args != null)
                user = await DiscordUtils.Utils.GetUser(args, Context.Guild);
            if (user == null)
                user = Context.User;
            int xp = await Program.P.BotDb.GetXp(user.Id);
            int level = xp / 20;
            await ReplyAsync("", false, new EmbedBuilder
            {
                Title = user.ToString(),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Level",
                        Value = xp / 20
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "XP",
                        Value = xp + " / " + ((level + 1) * 20)
                    }
                }
            }.Build());
        }
    }
}
