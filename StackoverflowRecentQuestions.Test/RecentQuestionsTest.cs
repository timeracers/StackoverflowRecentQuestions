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
        public Optional<bool> TestsPassed { get; private set; } = new Optional<bool>();
        private string TestsFailedString = "These Tests Failed:";

#if Integration
        [TestMethod]
        public async Task AggregateIntegrationTest()
        {
            await SetAggregateTestResults(
                new IO(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StackoverflowRecentQuestions"), new WebRequester());
            if (TestsPassed.HasValue && !TestsPassed.Value)
                Assert.Fail(TestsFailedString);
            else if (!TestsPassed.HasValue)
                Assert.Inconclusive("Either backoff period was ignored or this ip is out of stackexchange.api requests for \"today\"");
        }
#endif

        public async Task SetAggregateTestResults(IStore store, IWebRequester web)
        {
            var getter = new RecentQuestionsGetter(store, web);

            WaitBackoffPeriodIfHasRemainingQuota(store);
            var result = await getter.GetSince(0);
            if (result.HasValue)
                QuestionsWhenUnrestricted = result.Value;
            else
                return;

            WaitBackoffPeriodIfHasRemainingQuota(store);
            result = await getter.GetSince(0, 2);
            if (result.HasValue)
                QuestionsWhenUnrestrictedPage2 = result.Value;
            else
                return;

            WaitBackoffPeriodIfHasRemainingQuota(store);
            result = await getter.GetSince(Year2017, 1, "stackoverflow", new[] { "c#", "java" });
            if (result.HasValue)
                QuestionsWithEitherJavaOrCSharpSince2017 = result.Value;
            else
                return;

            TestsPassed = new Optional<bool>(true);
            var methods = GetType().GetMethods();
            foreach(var method in methods)
                if(method.Name.IndexOf("Test") == 0)
                    await AggregateResult(method.Name.Substring(4), web);
        }

        private void WaitBackoffPeriodIfHasRemainingQuota(IStore store)
        {
            var throttle = store.Exists("StackexchangeThrottle.json")
                ? JsonConvert.DeserializeObject<StackexchangeThrottle>(Encoding.UTF8.GetString(store.Read("StackexchangeThrottle.json")))
                : new StackexchangeThrottle();
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
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
                TestsFailedString += (TestsPassed.Value ? " ": ", ") + testName;
                TestsPassed = new Optional<bool>(false);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> TestRecentQuestionGetterGivesQuestionsWhenNoTagsAreSpecified(IWebRequester _)
        {
            return QuestionsWhenUnrestricted.Count > 0;
        }

        public async Task<bool> TestRecentQuestionGetterGivesQuestionWithAnySpecifiedTags(IWebRequester _)
        {
            return QuestionsWithEitherJavaOrCSharpSince2017.All((q) => q.Tags.Contains("c#") || q.Tags.Contains("java"));
        }

        public async Task<bool> TestQuestionsWithEitherJavaOrCSharpSince2017IsNotEmpty(IWebRequester _)
        {
            return QuestionsWithEitherJavaOrCSharpSince2017.Count > 0;
        }

        public async Task<bool> TestRecentQuestionGetterGivesNoDuplicatesAmongPagesOfQuestions(IWebRequester _)
        {
            return QuestionsWhenUnrestrictedPage2.All((q) => !QuestionsWhenUnrestricted.Contains(q));
        }

        public async Task<bool> TestQuestionsHaveValidLinks(IWebRequester web)
        {
            var result = true;
            var tests = QuestionsWhenUnrestricted.Take(5).Select(async (q) => (await web.Request(q.Link)).Item2 == HttpStatusCode.OK);
            result = (await Task.WhenAll(tests)).All((t) => t);
            return result;
        }

        public async Task<bool> TestRecentQuestionGetterGivesRecentQuestions(IWebRequester _)
        {
            return QuestionsWithEitherJavaOrCSharpSince2017.All((q) => q.CreationDate >= Year2017);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
