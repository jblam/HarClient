using System;
using System.Collections.Generic;
using System.Text;

namespace JBlam.HarClient.Tests
{
    /// <summary>
    /// Exception type for asserting behaviour which is expected to throw
    /// </summary>
    [Serializable]
    public class TestExpectedException : Exception
    {
        public TestExpectedException() { }
        public TestExpectedException(string message) : base(message) { }
        public TestExpectedException(string message, Exception inner) : base(message, inner) { }
        protected TestExpectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
