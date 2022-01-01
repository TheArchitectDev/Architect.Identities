using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class IdEncodingExtensionTests
	{
		[Fact]
		public void ToAlphanumeric_WithNegativeLong_ShouldThrow()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => (-1L).ToAlphanumeric());
		}

		[Fact]
		public void ToAlphanumeric_WithNegativeDecimal_ShouldThrow()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => (-1m).ToAlphanumeric());
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void ToAlphanumeric_WithLong_ShouldMatchIdEncoderResult(long id)
		{
			var expectedResult = IdEncoder.GetAlphanumeric(id);

			var result = id.ToAlphanumeric();

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void ToAlphanumeric_WithLongAndByteOutput_ShouldMatchIdEncoderResult(long id)
		{
			Span<byte> expectedOutput = stackalloc byte[11];
			IdEncoder.GetAlphanumeric(id, expectedOutput);

			Span<byte> output = stackalloc byte[11];
			id.ToAlphanumeric(output);

			Assert.True(output.SequenceEqual(expectedOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithUlong_ShouldMatchIdEncoderResult(ulong id)
		{
			var expectedResult = IdEncoder.GetAlphanumeric(id);

			var result = id.ToAlphanumeric();

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithUlongAndByteOutput_ShouldMatchIdEncoderResult(ulong id)
		{
			Span<byte> expectedOutput = stackalloc byte[11];
			IdEncoder.GetAlphanumeric(id, expectedOutput);

			Span<byte> output = stackalloc byte[11];
			id.ToAlphanumeric(output);

			Assert.True(output.SequenceEqual(expectedOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithDecimal_ShouldMatchIdEncoderResult(decimal id)
		{
			var expectedResult = IdEncoder.GetAlphanumeric(id);

			var result = id.ToAlphanumeric();

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithDecimalAndByteOutput_ShouldMatchIdEncoderResult(decimal id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			IdEncoder.GetAlphanumeric(id, expectedOutput);

			Span<byte> output = stackalloc byte[16];
			id.ToAlphanumeric(output);

			Assert.True(output.SequenceEqual(expectedOutput));
		}
	}
}
