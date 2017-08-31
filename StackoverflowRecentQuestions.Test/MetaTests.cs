using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions.Test
{
    [TestClass]
    public class MetaTests
    {
        [TestMethod]
        public async Task WhenUnthrottledRecentQuestionGetterReturnsQuestions()
        {
            var jsonQuestions = CreateJsonizedEmptyStackexchangeWrapper(300, 250);
            var web = new FakeWebRequester(CreateGzippedStringResponse(jsonQuestions));
            var getter = new RecentQuestionsGetter(new JsonStore(new DictionaryStore()), web);

            var questions = await getter.GetSince(0);

            Assert.IsTrue(questions.HasValue);
        }

        [TestMethod]
        public async Task WhenTheFirstApiCallIsMadeTheQueryResetTimeIsSetAndEnforced()
        {
            var jsonQuestions = CreateJsonizedEmptyStackexchangeWrapper(300, 299);
            var jsonQuestions2 = CreateJsonizedEmptyStackexchangeWrapper(300, 0);
            var web = new FakeWebRequester(CreateGzippedStringResponse(jsonQuestions),
                CreateGzippedStringResponse(jsonQuestions2),
                CreateGzippedStringResponse("This Shouldn't be reached", 500));
            var getter = new RecentQuestionsGetter(new JsonStore(new DictionaryStore()), web);

            await getter.GetSince(0);
            var questions = await getter.GetSince(0);
            var noValue = await getter.GetSince(0);

            Assert.IsTrue(questions.HasValue);
            Assert.IsFalse(noValue.HasValue);
            Assert.AreEqual(1, web.NextResponses.Count);
        }

        [TestMethod]
        public async Task WhenBackoffPeriodIsSetItIsRespected()
        {
            var jsonQuestions = CreateJsonizedEmptyStackexchangeWrapper(300, 250, 2);
            var web = new FakeWebRequester(CreateGzippedStringResponse(jsonQuestions),
                CreateGzippedStringResponse("This Shouldn't be reached", 500));
            var getter = new RecentQuestionsGetter(new JsonStore(new DictionaryStore()), web);

            await getter.GetSince(0);
            var noValue = await getter.GetSince(0);

            Assert.IsFalse(noValue.HasValue);
            Assert.AreEqual(1, web.NextResponses.Count);
        }

        [TestMethod]
        public async Task WhenThrottleViolationRecentQuestionGetterReturnsNoValueAndPreventsRetrying()
        {
            var throttleViolation = CreateJsonizedEmptyStackexchangeWrapper(300, 250);
            var web = new FakeWebRequester(CreateGzippedStringResponse(throttleViolation, 400),
                CreateGzippedStringResponse("This Shouldn't be reached", 500));
            var getter = new RecentQuestionsGetter(new JsonStore(new DictionaryStore()), web);

            var noValue = await getter.GetSince(0);
            var noValueToo = await getter.GetSince(0);

            Assert.IsFalse(noValue.HasValue);
            Assert.IsFalse(noValueToo.HasValue);
            Assert.AreEqual(1, web.NextResponses.Count);
        }

        private string CreateJsonizedEmptyStackexchangeWrapper(int quotaMax, int quotaRemaining, int backoff = 0)
        {
            return JsonConvert.SerializeObject(new StackexchangeWrapper(new Question[0], quotaMax, quotaRemaining, backoff));
        }

        private Response CreateGzippedStringResponse(string contents, int statusCode = 200)
        {
            return new Response(Gzip.Compress(Encoding.UTF8.GetBytes(contents)), (HttpStatusCode)statusCode);
        }
    }
}
