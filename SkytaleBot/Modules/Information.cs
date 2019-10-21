using Discord;
using Discord.Commands;
using DiscordUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Information : ModuleBase
    {
        [Command("Help")]
        public async Task Help()
        {
            await ReplyAsync("", false, new EmbedBuilder
            {
                Title = "Help",
                Color = Color.Blue,
                Description =
                "Profile [(optional) user]: See a user profile" + Environment.NewLine +
                "Daily: Get your daily money" + Environment.NewLine +
                "Give [user] [amount]: Give money to another user" + Environment.NewLine +
                "Slot: Play the slot game (cost 5)"
            }.Build());
        }

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
                Color = Color.Blue,
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
                        Name = "Roles",
                        Value = await Program.P.BotDb.GetAllRolesLevel(Context.Guild.Id)
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Report channel",
                        Value = await GetReportChanValue((string)json.Report)
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Moderation",
                        Value =
                            "Kick: " + (await Features.Moderation.KickBanCheck.CanKickAsync(Context.Guild) ? "Yes" : "No") + Environment.NewLine +
                            "Ban: " + (await Features.Moderation.KickBanCheck.CanBanAsync(Context.Guild) ? "Yes" : "No") + Environment.NewLine +
                            "Highest position: " + Features.Moderation.KickBanCheck.HighestRole(await Context.Guild.GetCurrentUserAsync())
                    }
                }
            }.Build());
            await ReplyAsync("Please check your PM.");
        }

        private async Task<string> GetReportChanValue(string input)
        {
            if (input == "None")
                return "None";
            ITextChannel chan = await Context.Guild.GetTextChannelAsync(ulong.Parse(input));
            return chan == null ? "Unknown (" + input + ")" : chan.ToString() + " (" + input + ")";
        }
    }
}
