using Discord;
using System.Linq;
using System.Threading.Tasks;

namespace SkytaleBot.Features.Moderation
{
    public static class KickBanCheck
    {
        public static async Task<bool> CanKick(IGuild guild)
            => (await guild.GetCurrentUserAsync()).GuildPermissions.KickMembers;

        public static async Task<bool> CanBan(IGuild guild)
            => (await guild.GetCurrentUserAsync()).GuildPermissions.BanMembers;

        public static int HighestRole(IGuildUser user)
            => user.Guild.Roles.Where(x => user.RoleIds.Contains(x.Id)).OrderByDescending(x => x.Id).Select(x => x.Position).First();
    }
}
