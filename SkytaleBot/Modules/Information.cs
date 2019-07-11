using Discord;
using Discord.Commands;
using DiscordUtils;
using System.Collections.Generic;
using System.Linq;
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
            dynamic json = await Program.P.BotDb.GetGuild(Context.Guild.Id);
            await Context.User.SendMessageAsync("", false, new EmbedBuilder
            {
                Title = "Status",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Admin roles",
                        Value = json.AdminRoles == "None" ?
                            "None" :
                            string.Join(", ", ((string[])json.AdminRoles.ToString().Split('|')).Select(x =>
                            {
                                IRole role = Context.Guild.GetRole(ulong.Parse(x));
                                if (role == null)
                                    return "Unknown (" + x + ")";
                                return role.Name + " (" + x + ")";
                            }))
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Staff roles",
                        Value = json.StaffRoles == "None" ?
                            "None" :
                            string.Join(", ", ((string[])json.StaffRoles.ToString().Split('|')).Select(x =>
                            {
                                IRole role = Context.Guild.GetRole(ulong.Parse(x));
                                if (role == null)
                                    return "Unknown (" + x + ")";
                                return role.ToString() + " (" + x + ")";
                            }))
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Report channel",
                        Value = json.Report == "None" ? // TODO
                            "None" : () => {
                                ITextChannel chan = await Context.Guild.GetTextChannelAsync(ulong.Parse(x));
                                return chan == null ? "Unknown (" + x + ")" : chan.ToString() + " (" + x + ")"; }))
                    }
                }
            }.Build());
            await Program.P.BotDb.GetGuild(Context.Guild.Id);
        }
    }
}
