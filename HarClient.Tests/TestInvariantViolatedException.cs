using System;

namespace JBlam.HarClient.Tests
{
    /// <summary>
    /// Exception type for any exceptions thrown in the course of running an automated test.
    /// </summary>
    /// <remarks>
    /// This exception indicates that the test code itself has a bug. This should only be
    /// caught or expected in meta-tests.
    /// </remarks>
    [Serializable]
    public class TestInvariantViolatedException : Exception
    {
        public TestInvariantViolatedException() { }
        public TestInvariantViolatedException(string message) : base(message) { }
        public TestInvariantViolatedException(string message, Exception inner) : base(message, inner) { }
        protected TestInvariantViolatedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
