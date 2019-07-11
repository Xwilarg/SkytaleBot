using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    [Group("Set")]
    public class Settings : ModuleBase
    {
        [Command("Report")]
        public async Task Report(string msgId = null)
        {
            if (!await IsAdmin((IGuildUser)Context.User))
            {
                await ReplyAsync("Only an admin can do this command.");
                return;
            }
            if (msgId == null)
            {
                await ReplyAsync("You must provide a valid text channel or 'none'.");
                return;
            }
            ITextChannel chan = null;
            if (msgId.ToLower() != "none")
            {
                chan = await DiscordUtils.Utils.GetTextChannel(msgId, Context.Guild);
                if (chan == null)
                {
                    await ReplyAsync("You must provide a valid text channel or 'none'.");
                    return;
                }
            }
            await Program.P.BotDb.UpdateGuildReport(Context.Guild.Id.ToString(), chan == null ? "None" : chan.Id.ToString());
            await ReplyAsync("Your report preferences were updated.");
        }

        [Command("Staff")]
        public async Task Staff(params string[] args)
        {
            string roles = await GetRolesId((IGuildUser)Context.User, (ITextChannel)Context.Channel, args);
            if (roles != null)
            {
                await Program.P.BotDb.UpdateGuildStaffRoles(Context.Guild.Id.ToString(), roles);
                await ReplyAsync("Your staff preferences were updated.");
            }
        }

        [Command("Admin")]
        public async Task Admin(params string[] args)
        {
            string roles = await GetRolesId((IGuildUser)Context.User, (ITextChannel)Context.Channel, args);
            if (roles != null)
            {
                await Program.P.BotDb.UpdateGuildAdminRoles(Context.Guild.Id.ToString(), roles);
                await ReplyAsync("Your admin preferences were updated.");
            }
        }

        public async Task<string> GetRolesId(IGuildUser user, ITextChannel chan, string[] args)
        {
            if (user.Id != user.Guild.OwnerId)
            {
                await chan.SendMessageAsync("Only " + (await user.Guild.GetOwnerAsync()).ToString() + " can do this command.");
                return null;
            }
            if (args.Length == 0)
            {
                await chan.SendMessageAsync("You must provide the roles that will be considered as staff.");
                return null;
            }
            if (args[0].ToLower() == "none")
                return "None";
            var roles = args.Select(x => DiscordUtils.Utils.GetRole(x, chan.Guild));
            if (roles.Any(x => x == null))
            {
                await chan.SendMessageAsync("One of the role you gave isn't a valid one.");
                return null;
            }
            return string.Join("|", roles.Select(x => x.Id.ToString()));
        }

        public static async Task<bool> IsAdmin(IGuildUser user)
            => user.Id == user.Guild.OwnerId || await Program.P.BotDb.IsAdmin(user.GuildId.ToString(), user.RoleIds.Select(x => x.ToString()).ToArray());

        public static async Task<bool> IsStaff(IGuildUser user)
            => await IsAdmin(user) || await Program.P.BotDb.IsStaff(user.GuildId.ToString(), user.RoleIds.Select(x => x.ToString()).ToArray());
    }
}
