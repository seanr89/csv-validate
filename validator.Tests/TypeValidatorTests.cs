using System;
using Microsoft.Extensions.Logging;
using Moq;
using validator.Services;
using Xunit;

namespace validator.Tests
{
    public class TypeValidatorTests
    {
        private readonly Mock<ILogger<TypeValidator>> _mockLogger;
        private readonly TypeValidator _validator;

        public TypeValidatorTests()
        {
            _mockLogger = new Mock<ILogger<TypeValidator>>();
            _validator = new TypeValidator(_mockLogger.Object);
        }

        [Fact]
        public void Validation_IsIndependentOfCurrentCulture()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

                Assert.True(_validator.ValidateDecimal("1234.56"));
                Assert.True(_validator.ValidateDecimal("65.25"));
                Assert.True(_validator.ValidateDouble("1.5"));
                Assert.False(_validator.ValidateDecimal("1,5"));
                Assert.False(_validator.ValidateDouble("1,5"));
                Assert.False(_validator.ValidateDecimal("1.234,56"));
                Assert.True(_validator.ValidateDate("18/05/2025", new[] { "dd/MM/yyyy" }));
                Assert.True(_validator.ValidateDateTime("05/18/2025 10:30:00", new[] { "MM/dd/yyyy HH:mm:ss" }));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateType_ReturnsFalse_WhenExpectedTypeIsNullOrWhitespace(string? expectedType)
        {
            Assert.False(_validator.ValidateType("123", expectedType!, null));
        }

        [Theory]
        [InlineData("123", true)]
        [InlineData("abc", false)]
        public void ValidateInt_Works(string value, bool expected)
        {
            var result = _validator.ValidateInt(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", true)]
        [InlineData("notabool", false)]
        public void ValidateBool_Works(string value, bool expected)
        {
            var result = _validator.ValidateBool(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2025-05-18", new[] { "yyyy-MM-dd" }, true)]
        [InlineData("18/05/2025", new[] { "dd/MM/yyyy" }, true)]
        [InlineData("05-18-2025", new[] { "yyyy-MM-dd" }, false)]
        public void ValidateDateTime_Works(string value, string[] formats, bool expected)
        {
            var result = _validator.ValidateDateTime(value, formats);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2025-05-18", null, true)]
        [InlineData("05/18/2025", null, true)]
        [InlineData("2025-05-18 10:30:00", null, false)]
        [InlineData("18/05/2025", new[] { "dd/MM/yyyy" }, true)]
        [InlineData("2025-05-18", new[] { "dd/MM/yyyy" }, false)]
        public void ValidateDate_Works(string value, string[]? formats, bool expected)
        {
            var result = _validator.ValidateDate(value, formats);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123.45", true)]
        [InlineData("notadouble", false)]
        public void ValidateDouble_Works(string value, bool expected)
        {
            var result = _validator.ValidateDouble(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123.45", true)]
        [InlineData("notadecimal", false)]
        public void ValidateDecimal_Works(string value, bool expected)
        {
            var result = _validator.ValidateDecimal(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123", "int", null, true)]
        [InlineData("abc", "int", null, false)]
        [InlineData("true", "bool", null, true)]
        [InlineData("nope", "bool", null, false)]
        [InlineData("2025-05-18", "datetime", new[] { "yyyy-MM-dd" }, true)]
        [InlineData("notadate", "datetime", new[] { "yyyy-MM-dd" }, false)]
        [InlineData("2025-05-18", "date", null, true)]
        [InlineData("2025-05-18 10:30:00", "date", null, false)]
        [InlineData("notadate", "date", null, false)]
        [InlineData("123.45", "double", null, true)]
        [InlineData("notadouble", "double", null, false)]
        [InlineData("123.45", "decimal", null, true)]
        [InlineData("notadecimal", "decimal", null, false)]
        [InlineData("any string", "string", null, true)]
        [InlineData("foo", "unknown", null, false)]
        public void ValidateType_Works(string value, string expectedType, string[]? formats, bool expected)
        {
            var result = _validator.ValidateType(value, expectedType, formats);
            Assert.Equal(expected, result);
        }
    }
}
