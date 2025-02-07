﻿using CoreEx.Http;
using My.Hr.Api;
using My.Hr.Api.Controllers;
using My.Hr.Business.Models;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace My.Hr.UnitTest
{
    [TestFixture]
    [Category("WithDB")]
    public class ReferenceDataControllerTest
    {
        [OneTimeSetUp]
        public Task Init() => EmployeeControllerTest.Init();

        [Test]
        public void A100_USState_All()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var v = test.Controller<ReferenceDataController>()
                .Run(c => c.USStateGetAll(null, null))
                .AssertOK()
                .GetValue<USState[]>()!;

            Assert.AreEqual(50, v.Length);
        }

        [Test]
        public void A110_USState_Codes()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var v = test.Controller<ReferenceDataController>()
                .Run(c => c.USStateGetAll(new string[] { "WA", "CO" }, null))
                .AssertOK()
                .GetValue<USState[]>()!;

            Assert.AreEqual(2, v.Length);
            Assert.AreEqual(new string[] { "CO", "WA" }, v.Select(x => x.Code));
        }

        [Test]
        public void A120_USState_Text()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var v = test.Controller<ReferenceDataController>()
                .Run(c => c.USStateGetAll(null, "*or*"))
                .AssertOK()
                .GetValue<USState[]>()!;

            Assert.AreEqual(8, v.Length);
            var x = v.Select(x => x.Code);
            Assert.AreEqual(new string[] { "CA", "CO", "FL", "GA", "NY", "NC", "ND", "OR" }, v.Select(x => x.Code));
        }

        [Test]
        public void A130_USState_FieldsAndNotModified()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var r = test.Controller<ReferenceDataController>()
                .Run(c => c.USStateGetAll(new string[] { "WA", "CO" }, null), new HttpRequestOptions().Include("code", "text"))
                .AssertOK()
                .AssertJson("[{\"code\":\"CO\",\"text\":\"Colorado\"},{\"code\":\"WA\",\"text\":\"Washington\"}]");

            test.Controller<ReferenceDataController>()
                .Run(c => c.USStateGetAll(new string[] { "WA", "CO" }, null), new HttpRequestOptions { ETag = r.Response?.Headers?.ETag?.Tag }.Include("code", "text"))
                .AssertNotModified();
        }

        [Test]
        public void B100_Gender_All()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var v = test.Controller<ReferenceDataController>()
                .Run(c => c.GenderGetAll(null, null))
                .AssertOK()
                .GetValue<Gender[]>()!;

            Assert.AreEqual(3, v.Length);
        }

        [Test]
        public void C100_Named()
        {
            using var test = ApiTester.Create<Startup>().UseJsonSerializer(new CoreEx.Text.Json.ReferenceDataContentJsonSerializer());

            var r = test.Controller<ReferenceDataController>()
                .Run(c => c.GetNamed(), new HttpRequestOptions { UrlQueryString = "gender&usstate" })
                .AssertOK();
        }
    }
}