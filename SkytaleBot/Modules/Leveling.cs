using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Leveling : ModuleBase
    {
        public static int GetLevelFromXp(int xp)
            => (int)Math.Sqrt(xp / 10);

        public static int GetXpFromLevel(int level)
            => level * level * 10;

        [Command("Profile")]
        public async Task Profile(string args = null)
        {
            IUser user = null;
            if (args != null)
                user = await DiscordUtils.Utils.GetUser(args, Context.Guild);
            if (user == null)
                user = Context.User;
            int xp = await Program.P.BotDb.GetXp(user.Id);
            int level = GetLevelFromXp(xp);
            await ReplyAsync("", false, new EmbedBuilder
            {
                Title = user.ToString(),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Level",
                        Value = level
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "XP",
                        Value = xp + " / " + GetXpFromLevel(level + 1)
                    }
                }
            }.Build());
        }
    }
}
