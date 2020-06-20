using System;

namespace HarClient.Tests
{
    /// <summary>
    /// Exception type for any exceptions thrown in the course of running an automated test.
    /// </summary>
    /// <remarks>
    /// This exception indicates that the test code itself has a bug. This should only be
    /// caught or expected in meta-tests.
    /// </remarks>
    [System.Serializable]
    public class TestException : Exception
    {
        public TestException() { }
        public TestException(string message) : base(message) { }
        public TestException(string message, Exception inner) : base(message, inner) { }
        protected TestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
