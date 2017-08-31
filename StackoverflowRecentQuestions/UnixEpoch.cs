namespace System
{
    public static class UnixEpoch
    {
        public static long Now
        {
            get { return DateTimeOffset.Now.ToUnixTimeSeconds(); }
        }
    }
}
