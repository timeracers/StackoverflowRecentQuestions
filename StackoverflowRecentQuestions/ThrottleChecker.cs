using Newtonsoft.Json;
using System;
using System.Text;

namespace StackoverflowRecentQuestions
{
    public class ThrottleChecker
    {
        private JsonStore _store;

        public ThrottleChecker(JsonStore store)
        {
            _store = store;
        }

        public Optional<string> IfICanNotMakeRequestsWhy()
        {
            var throttle = _store.Read<StackexchangeThrottle>("StackexchangeThrottle");
            var time = UnixEpoch.Now;
            if (time < throttle.BackoffUntil)
                return new Optional<string>("The server said to backoff for " + (throttle.BackoffUntil - time).ToString() + " more seconds.");
            else if (throttle.QuotaRemaining == 0 && UnixEpoch.Now < throttle.QuotaResetTime)
                return new Optional<string>("You ran out of api requests for today. It will reset at "
                    + DateTimeOffset.FromUnixTimeSeconds(throttle.QuotaResetTime) + ".");
            return new Optional<string>();
        }
    }
}
