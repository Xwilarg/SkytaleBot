using DiscordUtils;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SkytaleBot.Features.Moderation
{
    public static class MessageCheck
    {
        public static async Task<MessageError?> CheckMessageText(string msg)
        {
            if (msg.Length == 0)
                return null;
            dynamic json;
            using (HttpClient hc = new HttpClient())
            {
                HttpResponseMessage post = await hc.PostAsync("https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key=" + Program.P.PerspectiveApi, new StringContent(
                        JsonConvert.DeserializeObject("{comment: {text: \"" + Utils.EscapeString(msg) + "\"},"
                                                    + "languages: [\"en\"],"
                                                    + "requestedAttributes: {" + string.Join(":{}, ", categories.Select(x => x.Item1)) + ":{}} }").ToString(), Encoding.UTF8, "application/json"));

                json = JsonConvert.DeserializeObject(await post.Content.ReadAsStringAsync());
            }
            foreach (var s in categories)
            {
                float value = json.attributeScores[s.Item1].summaryScore.value;
                if (value >= s.Item2)
                    return new MessageError
                    {
                        currValue = value,
                        flag = s.Item1,
                        maxValue = s.Item2
                    };
            }
            return null;
        }

        public struct MessageError
        {
            public string flag;
            public float currValue;
            public float maxValue;
        }

        private static readonly Tuple<string, float>[] categories = new Tuple<string, float>[] {
            new Tuple<string, float>("TOXICITY", .90f),
            new Tuple<string, float>("SEVERE_TOXICITY", .70f),
            new Tuple<string, float>("IDENTITY_ATTACK", .70f),
            new Tuple<string, float>("INSULT", .70f),
            new Tuple<string, float>("THREAT", .70f),
            new Tuple<string, float>("OBSCENE", .70f),
            new Tuple<string, float>("INFLAMMATORY", .70f),
            new Tuple<string, float>("PROFANITY", .70f)
        };
    }
}
