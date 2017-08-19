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
            Assert.AreEqual(new Optional<string>(), new ThrottleChecker(new InMemory()).IfICanNotMakeRequestsWhy());
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsBackoffIfCurrentTimeIsLessThenBackoffUntilTime()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new StackexchangeThrottle(backoffUntil:DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 2))) }
            };
            Assert.AreEqual(new Optional<string>("The server said to backoff for").Value,
                new ThrottleChecker(new InMemory(startingValues)).IfICanNotMakeRequestsWhy().Value.Substring(0, 30));
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsOutOfRequestsIf0RequestsRemainingAndRequestsDidNotReset()
        {
            var resetTime = DateTimeOffset.UtcNow.AddHours(12);
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new StackexchangeThrottle(0, resetTime.ToUnixTimeSeconds()))) }
            };
            Assert.AreEqual(new Optional<string>("You ran out of api requests for today. It will reset at " + resetTime + "."),
                new ThrottleChecker(new InMemory(startingValues)).IfICanNotMakeRequestsWhy());
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsNoReasonToNotRequestWhenYouAreOutButItDoesNotKnowTheResetTime()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new StackexchangeThrottle(0))) }
            };
            Assert.AreEqual(new Optional<string>(), new ThrottleChecker(new InMemory(startingValues)).IfICanNotMakeRequestsWhy());
        }

        [TestMethod]
        public void IfICanNotMakeRequestsWhyReturnsNoReasonToNotRequestWhenBackoffPeriodEnded()
        {
            var startingValues = new Dictionary<string, byte[]>() {
                { "StackexchangeThrottle.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new StackexchangeThrottle(backoffUntil: 1))) }
            };
            Assert.AreEqual(new Optional<string>(), new ThrottleChecker(new InMemory(startingValues)).IfICanNotMakeRequestsWhy());
        }
    }
}
