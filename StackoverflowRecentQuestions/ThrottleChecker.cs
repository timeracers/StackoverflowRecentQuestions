using Newtonsoft.Json;
using System;
using System.Text;

namespace StackoverflowRecentQuestions
{
    public class ThrottleChecker
    {
        private IStore _store;

        public ThrottleChecker(IStore store)
        {
            _store = store;
        }

        public Optional<string> IfICanNotMakeRequestsWhy()
        {
            var throttle = _store.Exists("StackexchangeThrottle.json")
                ? JsonConvert.DeserializeObject<StackexchangeThrottle>(Encoding.UTF8.GetString(_store.Read("StackexchangeThrottle.json")))
                : new StackexchangeThrottle();
            var time = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (time < throttle.BackoffUntil)
                return new Optional<string>("The server said to backoff for " + (throttle.BackoffUntil - time).ToString() + " more seconds.");
            else if (throttle.QuotaRemaining == 0 && DateTimeOffset.Now.ToUnixTimeSeconds() < throttle.QuotaResetTime)
                return new Optional<string>("You ran out of api requests for today. It will reset at "
                    + DateTimeOffset.FromUnixTimeSeconds(throttle.QuotaResetTime) + ".");
            return new Optional<string>();
        }
    }
}
