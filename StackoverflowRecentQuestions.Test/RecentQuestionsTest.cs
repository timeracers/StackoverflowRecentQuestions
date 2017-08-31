using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions.Test
{
    [TestClass]
    public class RecentQuestionsTest
    {
        private long Year2017 = new DateTimeOffset(2017, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
        private List<Question> QuestionsWhenUnrestricted;
        private List<Question> QuestionsWhenUnrestrictedPage2;
        private List<Question> QuestionsWithEitherJavaOrCSharpSince2017;
        private bool TestsPassed;
        private string TestsFailedString = "These Tests Failed:";

#if Integration
        [TestMethod]
        public async Task AggregateIntegrationTest()
        {
            var store = new JsonStore(
                new HardDrive(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StackoverflowRecentQuestions"));
            var web = new WebRequester();
            var getter = new RecentQuestionsGetter(store, web);

            QuestionsWhenUnrestricted = await HandleStackexchangeThrottle(async () => await getter.GetSince(0), store);
            QuestionsWhenUnrestrictedPage2 = await HandleStackexchangeThrottle(async () => await getter.GetSince(0, 2), store);
            QuestionsWithEitherJavaOrCSharpSince2017 = await HandleStackexchangeThrottle(
                async () => await getter.GetSince(Year2017, 1, "stackoverflow", new[] { "c#", "java" }), store);

            TestsPassed = true;
            var methods = GetType().GetMethods();
            foreach (var method in methods)
                if (method.Name.IndexOf("Test") == 0)
                    await AggregateResult(method.Name.Substring(4), web);

            if (!TestsPassed)
                Assert.Fail(TestsFailedString);
        }

        private async Task<T> HandleStackexchangeThrottle<T>(Func<Task<Optional<T>>> getter, JsonStore store)
        {
            var result = await getter();
            WaitBackoffPeriodIfHasRemainingQuota(store);
            if (!result.HasValue)
                Assert.Inconclusive("Either backoff period was ignored or this ip is out of stackexchange.api requests for \"today\"");
            return result.Value;
        }

        private void WaitBackoffPeriodIfHasRemainingQuota(JsonStore store)
        {
            var throttle = store.Read<StackexchangeThrottle>("StackexchangeThrottle");
            var currentTime = UnixEpoch.Now;
            if (throttle.BackoffUntil > currentTime && throttle.QuotaRemaining != 0)
            {
                Debug.WriteLine("Waiting for backoff period to end.");
                Thread.Sleep((int)((throttle.BackoffUntil - currentTime) * 1000));
            }
        }

        private async Task AggregateResult(string testName, IWebRequester web)
        {
            bool result;
            try
            {
                var task = (Task<bool>)GetType().GetMethod("Test" + testName).Invoke(this, new object[] { web });
                await task.ConfigureAwait(false);
                result = task.Result;
            }
            catch
            {
                result = false;
            }
            if (!result)
            {
                TestsFailedString += (TestsPassed ? " ": ", ") + testName;
                TestsPassed = false;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> TestRecentQuestionGetterGivesQuestionsWhenNoTagsAreSpecified(IWebRequester _)
        {
            return QuestionsWhenUnrestricted.Count > 0;
        }

        public async Task<bool> TestRecentQuestionGetterGivesQuestionWithAnySpecifiedTags(IWebRequester _)
        {
            return QuestionsWithEitherJavaOrCSharpSince2017.Any() 
                && QuestionsWithEitherJavaOrCSharpSince2017.All((q) => q.Tags.Contains("c#") || q.Tags.Contains("java"));
        }

        public async Task<bool> TestRecentQuestionGetterGivesNoDuplicatesAmongPagesOfQuestions(IWebRequester _)
        {
            return QuestionsWhenUnrestrictedPage2.All((q) => !QuestionsWhenUnrestricted.Contains(q));
        }

        public async Task<bool> TestQuestionsHaveValidLinks(IWebRequester web)
        {
            var result = true;
            var tests = QuestionsWithEitherJavaOrCSharpSince2017.Take(5).Select(async (q) => (await web.Request(q.Link)).StatusCode == HttpStatusCode.OK);
            result = (await Task.WhenAll(tests)).All((t) => t);
            return result;
        }

        public async Task<bool> TestRecentQuestionGetterGivesRecentQuestions(IWebRequester _)
        {
            return QuestionsWithEitherJavaOrCSharpSince2017.All((q) => q.CreationDate >= Year2017);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#endif
    }
}
