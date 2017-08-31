using System.Net;

namespace StackoverflowRecentQuestions
{
    public class Response
    {
        public byte[] Bytes { get; }
        public HttpStatusCode StatusCode { get; }

        public Response(byte[] bytes, HttpStatusCode statusCode)
        {
            Bytes = bytes;
            StatusCode = statusCode;
        }
    }
}
