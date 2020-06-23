using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace JBlam.HarClient.Tests.Validation
{
    static class JAssert
    {
        public static JObject IsObject(JToken token)
        {
            Assert.IsInstanceOfType(token, typeof(JObject), "Token {0} is unexpectedly not an object", token.PathOrRoot());
            return (JObject)token;
        }
        public static JToken HasProperty(JObject parent, string child)
        {
            var candidate = parent[child];
            Assert.IsNotNull(candidate, "Mandatory property {0} missing on {1}", child, parent.PathOrRoot());
            return candidate;
        }
        public static JToken HasProperty(JObject parent, string child, JTokenType expectedType)
        {
            var token = HasProperty(parent, child);
            Assert.AreEqual(expectedType, token.Type, "Mandatory property {0}.{1} has unexpected type", parent.PathOrRoot(), child);
            return token;
        }
        public static JToken? HasOptionalProperty(JObject parent, string child, JTokenType expectedType)
        {
            var candidate = parent[child];
            if (!(candidate is null))
                Assert.AreEqual(expectedType, candidate.Type, "Optional property {0}.{1} has unexpected type", parent.PathOrRoot(), child);
            return candidate;
        }
        public static JValue HasNumericProperty(JObject parent, string child)
        {
            var token = HasProperty(parent, child);
            Assert.IsTrue(token.Type == JTokenType.Integer || token.Type == JTokenType.Float, "{0} is not a numeric value", token.Path);
            return (JValue)token;
        }
        public static JValue? HasOptionalNumericProperty(JObject parent, string child)
        {
            var token = parent[child];
            if (token is null)
                return null;
            Assert.IsTrue(token.Type == JTokenType.Integer || token.Type == JTokenType.Float, "{0} is not a numeric value", token.Path);
            return (JValue)token;
        }
        public static JToken? HasOptionalPropertyOrNull(JObject parent, string child, JTokenType expectedTypeIfNotNull)
        {
            var token = parent[child];
            if (token is null)
                return null;
            if (token.Type == JTokenType.Null)
                return token;
            Assert.AreEqual(expectedTypeIfNotNull, token.Type, "Property {0}.{1} has unexpected type", parent.PathOrRoot(), child);
            return token;
        }
        public static JObject HasObjectProperty(JObject parent, string child) =>
            (JObject)HasProperty(parent, child, JTokenType.Object);
        public static JObject? HasOptionalObjectProperty(JObject parent, string child) =>
            (JObject?)HasOptionalProperty(parent, child, JTokenType.Object);
        public static JArray HasArrayProperty(JObject parent, string child) =>
            (JArray)HasProperty(parent, child, JTokenType.Array);
        public static JArray? HasOptionalArrayProperty(JObject parent, string child) =>
            (JArray?)HasOptionalProperty(parent, child, JTokenType.Array);
        public static string HasStringProperty(JObject parent, string child) =>
            HasProperty(parent, child, JTokenType.String).Value<string>();
        public static string? HasOptionalStringProperty(JObject parent, string child) =>
            HasOptionalProperty(parent, child, JTokenType.String)?.Value<string>();
        static string PathOrRoot(this JToken token)
        {
            var path = token.Path;
            if (string.IsNullOrEmpty(path))
                return "(root)";
            return path;
        }
    }
}
