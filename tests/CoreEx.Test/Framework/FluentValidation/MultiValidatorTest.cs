﻿using CoreEx.Entities;
using CoreEx.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.FluentValidation
{
    [TestFixture]
    public class MultiValidatorTest
    {
        [Test]
        public async Task FluentError()
        {
            var r = await MultiValidator.Create()
                .Add(new ProductValidator().Wrap(), new Product { Id = "B2TF", Name = "DeLorean", Price = 88m })
                .ValidateAsync().ConfigureAwait(false);

            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages.Count);
            Assert.AreEqual("A DeLorean cannot be priced at 88 as that could cause a chain reaction that would unravel the very fabric of the space-time continuum and destroy the entire universe.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("Name", r.Messages[0].Property);
        }
    }
}