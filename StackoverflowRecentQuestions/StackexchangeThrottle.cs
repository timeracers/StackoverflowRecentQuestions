namespace StackoverflowRecentQuestions
{
    public class StackexchangeThrottle
    {
        public const long Unknown = -1;

        public long QuotaResetTime { get; set; }
        public int QuotaRemaining { get; set; }
        public long BackoffUntil { get; set; }

        public StackexchangeThrottle(int quotaRemaining = (int)Unknown, long quotaResetTime = Unknown, long backoffUntil = Unknown)
        {
            QuotaRemaining = quotaRemaining;
            QuotaResetTime = quotaResetTime;
            BackoffUntil = backoffUntil;
        }
    }
}
