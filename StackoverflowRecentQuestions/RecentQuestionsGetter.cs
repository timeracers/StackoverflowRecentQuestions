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
        private JsonStore _store;
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

        public RecentQuestionsGetter(JsonStore store, IWebRequester web, string site = "stackoverflow", int pagesize = 30) :
            this(store, web, new string[0], site) { }

        public RecentQuestionsGetter(JsonStore store, IWebRequester web, string[] tags, string site = "stackoverflow", int pagesize = 30)
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
            var throttle = _store.Read<StackexchangeThrottle>("StackexchangeThrottle");
            if (Unthrottled(throttle))
            {
                if (UnixEpoch.Now >= throttle.QuotaResetTime)
                    throttle.QuotaResetTime = StackexchangeThrottle.UNKNOWN;
                string url = CreateUrl(unixEpoch, page, site, tags);
                var results = await _web.Request(url);
                var wrapper = JsonConvert.DeserializeObject<StackexchangeWrapper>(Encoding.UTF8.GetString(Gzip.Decompress(results.Bytes)));
                if (results.StatusCode == HttpStatusCode.OK)
                {
                    UpdateThrottle(throttle, wrapper);
                    _store.Write("StackexchangeThrottle", throttle);
                    return new Optional<List<Question>>(wrapper.Items.ToList());
                }
                else
                {
                    throttle.BackoffUntil = ForceWait2Hours();
                    _store.Write("StackexchangeThrottle", throttle);
                }
            }
            return new Optional<List<Question>>();
        }

        private static void UpdateThrottle(StackexchangeThrottle throttle, StackexchangeWrapper wrapper)
        {
            throttle.QuotaRemaining = wrapper.QuotaRemaining;
            if (throttle.QuotaRemaining + 1 == wrapper.QuotaMax)
                throttle.QuotaResetTime = UnixEpoch.Now + (long)TimeSpan.FromDays(1).TotalSeconds;
            throttle.BackoffUntil = wrapper.Backoff != default(int) ? UnixEpoch.Now + wrapper.Backoff : StackexchangeThrottle.UNKNOWN;
        }

        private string CreateUrl(long unixEpoch, int page, string site, string[] tags)
        {
            var url = "https://api.stackexchange.com/2.2/search/advanced?order=desc&sort=votes";
            url += "&pagesize=" + _pagesize;
            url += "&fromdate=" + unixEpoch;
            url += "&page=" + page;
            url += "&closed=False";
            url += "&site=" + site;
            url += "&q=answers:0";
            if (tags.Length > 0)
            {
                url += "+[" + tags[0] + "]";
                for (var i = 1; i < tags.Count(); i++)
                    url += " OR [" + tags[i] + "]";
            }
            return new Uri(url).AbsoluteUri.Replace("#", "%23");
        }

        private static bool Unthrottled(StackexchangeThrottle throttle)
        {
            return UnixEpoch.Now >= throttle.BackoffUntil
                && (throttle.QuotaRemaining > 0 || throttle.QuotaRemaining == StackexchangeThrottle.UNKNOWN
                || UnixEpoch.Now >= throttle.QuotaResetTime);
        }

        private long ForceWait2Hours()
        {
            return UnixEpoch.Now + (long)TimeSpan.FromHours(2).TotalSeconds;
        }
    }
}
