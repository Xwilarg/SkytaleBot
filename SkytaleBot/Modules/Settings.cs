using Discord.Commands;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Settings : ModuleBase
    {
        [Command("Report")]
        public async Task Report(string msgId = null)
        {
            ulong id;
            if (msgId == null || !ulong.TryParse(msgId, out id))
            {

            }
        }
    }
}
