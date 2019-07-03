using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
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

        public async Task InitGuild(ulong guildId)
        {
            string guildIdStr = guildId.ToString();
            if (await R.Db(dbName).Table("Guilds").GetAll(guildIdStr).Count().Eq(0).RunAsync<bool>(conn))
            {
                await R.Db(dbName).Table("Guilds").Insert(R.HashMap("id", guildIdStr)
                    .With("Report", "None")
                    ).RunAsync(conn);
            }
        }
        public async Task<string> GetGuild(ulong guildId)
        {
            return (JsonConvert.SerializeObject(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)));
        }

        private RethinkDB R;
        private Connection conn;
        private string dbName;
    }
}
