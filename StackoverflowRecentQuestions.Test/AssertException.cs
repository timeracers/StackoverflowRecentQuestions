using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace StackoverflowRecentQuestions.Test
{
    public static class AssertException
    {
        public static void Throws(Action action)
        {
            Throws<Exception>(action);
        }

        public static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            Assert.Fail("Exception" + typeof(T).ToString() + " not thrown.");
        }
    }
}
