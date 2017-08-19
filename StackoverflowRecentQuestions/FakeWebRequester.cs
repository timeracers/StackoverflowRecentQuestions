using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions
{
    public class FakeWebRequester : IWebRequester
    {
        public Tuple<byte[], HttpStatusCode> DefaultResponse { get; set; }
        public List<Tuple<byte[], HttpStatusCode>> NextResponses { get; set; }

        public FakeWebRequester(params Tuple<byte[], HttpStatusCode>[] firstResponses)
        {
            DefaultResponse = new Tuple<byte[], HttpStatusCode>(new byte[0], (HttpStatusCode)500);
            NextResponses = firstResponses.ToList();
        }

        public async Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method = "GET")
        {
            return await Request(url, method, "");
        }

        public async Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method, string parameter)
        {
            if(NextResponses.Count > 0)
            {
                var response = NextResponses[0];
                NextResponses.RemoveAt(0);
                return response;
            }
            return DefaultResponse;
        }
    }
}
