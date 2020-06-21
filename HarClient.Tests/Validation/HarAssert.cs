using HarSharp;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JBlam.HarClient.Tests.Validation
{
    static class HarAssert
    {
        public static void IsValid(Har har) =>
            IsValidLog(JAssert.HasObjectProperty(har.ToJObject(), "log"));
        static JObject ToJObject(this Har har) =>
            JObject.FromObject(har, new JsonSerializer
            {
                ContractResolver = HarMessageHandler.HarSerializerSettings.ContractResolver,
                DateFormatHandling = HarMessageHandler.HarSerializerSettings.DateFormatHandling,
                NullValueHandling = HarMessageHandler.HarSerializerSettings.NullValueHandling,
            });

        // Regex Copyright (c) 2007 Jan Odvarko
        // http://www.softwareishard.com/har/viewer/ (schema tab)
        internal static readonly Regex isoDatetime = new Regex(@"^(\d{4})(-)?(\d\d)(-)?(\d\d)(T)?(\d\d)(:)?(\d\d)(:)?(\d\d)(\.\d+)?(Z|([+-])(\d\d)(:)?(\d\d))");

        static void IsValidLog(JObject log)
        {
            _ = JAssert.HasStringProperty(log, "version");
            AssertIsValidCreatorBrowser(JAssert.HasObjectProperty(log, "creator"));
            if (JAssert.HasOptionalObjectProperty(log, "browser") is JObject browser)
                AssertIsValidCreatorBrowser(browser);
            var pageIds = AssertValidPages(
                JAssert.HasOptionalArrayProperty(log, "pages"));
            AssertValidEntries(
                JAssert.HasArrayProperty(log, "entries"), pageIds);
            _ = HasOptionalComment(log);
        }
        static void AssertIsValidCreatorBrowser(JObject creator)
        {
            _ = JAssert.HasStringProperty(creator, "name");
            _ = JAssert.HasStringProperty(creator, "version");
            _ = HasOptionalComment(creator);
        }
        static IReadOnlyCollection<string> AssertValidPages(JArray? pages)
        {
            if (pages is null)
                return Array.Empty<string>();
            var pageIds = new HashSet<string>();
            foreach (var member in pages)
            {
                Assert.IsTrue(
                    pageIds.Add(AssertIsValidPage(JAssert.IsObject(member))),
                    "Pages collection contained duplicate IDs");
            }
            return pageIds;

            static string AssertIsValidPage(JObject page)
            {
                StringAssert.Matches(
                    JAssert.HasStringProperty(page, "startedDateTime"),
                    isoDatetime);
                var pageId = JAssert.HasStringProperty(page, "id");
                _ = JAssert.HasStringProperty(page, "title");
                AssertIsPageTiming(JAssert.HasObjectProperty(page, "pageTimings"));
                _ = HasOptionalComment(page);
                return pageId;
            }
            static void AssertIsPageTiming(JObject pageTiming)
            {
                AssertHasOptionalNumberOrMinusOne(pageTiming, "onContentLoad");
                AssertHasOptionalNumberOrMinusOne(pageTiming, "onLoad");
                _ = HasOptionalComment(pageTiming);
            }
        }
        static void AssertValidEntries(JArray entries, IReadOnlyCollection<string> validRefs)
        {
            foreach (var member in entries)
            {
                var pageref = AssertIsValidEntry(JAssert.IsObject(member));
                if (pageref != null)
                    Assert.IsTrue(validRefs.Contains(pageref), "Entry refers to undefined page {0}", pageref);
            }

            static string? AssertIsValidEntry(JObject entry)
            {
                StringAssert.Matches(
                    JAssert.HasStringProperty(entry, "startedDateTime"),
                    isoDatetime);
                var time = JAssert.HasNumericProperty(entry, "time").Value<double>();
                AssertIsValidRequest(JAssert.HasObjectProperty(entry, "request"));
                AssertIsValidResponse(JAssert.HasObjectProperty(entry, "response"));
                AssertIsValidCache(JAssert.HasObjectProperty(entry, "cache"));
                var timings = AssertIsValidTimings(
                    JAssert.HasObjectProperty(entry, "timings"));
                if (timings.HasValue)
                    Assert.AreEqual(timings.Value, time, delta: 1.0, "Timings were not consistent with time property");
                _ = JAssert.HasOptionalStringProperty(entry, "serverIPAddress");
                _ = JAssert.HasOptionalStringProperty(entry, "connection");
                _ = HasOptionalComment(entry);
                return JAssert.HasOptionalStringProperty(entry, "pageref");
            }
            static double? AssertIsValidTimings(JObject timings)
            {
                _ = HasOptionalComment(timings);
                return new[]
                {
                    AssertHasOptionalNumberOrMinusOne(timings, "blocked"),
                    AssertHasOptionalNumberOrMinusOne(timings, "dns"),
                    AssertHasOptionalNumberOrMinusOne(timings, "connect"),
                    AssertIsNumberOrMinusOne(timings, "send"),
                    AssertIsNumberOrMinusOne(timings, "wait"),
                    AssertIsNumberOrMinusOne(timings, "receive"),
                    AssertHasOptionalNumberOrMinusOne(timings, "ssl"),
                }.Aggregate((u, v) => !u.HasValue ? v : u + v.GetValueOrDefault());
            }
            static void AssertIsValidRequest(JObject request)
            {
                _ = JAssert.HasStringProperty(request, "method");
                _ = JAssert.HasStringProperty(request, "url");
                _ = JAssert.HasStringProperty(request, "httpVersion");
                AssertIsValidCookies(JAssert.HasArrayProperty(request, "cookies"));
                AssertIsValidRecords(JAssert.HasArrayProperty(request, "headers"));
                AssertIsValidRecords(JAssert.HasArrayProperty(request, "queryString"));
                if (JAssert.HasOptionalObjectProperty(request, "postData") is JObject o)
                    AssertIsValidPostData(o);
                AssertIsNumberOrMinusOne(request, "headersSize");
                AssertIsNumberOrMinusOne(request, "bodySize");
                _ = HasOptionalComment(request);

                static void AssertIsValidPostData(JObject postData)
                {
                    _ = JAssert.HasStringProperty(postData, "mimeType");
                    foreach (var item in JAssert.HasArrayProperty(postData, "params"))
                    {
                        var p = JAssert.IsObject(item);
                        _ = JAssert.HasStringProperty(p, "name");
                        _ = JAssert.HasOptionalStringProperty(p, "value");
                        _ = JAssert.HasOptionalStringProperty(p, "fileName");
                        _ = JAssert.HasOptionalStringProperty(p, "contentType");
                        _ = HasOptionalComment(p);
                    }
                    _ = JAssert.HasStringProperty(postData, "text");
                    _ = HasOptionalComment(postData);
                }
            }

            static void AssertIsValidCache(JObject cache)
            {
                AssertIsValidCacheRecord(
                    JAssert.HasOptionalPropertyOrNull(cache, "beforeRequest", JTokenType.Object));
                AssertIsValidCacheRecord(
                    JAssert.HasOptionalPropertyOrNull(cache, "afterRequest", JTokenType.Object));
                _ = HasOptionalComment(cache);

                static void AssertIsValidCacheRecord(JToken? possibleCacheRecord)
                {
                    if (possibleCacheRecord is JObject cacheRecord)
                    {
                        if (JAssert.HasOptionalStringProperty(cacheRecord, "expires") is string expires)
                            StringAssert.Matches(expires, isoDatetime);
                        StringAssert.Matches(
                            JAssert.HasStringProperty(cacheRecord, "lastAccess"),
                            isoDatetime);
                        _ = JAssert.HasStringProperty(cacheRecord, "eTag");
                        _ = JAssert.HasNumericProperty(cacheRecord, "hitCount");
                        _ = HasOptionalComment(cacheRecord);
                    }
                }
            }
            static void AssertIsValidResponse(JObject response)
            {
                _ = JAssert.HasProperty(response, "status", JTokenType.Integer);
                _ = JAssert.HasStringProperty(response, "statusText");
                _ = JAssert.HasStringProperty(response, "httpVersion");
                AssertIsValidCookies(JAssert.HasArrayProperty(response, "cookies"));
                AssertIsValidRecords(JAssert.HasArrayProperty(response, "headers"));
                AssertIsValidContent(JAssert.HasObjectProperty(response, "content"));
                _ = JAssert.HasStringProperty(response, "redirectURL");
                AssertIsNumberOrMinusOne(response, "headersSize");
                AssertIsNumberOrMinusOne(response, "bodySize");
                _ = HasOptionalComment(response);

                static void AssertIsValidContent(JObject content)
                {
                    _ = JAssert.HasProperty(content, "size", JTokenType.Integer);
                    _ = JAssert.HasOptionalProperty(content, "compression", JTokenType.Integer);
                    _ = JAssert.HasStringProperty(content, "mimeType");
                    _ = JAssert.HasOptionalStringProperty(content, "text");
                    _ = HasOptionalComment(content);
                }
            }
        }
        static void AssertIsValidCookies(JArray cookies)
        {
            foreach (var item in cookies)
            {
                var cookie = JAssert.IsObject(item);
                _ = JAssert.HasStringProperty(cookie, "name");
                _ = JAssert.HasStringProperty(cookie, "value");
                _ = JAssert.HasOptionalStringProperty(cookie, "path");
                _ = JAssert.HasOptionalStringProperty(cookie, "domain");
                if (JAssert.HasOptionalStringProperty(cookie, "expires") is string s)
                    StringAssert.Matches(s, isoDatetime);
                _ = JAssert.HasOptionalProperty(cookie, "httpOnly", JTokenType.Boolean);
                _ = JAssert.HasOptionalProperty(cookie, "secure", JTokenType.Boolean);
                _ = HasOptionalComment(cookie);
            }
        }
        static void AssertIsValidRecords(JArray records)
        {
            foreach (var item in records)
            {
                var record = JAssert.IsObject(item);
                _ = JAssert.HasStringProperty(record, "name");
                _ = JAssert.HasStringProperty(record, "value");
                _ = HasOptionalComment(record);
            }
        }
        static string? HasOptionalComment(JObject o) => JAssert.HasOptionalStringProperty(o, "comment");
        static double? AssertHasOptionalNumberOrMinusOne(JObject parent, string child)
        {
            if (JAssert.HasOptionalNumericProperty(parent, child) is JValue jValue)
            {
                var value = jValue.Value<double>();
                if (value == -1)
                    return null;
                if (value >= 0)
                    return value;
                Assert.Fail("Optional property {0}.{1} was neither -1 nor >= 0", parent.Path, child);
            }
            return null;
        }
        static double? AssertIsNumberOrMinusOne(JObject parent, string child)
        {
            var value = JAssert.HasNumericProperty(parent, child).Value<double>();
            if (value == -1)
                return null;
            if (value >= 0)
                return value;
            throw new AssertFailedException($"Property {parent.Path}.{child} was neither -1 nor >= 0");
        }
    }
}
