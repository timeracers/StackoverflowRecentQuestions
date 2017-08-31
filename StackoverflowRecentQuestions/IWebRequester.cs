using System.Threading.Tasks;

namespace StackoverflowRecentQuestions
{
    public interface IWebRequester
    {
        Task<Response> Request(string url, string method = "GET");
        Task<Response> Request(string url, string method, string parameter);
    }
}