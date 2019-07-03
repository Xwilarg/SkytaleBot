using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    [Group("Set")]
    public class Settings : ModuleBase
    {
        [Command("Report")]
        public async Task Report(string msgId = null)
        {
            if (msgId == null)
            {
                await ReplyAsync("You must provide a valid text channel or 'none'");
                return;
            }
            ITextChannel chan = null;
            if (msgId.ToLower() != "none")
            {
                chan = await DiscordUtils.Utils.GetTextChannel(msgId, Context.Guild);
                if (chan == null)
                {
                    await ReplyAsync("You must provide a valid text channel or 'none'");
                    return;
                }
            }
            await Program.P.BotDb.UpdateGuildReport(Context.Guild.Id.ToString(), chan == null ? "None" : chan.Id.ToString());
            await ReplyAsync("Your report preferences were updated.");
        }
    }
}
