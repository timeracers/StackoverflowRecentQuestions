using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions.Test
{
    [TestClass]
    public class MetaTests
    {
        [TestMethod]
        public async Task WhenUnthrottledIntegrationTestsWorkWithoutWaiting()
        {
            var store = new InMemory();
            var questionsWhenUnrestricted = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 1, 1) }, 300, 250));
            var questionsWhenUnrestricted2 = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 2, 2) }, 300, 250));
            var questionsWithEitherJavaOrCSharpSince2017 = JsonConvert.SerializeObject(
                new StackexchangeWrapper(new[] { new Question("", "", 2000000000, 3, "c#") }, 300, 250));
            var web = new FakeWebRequester(CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted),
                CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted2),
                CreateWebResponseWithThisStringGzipped(questionsWithEitherJavaOrCSharpSince2017))
                { DefaultResponse = CreateWebResponseWithThisStringGzipped("") };
            var tests = new RecentQuestionsTest();
            await tests.SetAggregateTestResults(store, web);
            Assert.AreEqual(new Optional<bool>(true), tests.TestsPassed);
        }

        [TestMethod]
        public async Task WhenTheFirstApiCallIsMadeTheQueryResetTimeIsSetAndEnforced()
        {
            var store = new InMemory();
            var questionsWhenUnrestricted = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 1, 1) }, 300, 299));
            var questionsWhenUnrestricted2 = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 2, 2) }, 300, 0));
            var web = new FakeWebRequester(CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted),
                CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted2),
                CreateWebResponseWithThisStringGzipped("This Shouldn't be reached", 500))
                { DefaultResponse = CreateWebResponseWithThisStringGzipped("") };
            var tests = new RecentQuestionsTest();
            await tests.SetAggregateTestResults(store, web);
            Assert.AreEqual(new Optional<bool>(), tests.TestsPassed);
            Assert.AreEqual(1, web.NextResponses.Count);
        }

        [TestMethod]
        public async Task WhenBackoffPeriodIsSetItIsRespected()
        {
            var store = new InMemory();
            var questionsWhenUnrestricted = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 1, 1) }, 300, 250, 1));
            var questionsWhenUnrestricted2 = JsonConvert.SerializeObject(new StackexchangeWrapper(new[] { new Question("", "", 2, 2) }, 300, 250, 1));
            var questionsWithEitherJavaOrCSharpSince2017 = JsonConvert.SerializeObject(
                new StackexchangeWrapper(new[] { new Question("", "", 2000000000, 3, "c#") }, 300, 250));
            var web = new FakeWebRequester(CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted),
                CreateWebResponseWithThisStringGzipped(questionsWhenUnrestricted2),
                CreateWebResponseWithThisStringGzipped(questionsWithEitherJavaOrCSharpSince2017))
            { DefaultResponse = CreateWebResponseWithThisStringGzipped("") };
            var tests = new RecentQuestionsTest();
            var start = DateTime.Now;
            await tests.SetAggregateTestResults(store, web);
            var end = DateTime.Now;
            //Since it isn't very procise it might be only 1.99 seconds.
            Assert.IsTrue(end >= start + new TimeSpan(0, 0, 1));
            Assert.AreEqual(new Optional<bool>(true), tests.TestsPassed);
        }

        [TestMethod]
        public async Task WhenThrottleViolationTestsAreAborted()
        {
            var store = new InMemory();
            var throttleViolation = JsonConvert.SerializeObject(new StackexchangeWrapper());
            var web = new FakeWebRequester(CreateWebResponseWithThisStringGzipped(throttleViolation, 400),
                CreateWebResponseWithThisStringGzipped("This Shouldn't be reached", 500));
            var tests = new RecentQuestionsTest();
            await tests.SetAggregateTestResults(store, web);
            Assert.AreEqual(new Optional<bool>(), tests.TestsPassed);
            Assert.AreEqual(1, web.NextResponses.Count);
        }

        private Tuple<byte[], HttpStatusCode> CreateWebResponseWithThisStringGzipped(string json, int statusCode = 200)
        {
            return new Tuple<byte[], HttpStatusCode>(Gzip.Compress(Encoding.UTF8.GetBytes(json)), (HttpStatusCode)statusCode);
        }
    }
}
