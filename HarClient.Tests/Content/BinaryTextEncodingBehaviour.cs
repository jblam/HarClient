using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JBlam.HarClient.Tests.Content
{
    [TestClass]
    public class BinaryTextEncodingBehaviour
    {
        static readonly string AllByteValues = new string(Enumerable.Range(0, 256).Select(i => (char)i).ToArray());

        [TestMethod]
        public void CanRoundtripBinaryContent()
        {
            var content = Tuple.Create(AllByteValues);
            var encoded = JsonConvert.SerializeObject(content, HarMessageHandler.HarSerializerSettings);
            var decoded = JsonConvert.DeserializeObject<Tuple<string>>(encoded);
            // the NEXT LINE (NEL) character has been problematic in other approaches.
            StringAssert.Contains(encoded, "\\u0085");
            Assert.AreEqual(AllByteValues, decoded.Item1);
        }
    }
}
