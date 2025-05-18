using System;
using Microsoft.Extensions.Logging;
using Moq;
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
        [InlineData("any string", true)]
        [InlineData("", true)]
        public void ValidateString_AlwaysTrue(string value, bool expected)
        {
            var result = _validator.ValidateString(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123", "int", null, true)]
        [InlineData("abc", "int", null, false)]
        [InlineData("true", "bool", null, true)]
        [InlineData("nope", "bool", null, false)]
        [InlineData("2025-05-18", "datetime", new[] { "yyyy-MM-dd" }, true)]
        [InlineData("notadate", "datetime", new[] { "yyyy-MM-dd" }, false)]
        [InlineData("123.45", "double", null, true)]
        [InlineData("notadouble", "double", null, false)]
        [InlineData("123.45", "decimal", null, true)]
        [InlineData("notadecimal", "decimal", null, false)]
        [InlineData("any string", "string", null, true)]
        [InlineData("foo", "unknown", null, true)]
        public void ValidateType_Works(string value, string expectedType, string[] formats, bool expected)
        {
            var result = _validator.ValidateType(value, expectedType, formats ?? Array.Empty<string>());
            Assert.Equal(expected, result);
        }
    }
}
