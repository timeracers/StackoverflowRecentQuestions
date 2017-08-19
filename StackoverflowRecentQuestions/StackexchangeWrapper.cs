using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions
{
    public class StackexchangeWrapper
    {
        //Is absent if bad request (400)
        public Question[] Items { get; set; }
        [JsonProperty("quota_max")]
        //Absent if bad request
        public int QuotaMax { get; set; }
        [JsonProperty("quota_remaining")]
        //Absent if bad request
        public int QuotaRemaining { get; set; }
        //May be absent
        public int Backoff { get; set; }

        public StackexchangeWrapper()
        {
        }

        public StackexchangeWrapper(Question[] items, int quotaMax, int quotaRemaining, int backoff = 0)
        {
            Items = items;
            QuotaMax = quotaMax;
            QuotaRemaining = quotaRemaining;
            Backoff = backoff;
        }
    }
}
