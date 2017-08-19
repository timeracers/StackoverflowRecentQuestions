using System;
using System.Net;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions
{
    public interface IWebRequester
    {
        Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method = "GET");
        Task<Tuple<byte[], HttpStatusCode>> Request(string url, string method, string parameter);
    }
}