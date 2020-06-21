using HarSharp;
using JBlam.HarClient.Tests.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class HarSerialisationBehaviour
    {
        static Har CreateEmptyHar() => new HarMessageHandler().CreateHar();

        [TestMethod]
        public void EmptyHarIsValid()
        {
            var har = CreateEmptyHar();
            HarAssert.IsValid(har);
        }
    }
}
