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
        public Response DefaultResponse { get; set; }
        public List<Response> NextResponses { get; set; }

        public FakeWebRequester(params Response[] firstResponses)
        {
            DefaultResponse = new Response(new byte[0], (HttpStatusCode)500);
            NextResponses = firstResponses.ToList();
        }

        public async Task<Response> Request(string url, string method = "GET")
        {
            return await Request(url, method, "");
        }

        public async Task<Response> Request(string url, string method, string parameter)
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
