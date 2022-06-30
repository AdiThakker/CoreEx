﻿using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.WebApis;
using DbEx.Migration;
using DbEx.Migration.Data;
using Microsoft.Extensions.Configuration;
using My.Hr.Business.Models;
using My.Hr.Functions;
using My.Hr.Functions.Functions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace My.Hr.UnitTest
{
    [TestFixture]
    [Category("WithDB")]
    public class EmployeeFunctionTest
    {
        [OneTimeSetUp]
        public async Task Init()
        {
            HttpConsts.IncludeFieldsQueryStringName = "include-fields";

            using var test = FunctionTester.Create<Startup>();
            var cs = test.Configuration.GetConnectionString("Database");
            if (await Database.Program.RunMigrator(cs, typeof(EmployeeControllerTest).Assembly, MigrationCommand.ResetAndAll.ToString()).ConfigureAwait(false) != 0)
                Assert.Fail("Database migration failed.");
        }

        [Test]
        public void A100_Get_NotFound()
        {
            using var test = FunctionTester.Create<Startup>();

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{404.ToGuid()}"), 404.ToGuid()))
                .AssertNotFound();
        }

        [Test]
        public void A110_Get_Found()
        {
            using var test = FunctionTester.Create<Startup>();

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{1.ToGuid()}"), 1.ToGuid()))
                .AssertOK()
                .Assert(new Employee
                {
                    Id = 1.ToGuid(),
                    Email = "w.jones@org.com",
                    FirstName = "Wendy",
                    LastName = "Jones",
                    Gender = "F",
                    Birthday = new DateTime(1985, 03, 18, 0, 0, 0, DateTimeKind.Unspecified),
                    StartDate = new DateTime(2000, 12, 11, 0, 0, 0, DateTimeKind.Unspecified),
                    PhoneNo = "(425) 612 8113"
                }, nameof(Employee.ETag));
        }

        [Test]
        public void A120_Get_NotModified()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{1.ToGuid()}"), 1.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{1.ToGuid()}", new CoreEx.Http.HttpRequestOptions { ETag = e.ETag }), 1.ToGuid()))
                .AssertNotModified();
        }

        [Test]
        public void A130_Get_IncludeFields()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{1.ToGuid()}", new CoreEx.Http.HttpRequestOptions().Include("FirstName", "LastName")), 1.ToGuid()))
                .AssertOK()
                .AssertJson("{\"firstName\":\"Wendy\",\"lastName\":\"Jones\"}");
        }

        [Test]
        public void B100_GetAll_All()
        {
            using var test = FunctionTester.Create<Startup>();

            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAllAsync(test.CreateHttpRequest(HttpMethod.Get, "api/employees")))
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNotNull(v?.Collection);
            Assert.AreEqual(4, v!.Collection.Count);
            Assert.AreEqual(new string[] { "Browne", "Jones", "Smith", "Smithers" }, v.Collection.Select(x => x.LastName).ToArray());
        }

        [Test]
        public void B110_GetAll_Paging()
        {
            using var test = FunctionTester.Create<Startup>();

            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAllAsync(test.CreateHttpRequest(HttpMethod.Get, "api/employees", new CoreEx.Http.HttpRequestOptions { Paging = PagingArgs.CreateSkipAndTake(1, 2, true) })))
                .AssertOK()
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNotNull(v?.Collection);
            Assert.AreEqual(2, v!.Collection.Count);
            Assert.AreEqual(new string[] { "Jones", "Smith" }, v.Collection.Select(x => x.LastName).ToArray());
            Assert.IsNotNull(v.Paging);
            Assert.AreEqual(4, v.Paging!.TotalCount);
        }

        [Test]
        public void B120_GetAll_PagingAndIncludeFields()
        {
            using var test = FunctionTester.Create<Startup>();

            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAllAsync(test.CreateHttpRequest(HttpMethod.Get, "api/employees", new CoreEx.Http.HttpRequestOptions { Paging = PagingArgs.CreateSkipAndTake(1, 2, false) }.Include("lastname"))))
                .AssertOK()
                .AssertJson("[ { \"lastName\": \"Jones\" }, { \"lastName\": \"Smith\" } ]")
                .GetValue<EmployeeCollectionResult>();

            Assert.IsNull(v!.Paging!.TotalCount); // No count requested.
        }

        [Test]
        public void C100_Create_Error()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.HttpTrigger<EmployeeFunction>()
                .Run(c => c.CreateAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "api/employees", e)))
                .AssertErrors(
                    new ApiError("Email", "'Email' must not be empty."));
        }

        [Test]
        public void C110_Create_Success()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                Email = "rs@email.com",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(c => c.CreateAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "api/employees", e)))
                .AssertCreated()
                .Assert(e, "Id", "ETag")
                .AssertLocationHeader<Employee>(v => new Uri($"api/employees/{v!.Id}", UriKind.Relative))
                .GetValue<Employee>();

            // Do a GET to make sure it is in the database and all fields equal.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{v!.Id}"), v.Id))
                .AssertOK()
                .Assert(v);
        }

        [Test]
        public void D100_Update_Error()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.UpdateAsync(test.CreateJsonHttpRequest(HttpMethod.Put, $"api/employees/{404.ToGuid()}", e), 404.ToGuid()))
                .AssertErrors(
                    new ApiError("Email", "'Email' must not be empty."));
        }

        [Test]
        public void D110_Update_NotFound()
        {
            using var test = FunctionTester.Create<Startup>();

            var e = new Employee
            {
                FirstName = "Rebecca",
                LastName = "Smythe",
                Birthday = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
                Gender = "M",
                PhoneNo = "555 123 4567",
                Email = "rs@email.com",
                StartDate = new DateTime(2020, 01, 08, 0, 0, 0, DateTimeKind.Unspecified)
            };

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.UpdateAsync(test.CreateJsonHttpRequest(HttpMethod.Put, $"api/employees/{404.ToGuid()}", e), 404.ToGuid()))
                .AssertNotFound();
        }

        [Test]
        public void D120_Update_Success()
        {
            using var test = FunctionTester.Create<Startup>();

            // Get current.
            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Update it.
            v.FirstName += "X";

            v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.UpdateAsync(test.CreateJsonHttpRequest(HttpMethod.Put, $"api/employees/{v.Id}", v), v.Id))
                .AssertOK()
                .Assert(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{v.Id}"), v.Id))
                .AssertOK()
                .Assert(v);
        }

        [Test]
        public void D130_Update_ConcurrencyError()
        {
            using var test = FunctionTester.Create<Startup>();

            // Get current.
            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Update it with errant etag.
            v.FirstName += "X";
            v.ETag = "ZZZZZZZZZZZZ";

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.UpdateAsync(test.CreateJsonHttpRequest(HttpMethod.Put, $"api/employees/{v.Id}", v), v.Id))
                .AssertPreconditionFailed();
        }

        [Test]
        public void E100_Delete()
        {
            using var test = FunctionTester.Create<Startup>();

            // Get current.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertOK();

            // Delete it.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.DeleteAsync(test.CreateHttpRequest(HttpMethod.Delete, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertNoContent();

            // Must not exist.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertNotFound();

            // Delete it again; should appear as if deleted as operation is considered idempotent.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.DeleteAsync(test.CreateHttpRequest(HttpMethod.Delete, $"api/employees/{2.ToGuid()}"), 2.ToGuid()))
                .AssertNoContent();
        }

        [Test]
        public void F100_Patch_NotFound()
        {
            using var test = FunctionTester.Create<Startup>();

            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.PatchAsync(test.CreateHttpRequest(HttpMethod.Patch, $"api/employees/{404.ToGuid()}", "{}", HttpConsts.MergePatchMediaTypeName), 404.ToGuid()))
                .AssertNotFound();
        }

        [Test]
        public void F110_Patch_Concurrency()
        {
            using var test = FunctionTester.Create<Startup>();

            // Get current.
            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{4.ToGuid()}"), 4.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Patch it with errant etag.
            v.FirstName += "X";

            var req = test.CreateHttpRequest(HttpMethod.Patch, $"api/employees/{v.Id}", $"{{ \"firstName\": \"{v.FirstName}\" }}", new CoreEx.Http.HttpRequestOptions { ETag = "ZZZZZZZZZZZZ" }, HttpConsts.MergePatchMediaTypeName);
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.PatchAsync(req, v.Id))
                .AssertPreconditionFailed();
        }

        [Test]
        public void F120_Patch()
        {
            using var test = FunctionTester.Create<Startup>();

            // Get current.
            var v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{4.ToGuid()}"), 4.ToGuid()))
                .AssertOK()
                .GetValue<Employee>()!;

            // Patch it with errant etag.
            v.FirstName += "X";

            var req = test.CreateHttpRequest(HttpMethod.Patch, $"api/employees/{v.Id}", $"{{ \"firstName\": \"{v.FirstName}\" }}", new CoreEx.Http.HttpRequestOptions { ETag = v.ETag }, HttpConsts.MergePatchMediaTypeName);
            v = test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.PatchAsync(req, v.Id))
                .AssertOK()
                .Assert(v, "ETag")
                .GetValue<Employee>()!;

            // Get again and check all.
            test.HttpTrigger<EmployeeFunction>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, $"api/employees/{v.Id}"), v.Id))
                .AssertOK()
                .Assert(v);
        }
    }
}