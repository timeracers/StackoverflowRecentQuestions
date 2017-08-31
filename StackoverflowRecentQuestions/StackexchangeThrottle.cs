using Newtonsoft.Json;

namespace StackoverflowRecentQuestions
{
    public class StackexchangeThrottle
    {
        public const long UNKNOWN = -1;

        public long QuotaResetTime { get; set; } = UNKNOWN;
        public int QuotaRemaining { get; set; } = (int)UNKNOWN;
        public long BackoffUntil { get; set; } = UNKNOWN;

        [JsonConstructor]
        private StackexchangeThrottle() { }
        
        public StackexchangeThrottle(int quotaRemaining = (int)UNKNOWN, long quotaResetTime = UNKNOWN, long backoffUntil = UNKNOWN)
        {
            QuotaRemaining = quotaRemaining;
            QuotaResetTime = quotaResetTime;
            BackoffUntil = backoffUntil;
        }
    }
}
