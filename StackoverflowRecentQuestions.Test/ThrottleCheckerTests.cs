using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StackoverflowRecentQuestions.Test
{
    [TestClass]
    public class ThrottleCheckerTests
    {
        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsNoReasonToNotRequestWhenNoFileIsStored()
        {
            Assert.AreEqual(new Optional<string>(), new ThrottleChecker(new JsonStore(new DictionaryStore())).IfICanNotMakeRequestsWhy());
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsBackoffIfCurrentTimeIsLessThenBackoffUntilTime()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new StackexchangeThrottle(backoffUntil:UnixEpoch.Now + 2))) }
            };
            var checker = new ThrottleChecker(new JsonStore(new DictionaryStore(startingValues)));
                
            var why = checker.IfICanNotMakeRequestsWhy().Value.Substring(0, 30);

            Assert.AreEqual(new Optional<string>("The server said to backoff for").Value, why);
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsOutOfRequestsIf0RequestsRemainingAndRequestsDidNotReset()
        {
            var resetTime = UnixEpoch.Now + (long)TimeSpan.FromHours(12).TotalSeconds;
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new StackexchangeThrottle(0, resetTime))) }
            };
            var checker = new ThrottleChecker(new JsonStore(new DictionaryStore(startingValues)));

            var why = checker.IfICanNotMakeRequestsWhy();

            Assert.AreEqual(new Optional<string>("You ran out of api requests for today. It will reset at "
                + DateTimeOffset.FromUnixTimeSeconds(resetTime) + "."), why);
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsNoReasonToNotRequestWhenYouAreOutButItDoesNotKnowTheResetTime()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new StackexchangeThrottle(0))) }
            };
            var checker = new ThrottleChecker(new JsonStore(new DictionaryStore(startingValues)));

            var noReason = checker.IfICanNotMakeRequestsWhy();

            Assert.AreEqual(new Optional<string>(), noReason);
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsNoReasonToNotRequestWhenBackoffPeriodEnded()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new StackexchangeThrottle(backoffUntil: 1))) }
            };
            var checker = new ThrottleChecker(new JsonStore(new DictionaryStore(startingValues)));

            var noReason = checker.IfICanNotMakeRequestsWhy();

            Assert.AreEqual(new Optional<string>(), noReason);
        }
    }
}
