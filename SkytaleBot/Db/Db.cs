using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
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
            if (!await R.Db(dbName).TableList().Contains("Users").RunAsync<bool>(conn)) // TODO: User XP must not be kept in a separate table
                await R.Db(dbName).TableCreate("Users").RunAsync(conn);
            if (!await R.Db(dbName).TableList().Contains("Guilds").RunAsync<bool>(conn))
                await R.Db(dbName).TableCreate("Guilds").RunAsync(conn);
            if (!await R.Db(dbName).TableList().Contains("GuildsLevel").RunAsync<bool>(conn)) // Store roles granted for new levels
                await R.Db(dbName).TableCreate("GuildsLevel").RunAsync(conn);
            xps = new Dictionary<ulong, int>();
            money = new Dictionary<ulong, int>();
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
            if (await R.Db(dbName).Table("GuildsLevel").GetAll(guildIdStr).Count().Eq(0).RunAsync<bool>(conn))
            {
                await R.Db(dbName).Table("GuildsLevel").Insert(R.HashMap("id", guildIdStr)).RunAsync(conn);
            }
        }

        public async Task SetRoleForLevel(ulong guildId, int level, ulong roleId)
        {
            string guildIdStr = guildId.ToString();
            if (await R.Db(dbName).Table("Guilds").GetAll(guildIdStr).Count().Eq(0).RunAsync<bool>(conn))
                await R.Db(dbName).Table("GuildsLevel").Insert(R.HashMap("id", guildIdStr)
                .With(level.ToString(), roleId.ToString())
                ).RunAsync(conn);
            else
                await R.Db(dbName).Table("GuildsLevel").Update(R.HashMap("id", guildIdStr)
                .With(level.ToString(), roleId.ToString())
                ).RunAsync(conn);
        }

        public async Task<ulong> GetRoleForLevel(ulong guildId, int level)
        {
            string guildIdStr = guildId.ToString();
            return ulong.Parse(await R.Db(dbName).Table("GuildsLevel").Get(guildIdStr).GetField(level.ToString()).RunAsync<string>(conn));
        }

        public async Task<string> GetAllRolesLevel(ulong guildId)
            => (await R.Db(dbName).Table("GuildsLevel").Get(guildId.ToString()).RunAsync(conn)).ToString();

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

        public async Task<bool> IsAdmin(string guildId, string[] rolesId)
        {
            string roles = (string)(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)).AdminRoles;
            if (roles == "none")
                return false;
            return roles.Split('|').Any(x => rolesId.Contains(x));
        }

        public async Task<bool> IsStaff(string guildId, string[] rolesId)
        {
            string roles = (string)(await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)).StaffRoles;
            if (roles == "none")
                return false;
            return roles.Split('|').Any(x => rolesId.Contains(x));
        }

        public async Task<string> GetReportChanId(string guildId)
        {
            return (await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn)).Report;
        }

        public async Task<dynamic> GetGuild(ulong guildId)
        {
            return await R.Db(dbName).Table("Guilds").Get(guildId.ToString()).RunAsync(conn);
        }

        public async Task GainXp(ulong userId, int ammount)
        {
            if (!xps.ContainsKey(userId))
                await LoadUser(userId);
            string userIdStr = userId.ToString();
            xps[userId] = await R.Db(dbName).Table("Users").Get(userIdStr).GetField("Xp").Add(ammount).RunAsync<int>(conn);
            await R.Db(dbName).Table("Users").Update(R.HashMap("id", userIdStr)
                .With("Xp", xps[userId])
                ).RunAsync(conn);
        }

        public async Task GainMoney(ulong userId, int ammount)
        {
            if (!xps.ContainsKey(userId))
                await LoadUser(userId);
            string userIdStr = userId.ToString();
            money[userId] = await R.Db(dbName).Table("Users").Get(userIdStr).GetField("Money").Add(ammount).RunAsync<int>(conn);
            await R.Db(dbName).Table("Users").Update(R.HashMap("id", userIdStr)
                .With("Money", money[userId])
                ).RunAsync(conn);
        }

        public async Task<int> GetXp(ulong userId)
        {
            if (!xps.ContainsKey(userId))
                await LoadUser(userId);
            return xps[userId];
        }

        public async Task<int> GetMoney(ulong userId)
        {
            if (!money.ContainsKey(userId))
                await LoadUser(userId);
            return money[userId];
        }

        /// <summary>
        /// Return the time between next daily
        /// If < 0 then time is expired
        /// </summary>
        public async Task<double> CanDoDaily(ulong userId)
            => await CanDoInternal(userId, "Daily");

        public async Task<double> CanDoXp(ulong userId)
            => await CanDoInternal(userId, "WaitXp");

        private async Task<double> CanDoInternal(ulong userId, string dbEntry)
        {
            string userIdStr = userId.ToString();
            if (await R.Db(dbName).Table("Users").GetAll(userIdStr).Count().Eq(0).RunAsync<bool>(conn))
                return -1;
            string daily = (await R.Db(dbName).Table("Users").Get(userIdStr).GetField(dbEntry).RunAsync<string>(conn));
            if (daily == "0")
                return -1;
            return DateTime.Parse(daily).Subtract(DateTime.Now).TotalSeconds;
        }

        public async Task ResetDaily(ulong userId)
            => await ResetInternal(userId, "Daily", dailySecs);

        public async Task ResetXp(ulong userId)
            => await ResetInternal(userId, "WaitXp", secsBeforeNextMessage);

        private async Task ResetInternal(ulong userId, string dbEntry, double value)
        {
            string userIdStr = userId.ToString();
            if (await R.Db(dbName).Table("Users").GetAll(userIdStr).Count().Eq(0).RunAsync<bool>(conn))
                await LoadUser(userId);
            await R.Db(dbName).Table("Users").Update(R.HashMap("id", userIdStr)
                .With(dbEntry, DateTime.Now.AddSeconds(value).ToString())
                ).RunAsync(conn);
        }

        private double dailySecs = 86400.0;
        private double secsBeforeNextMessage = 300.0; // 5 minutes

        private async Task LoadUser(ulong userId)
        {
            string userIdStr = userId.ToString();
            if (await R.Db(dbName).Table("Users").GetAll(userIdStr).Count().Eq(0).RunAsync<bool>(conn))
            {
                xps.Add(userId, 0);
                money.Add(userId, 10);
                await R.Db(dbName).Table("Users").Insert(R.HashMap("id", userIdStr)
                    .With("Xp", 0)
                    .With("Money", 10)
                    .With("Daily", "0")
                    .With("WaitXp", "0")
                    ).RunAsync(conn);
            }
            else
            {
                xps.Add(userId, (int)(await R.Db(dbName).Table("Users").Get(userIdStr).RunAsync(conn)).Xp);
                money.Add(userId, (int)(await R.Db(dbName).Table("Users").Get(userIdStr).RunAsync(conn)).Xp);
            }
        }

        public async Task<List<string>> GetAllUsersAtThanLevel(int level)
        {
            List<string> users = new List<string>();
            foreach (var user in await R.Db(dbName).Table("Users").RunAsync(conn))
            {
                if (Modules.Leveling.GetLevelFromXp((int)user.Xp) >= level)
                    users.Add((string)user.id);
            }
            return users;
        }

        private RethinkDB R;
        private Connection conn;
        private string dbName;
        private Dictionary<ulong, int> xps;
        private Dictionary<ulong, int> money;
    }
}
