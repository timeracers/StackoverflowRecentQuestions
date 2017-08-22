using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace StackoverflowRecentQuestions
{
    public class RecentQuestionsGetter
    {
        private IStore _store;
        private IWebRequester _web;
        public string[] Tags { private get; set; }
        public string Site { private get; set; }
        private int _pagesize;
        public int Pagesize
        {
            set
            {
                if (value > 100 || value < 1)
                    throw new InvalidOperationException("Pagesize must be in the range of 1 and 100");
                _pagesize = value;
            }
        }

        public RecentQuestionsGetter(IStore store, IWebRequester web, string site = "stackoverflow", int pagesize = 30) :
            this(store, web, new string[0], site) { }

        public RecentQuestionsGetter(IStore store, IWebRequester web, string[] tags, string site = "stackoverflow", int pagesize = 30)
        {
            _store = store;
            _web = web;
            Tags = tags;
            Site = site;
            Pagesize = pagesize;
        }

        public async Task<Optional<List<Question>>> GetSince(long unixEpoch, int page = 1)
        {
            return await GetSince(unixEpoch, page, Site, Tags);
        }

        public async Task<Optional<List<Question>>> GetSince(long unixEpoch, int page, string site, string[] tags)
        {
            var throttle = _store.Exists("StackexchangeThrottle.json")
                ? JsonConvert.DeserializeObject<StackexchangeThrottle>(Encoding.UTF8.GetString(_store.Read("StackexchangeThrottle.json")))
                : new StackexchangeThrottle();
            if (DateTimeOffset.Now.ToUnixTimeSeconds() >= throttle.BackoffUntil
                && throttle.QuotaRemaining > 0 || throttle.QuotaRemaining == StackexchangeThrottle.Unknown
                || DateTimeOffset.Now.ToUnixTimeSeconds() >= throttle.QuotaResetTime)
            {
                throttle.BackoffUntil = StackexchangeThrottle.Unknown;
                if (DateTimeOffset.Now.ToUnixTimeSeconds() >= throttle.QuotaResetTime)
                    throttle.QuotaResetTime = StackexchangeThrottle.Unknown;

                var url = "https://api.stackexchange.com/2.2/search/advanced?order=desc&sort=votes";
                url += "&pagesize=" + _pagesize;
                url += "&fromdate=" + unixEpoch;
                url += "&page=" + page;
                url += "&accepted=False";
                url += "&closed=False";
                url += "&site=" + site;
                if (tags.Length > 0)
                {
                    url += "&q=[" + tags[0] + "]";
                    for (var i = 1; i < tags.Count(); i++)
                        url += " OR [" + tags[i] + "]";
                }
                var results = await _web.Request(new Uri(url).AbsoluteUri.Replace("#", "%23"));


                var wrapper = JsonConvert.DeserializeObject<StackexchangeWrapper>(Encoding.UTF8.GetString(Gzip.Decompress(results.Item1)));

                if (results.Item2 == HttpStatusCode.OK)
                {
                    throttle.QuotaRemaining = wrapper.QuotaRemaining;
                    if (throttle.QuotaRemaining + 1 == wrapper.QuotaMax)
                        throttle.QuotaResetTime = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();
                    if (wrapper.Backoff != default(int))
                        throttle.BackoffUntil = DateTimeOffset.Now.AddSeconds(wrapper.Backoff).ToUnixTimeSeconds();

                    var questions = wrapper.Items.ToList();
                    _store.Write("StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(throttle)));
                    return new Optional<List<Question>>(questions);
                }
                else
                {
                    throttle.BackoffUntil = ForceWait2Hours();
                    _store.Write("StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(throttle)));
                    return new Optional<List<Question>>();
                }
            }
            else
            {
                return new Optional<List<Question>>();
            }
        }

        private long ForceWait2Hours()
        {
            return DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds();
        }
    }
}
