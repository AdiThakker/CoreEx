﻿using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class ValueValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Run_With_NotNull()
        {
            string name = "George";
            var r = await name.Validate().Mandatory().String(50).ValidateAsync();
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasErrors);
        }

        [Test]
        public async Task Run_With_Null()
        {
            string? name = null;
            var r = await name.Validate().Mandatory().String(50).ValidateAsync();
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Value is required.", r.Messages[0].Text);
        }
    }
}