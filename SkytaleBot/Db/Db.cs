using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using System.Linq;
using System.Threading.Tasks;

namespace SkytaleBot.Db
{
    public class Db
    {
        public Db()
        {
            R = RethinkDB.R;
        }

        public async Task InitAsync(string dbName)
        {
            this.dbName = dbName;
            conn = await R.Connection().ConnectAsync();
            if (!await R.DbList().Contains(dbName).RunAsync<bool>(conn))
                await R.DbCreate(dbName).RunAsync(conn);
            if (!await R.Db(dbName).TableList().Contains("Users").RunAsync<bool>(conn))
                await R.Db(dbName).TableCreate("Users").RunAsync(conn);
            if (!await R.Db(dbName).TableList().Contains("Guilds").RunAsync<bool>(conn))
                await R.Db(dbName).TableCreate("Guilds").RunAsync(conn);
        }

        public async Task InitGuild(string guildId)
        {
            string guildIdStr = guildId.ToString();
            if (await R.Db(dbName).Table("Guilds").GetAll(guildIdStr).Count().Eq(0).RunAsync<bool>(conn))
            {
                await R.Db(dbName).Table("Guilds").Insert(R.HashMap("id", guildIdStr)
                    .With("Report", "None")
                    .With("StaffRoles", "None")
                    .With("AdminRoles", "None")
                    ).RunAsync(conn);
            }
        }

        public async Task UpdateGuildReport(string guildId, string chanId)
        {
            await R.Db(dbName).Table("Guilds").Update(R.HashMap("id", guildId)
                .With("Report", chanId)
                ).RunAsync(conn);
        }

        public async Task UpdateGuildStaffRoles(string guildId, string roles)
        {
            await R.Db(dbName).Table("Guilds").Update(R.HashMap("id", guildId)
                .With("StaffRoles", roles)
                ).RunAsync(conn);
        }

        public async Task UpdateGuildAdminRoles(string guildId, string roles)
        {
            await R.Db(dbName).Table("Guilds").Update(R.HashMap("id", guildId)
                .With("AdminRoles", roles)
                ).RunAsync(conn);
        }

        public async Task<bool> IsAdmin(string guildId, string userId)
        {
            string roles = (string)(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)).AdminRoles;
            if (roles == "none")
                return false;
            return roles.Split('|').Contains(userId);
        }

        public async Task<bool> IsStaff(string guildId, string userId)
        {
            string roles = (string)(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)).StaffRoles;
            if (roles == "none")
                return false;
            return roles.Split('|').Contains(userId);
        }

        public async Task<string> GetGuild(ulong guildId)
        {
            return JsonConvert.SerializeObject(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn));
        }

        private RethinkDB R;
        private Connection conn;
        private string dbName;
    }
}
