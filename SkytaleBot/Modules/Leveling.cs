using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkytaleBot.Modules
{
    public class Leveling : ModuleBase
    {
        public static int GetLevelFromXp(int xp)
            => (int)Math.Sqrt(xp / 40);

        public static int GetXpFromLevel(int level)
            => level * level * 40;

        [Command("Daily")]
        public async Task Daily()
        {
            double daily = await Program.P.BotDb.CanDoDaily(Context.User.Id);
            if (daily < 0)
            {
                await Program.P.BotDb.ResetDaily(Context.User.Id);
                await Program.P.BotDb.GainMoney(Context.User.Id, 20);
                await ReplyAsync("You earned 20 daily money!");
            }
            else
            {
                DateTime dt = DateTime.Now.AddSeconds(daily);
                await ReplyAsync("You must wait " + dt.Subtract(DateTime.Now).TotalHours.ToString("00") + " more hour.");
            }
        }

        [Command("Give")]
        public async Task Profile(string name, string amount)
        {
            IUser user = await DiscordUtils.Utils.GetUser(name, Context.Guild);
            if (user == null)
            {
                await ReplyAsync("This user doesn't exist.");
                return;
            }
            int resAmount;
            if (!int.TryParse(amount, out resAmount) || resAmount <= 0)
            {
                await ReplyAsync("Invalid amount of money, must be a stricly positive number.");
                return;
            }
            if (await Program.P.BotDb.GetMoney(Context.User.Id) < resAmount && !await Settings.IsAdmin((IGuildUser)Context.User))
            {
                await ReplyAsync("You don't have enough money.");
                return;
            }
            await Program.P.BotDb.GainMoney(Context.User.Id, -resAmount);
            await Program.P.BotDb.GainMoney(user.Id, resAmount);
            await ReplyAsync("Operation completed.");
        }

        [Command("Slot")]
        public async Task Slot(params string[] _)
        {
            const int slotPrice = 5;
            if (await Program.P.BotDb.GetMoney(Context.User.Id) < slotPrice)
            {
                await ReplyAsync("You don't have enough money.");
                return;
            }
            await Program.P.BotDb.GainMoney(Context.User.Id, -slotPrice);
            List<string> slotValues = new List<string>();
            slotValues.AddRange(Enumerable.Repeat("🈷", 1).ToList());
            slotValues.AddRange(Enumerable.Repeat("💎", 3).ToList());
            slotValues.AddRange(Enumerable.Repeat("🎉", 5).ToList());
            slotValues.AddRange(Enumerable.Repeat("🍌", 10).ToList());
            slotValues.AddRange(Enumerable.Repeat("🎲", 20).ToList());
            slotValues.AddRange(Enumerable.Repeat("🐟", 30).ToList());
            Dictionary<string, int> values = new Dictionary<string, int>()
            {
                { "🈷", 1000 },
                { "💎", 250 },
                { "🎉", 100 },
                { "🍌", 70 },
                { "🎲", 40 },
                { "🐟", 16 }
            };
            string first = slotValues[Program.P.Rand.Next(0, slotValues.Count)];
            string second = slotValues[Program.P.Rand.Next(0, slotValues.Count)];
            string third = slotValues[Program.P.Rand.Next(0, slotValues.Count)];
            int amountEarned = 0;
            if (first == second && second == third)
                amountEarned += values[first];
            else if (first == second || first == third)
                amountEarned += values[first] / 4;
            else if (second == third)
                amountEarned += values[second] / 4;
            string str = first + " " + second + " " + third;
            if (amountEarned == 0)
                await ReplyAsync(str + Environment.NewLine +"You didn't win anything");
            else
            {
                await ReplyAsync(str + Environment.NewLine + "You earned " + amountEarned + " money.");
                await Program.P.BotDb.GainMoney(Context.User.Id, amountEarned);
            }
        }

        [Command("Profile")]
        public async Task Profile(string args = null)
        {
            IUser user = null;
            if (args != null)
                user = await DiscordUtils.Utils.GetUser(args, Context.Guild);
            if (user == null)
                user = Context.User;
            int xp = await Program.P.BotDb.GetXp(user.Id);
            int level = GetLevelFromXp(xp);
            await ReplyAsync("", false, new EmbedBuilder
            {
                Title = user.ToString(),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Level",
                        Value = level
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "XP",
                        Value = xp + " / " + GetXpFromLevel(level + 1)
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Money",
                        Value = await Program.P.BotDb.GetMoney(user.Id)
                    }
                }
            }.Build());
        }
    }
}
