using Discord;
using Discord.Commands;
using DiscordUtils;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Moderation : ModuleBase
    {
        [Command("Kick")]
        public async Task Kick(string member, params string[] reason)
        {
            if (!await Settings.IsStaff((IGuildUser)Context.User))
            {
                await ReplyAsync("Only a staff can do this command.");
                return;
            }
            IGuildUser user = await Utils.GetUser(member, Context.Guild);
            if (user == null)
            {
                await ReplyAsync("Invalid user");
                return;
            }
            if (!await Features.Moderation.KickBanCheck.CanKickAsync(Context.Guild))
            {
                await ReplyAsync("I don't have the permission to kick people");
                return;
            }
            if (Features.Moderation.KickBanCheck.HighestRole(await Context.Guild.GetCurrentUserAsync()) <= Features.Moderation.KickBanCheck.HighestRole(user))
            {
                await ReplyAsync("I can't kick this user.");
                return;
            }
            await user.KickAsync(string.Join(" ", reason));
            await ReplyAsync("User " + user.ToString() + " was kicked" + (reason.Length > 1 ? " for the following reason: " + string.Join(" ", reason) : "."));
        }

        [Command("Ban")]
        public async Task Ban(string member, params string[] reason)
        {
            if (!await Settings.IsAdmin((IGuildUser)Context.User))
            {
                await ReplyAsync("Only an admin can do this command.");
                return;
            }
            IGuildUser user = await Utils.GetUser(member, Context.Guild);
            if (user == null)
            {
                await ReplyAsync("Invalid user");
                return;
            }
            if (!await Features.Moderation.KickBanCheck.CanBanAsync(Context.Guild))
            {
                await ReplyAsync("I don't have the permission to kick people");
                return;
            }
            if (Features.Moderation.KickBanCheck.HighestRole(await Context.Guild.GetCurrentUserAsync()) <= Features.Moderation.KickBanCheck.HighestRole(user))
            {
                await ReplyAsync("I can't kick this user.");
                return;
            }
            await user.BanAsync(0, string.Join(" ", reason));
            await ReplyAsync("User " + user.ToString() + " was banned" + (reason.Length > 1 ? " for the following reason: " + string.Join(" ", reason) : "."));
        }
    }
}
