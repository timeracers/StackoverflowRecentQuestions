using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackoverflowRecentQuestions;
using StackoverflowRecentQuestions.UI;
using System;

namespace StackoverflowRecentQuestions.Test
{
    [TestClass]
    public class SingleStoreTests
    {
        [TestMethod]
        public void SingleStoreWhenConstructedWithoutValueItHasNoValue()
        {
            var store = new SingleStore<string>();
            AssertHasNoValue(store);
        }

        [TestMethod]
        public void SingleStoreWhenConstructedWithAValueItHasThatValue()
        {
            var store = new SingleStore<string>("");
            AssertHasValue("", store);
        }

        [TestMethod]
        public void SingleStoreWhenClearedItNoLongerHasValue()
        {
            var store = new SingleStore<string>("");
            store.Clear();
            AssertHasNoValue(store);
        }

        [TestMethod]
        public void SingleStoreWhenValueIsSetItNowHasThatValue()
        {
            var store = new SingleStore<string>();
            store.Value = "";
            AssertHasValue("", store);
        }

        [TestMethod]
        public void SingleStoreWhenConstructedOrSetWithNullItHasNoValue()
        {
            var store = new SingleStore<string>(null);
            AssertHasNoValue(store);
            store.Value = null;
            AssertHasNoValue(store);

        }

        private void AssertHasNoValue<T>(SingleStore<T> store)
        {
            Assert.IsFalse(store.HasValue);
            AssertException.Throws<InvalidOperationException>(() => { var _ = store.Value; });
        }

        private void AssertHasValue<T>(T value, SingleStore<T> store)
        {
            Assert.IsTrue(store.HasValue);
            Assert.AreEqual(value, store.Value);
        }
    }
}
