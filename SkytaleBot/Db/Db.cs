﻿using Newtonsoft.Json;
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
            if (!await R.Db(dbName).TableList().Contains("Users").RunAsync<bool>(conn))
                await R.Db(dbName).TableCreate("Users").RunAsync(conn);
            if (!await R.Db(dbName).TableList().Contains("Guilds").RunAsync<bool>(conn))
                await R.Db(dbName).TableCreate("Guilds").RunAsync(conn);
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
        {
            string userIdStr = userId.ToString();
            if (await R.Db(dbName).Table("Users").GetAll(userIdStr).Count().Eq(0).RunAsync<bool>(conn))
                return -1;
            string daily = (await R.Db(dbName).Table("Users").Get(userIdStr).GetField("Daily").RunAsync<string>(conn));
            if (daily == "0")
                return -1;
            return dailySecs - DateTime.Now.Subtract(DateTime.Parse(daily)).TotalSeconds;
        }

        public async Task ResetDaily(ulong userId)
        {
            string userIdStr = userId.ToString();
            if (await R.Db(dbName).Table("Users").GetAll(userIdStr).Count().Eq(0).RunAsync<bool>(conn))
                await LoadUser(userId);
            await R.Db(dbName).Table("Users").Update(R.HashMap("id", userIdStr)
                .With("Daily", DateTime.Now.ToString())
                ).RunAsync(conn);
        }

        private double dailySecs = 86400.0;

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
                    ).RunAsync(conn);
            }
            else
            {
                xps.Add(userId, (int)(await R.Db(dbName).Table("Users").Get(userIdStr).RunAsync(conn)).Xp);
                money.Add(userId, (int)(await R.Db(dbName).Table("Users").Get(userIdStr).RunAsync(conn)).Xp);
            }
        }

        private RethinkDB R;
        private Connection conn;
        private string dbName;
        private Dictionary<ulong, int> xps;
        private Dictionary<ulong, int> money;
    }
}
