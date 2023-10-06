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
			var expectedResult = AlphanumericIdEncoder.Encode(id);

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
			AlphanumericIdEncoder.Encode(id, expectedOutput);

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
			var expectedResult = AlphanumericIdEncoder.Encode(id);

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
			AlphanumericIdEncoder.Encode(id, expectedOutput);

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
			var expectedResult = AlphanumericIdEncoder.Encode(id);

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
			AlphanumericIdEncoder.Encode(id, expectedOutput);

			Span<byte> output = stackalloc byte[16];
			id.ToAlphanumeric(output);

			Assert.True(output.SequenceEqual(expectedOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithGuid_ShouldMatchIdEncoderResult(decimal id)
		{
			var guid = AlphanumericIdEncoderTests.Guid(id);

			var expectedResult = AlphanumericIdEncoder.Encode(guid);

			var result = guid.ToAlphanumeric();

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void ToAlphanumeric_WithGuidAndByteOutput_ShouldMatchIdEncoderResult(decimal id)
		{
			var guid = AlphanumericIdEncoderTests.Guid(id);

			Span<byte> expectedOutput = stackalloc byte[22];
			AlphanumericIdEncoder.Encode(guid, expectedOutput);

			Span<byte> output = stackalloc byte[22];
			guid.ToAlphanumeric(output);

			Assert.True(output.SequenceEqual(expectedOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void ToHexadecimal_Regularly_ShouldMatchIdEncoderResult(decimal id)
		{
			var expectedLongResult = HexadecimalIdEncoder.Encode((long)id);
			var expectedUlongResult = HexadecimalIdEncoder.Encode((ulong)id);
			var expectedDecimalResult = HexadecimalIdEncoder.Encode(id);
			var expectedGuidResult = HexadecimalIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id));

			var longResult = ((long)id).ToHexadecimal();
			var ulongResult = ((ulong)id).ToHexadecimal();
			var decimalResult = id.ToHexadecimal();
			var guidResult = AlphanumericIdEncoderTests.Guid(id).ToHexadecimal();

			Assert.Equal(expectedLongResult, longResult);
			Assert.Equal(expectedUlongResult, ulongResult);
			Assert.Equal(expectedDecimalResult, decimalResult);
			Assert.Equal(expectedGuidResult, guidResult);

			var longResultBytes = new byte[16];
			var ulongResultBytes = new byte[16];
			var decimalResultBytes = new byte[26];
			var guidResultBytes = new byte[32];
			((long)id).ToHexadecimal(longResultBytes);
			((ulong)id).ToHexadecimal(ulongResultBytes);
			id.ToHexadecimal(decimalResultBytes);
			AlphanumericIdEncoderTests.Guid(id).ToHexadecimal(guidResultBytes);

			Assert.Equal(expectedLongResult.Select(chr => (byte)chr), longResultBytes);
			Assert.Equal(expectedUlongResult.Select(chr => (byte)chr), ulongResultBytes);
			Assert.Equal(expectedDecimalResult.Select(chr => (byte)chr), decimalResultBytes);
			Assert.Equal(expectedGuidResult.Select(chr => (byte)chr), guidResultBytes);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("2147483647")] // Int32.MaxValue
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void ToUInt128_Regularly_ShouldReverseToGuid(string uint128IdString)
		{
			var id = UInt128.Parse(uint128IdString);

			var guid = IdEncodingExtensions.ToGuid(id);
			var reversed = IdEncodingExtensions.ToUInt128(guid);

			Assert.Equal(id, reversed);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("2147483647")] // Int32.MaxValue
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void ToGuid_Regularly_ShouldHaveSameBinaryEncoding(string uint128IdString)
		{
			var id = UInt128.Parse(uint128IdString);
			var guid = IdEncodingExtensions.ToGuid(id);

			var expectedBinaryRepresentation = BinaryIdEncoder.Encode(id);
			var binaryRepresentation = BinaryIdEncoder.Encode(guid);

			Assert.Equal(expectedBinaryRepresentation, binaryRepresentation);
		}
	}
}
