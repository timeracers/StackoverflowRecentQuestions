using System;
using System.Net;
using System.Threading.Tasks;
using WebCrawler;

namespace StackoverflowRecentQuestions
{
    public class WebRequester : IWebRequester
    {
        public async Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method = "GET")
        {
            var request = new HttpRequest(url, method);
            await request.Go();
            return new Tuple<byte[], HttpStatusCode>(request.Response, request.StatusCode);
        }

        public async Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method, string parameter)
        {
            var request = new HttpRequest(url, method, parameter);
            await request.Go();
            return new Tuple<byte[], HttpStatusCode>(request.Response, request.StatusCode);
        }
    }
}
