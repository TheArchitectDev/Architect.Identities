using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class IdEncoderDecimalTests
	{
		private const decimal SampleId = 447835050025542181830910637m;
		private const string SampleAlphanumericString = "1drbWFYI4a3pLliX";
		private static readonly byte[] SampleAlphanumericBytes = Encoding.ASCII.GetBytes(SampleAlphanumericString);

		private static bool Throws(Action action)
		{
			try
			{
				action();
				return false;
			}
			catch
			{
				return true;
			}
		}

		private static bool[] CheckIfThrowsForAllEncodings(decimal id, byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => IdEncoder.GetAlphanumeric(id, bytes)),
				Throws(() => IdEncoder.GetAlphanumeric(id)),
			};
		}

		private static decimal?[] ResultForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetDecimal(bytes, out var id) ? id : -1m,
				IdEncoder.TryGetDecimal(chars, out id) ? id : -1m,
				IdEncoder.GetDecimalOrDefault(bytes),
				IdEncoder.GetDecimalOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetDecimal(bytes, out _),
				IdEncoder.TryGetDecimal(chars, out _),
				IdEncoder.GetDecimalOrDefault(bytes) is not null,
				IdEncoder.GetDecimalOrDefault(chars) is not null,
			};
		}

		[Fact]
		public void GetAlphanumeric_WithTooLongInput_ShouldSucceed()
		{
			IdEncoder.GetAlphanumeric(SampleId, stackalloc byte[100]);
		}

		[Fact]
		public void GetAlphanumeric_WithTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => IdEncoder.GetAlphanumeric(SampleId, new byte[15]));
		}

		[Fact]
		public void AllEncodingMethods_WithSign_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: true, scale: 0);
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => didThrow));
		}

		[Fact]
		public void AllEncodingMethods_WithNonzeroScale_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: false, scale: 1);
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => didThrow));
		}

		[Fact]
		public void AllEncodingMethods_WithOverflow_ShouldThrow()
		{
			var id = 1 + DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => didThrow));
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => !didThrow));
		}

		[Fact]
		public void GetAlphanumeric_WithIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = 2m;
			var three = (decimal)UInt64.MaxValue - 1;
			var four = (decimal)UInt64.MaxValue;
			var five = DistributedIdGenerator.MaxValue;

			var a = IdEncoder.GetAlphanumeric(one);
			var b = IdEncoder.GetAlphanumeric(two);
			var c = IdEncoder.GetAlphanumeric(three);
			var d = IdEncoder.GetAlphanumeric(four);
			var e = IdEncoder.GetAlphanumeric(five);

			var expectedOrder = new[] { a, b, c, d, e };
			var sortedOrder = new[] { d, a, c, b, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Theory]
		[InlineData(0, "0000000000000000")]
		[InlineData(1, "0000000000000001")]
		[InlineData(61, "000000000000000z")]
		[InlineData(62, "0000000000000010")]
		[InlineData(1UL << 32, "00000000004gfFC4")]
		[InlineData(1 + (1UL << 32), "00000000004gfFC5")]
		public void GetAlphanumeric_WithValue_ShouldReturnExpectedResult(decimal input, string expectedOutput)
		{
			var shortString = IdEncoder.GetAlphanumeric(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = IdEncoder.GetAlphanumeric(DistributedIdGenerator.MaxValue);

			Assert.Equal("agbFu5KnEQGxp4QB", shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithByteOutput_ShouldSucceed()
		{
			IdEncoder.GetAlphanumeric(SampleId, stackalloc byte[16]);
		}

		[Fact]
		public void GetAlphanumeric_WithStringReturnValue_ShouldSucceed()
		{
			_ = IdEncoder.GetAlphanumeric(SampleId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("61")]
		[InlineData("62")]
		[InlineData("447835050025542181830910637")]
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void GetAlphanumeric_WithValue_ShouldBeReversibleByAllDecoders(string idToString)
		{
			var id = Decimal.Parse(idToString, NumberStyles.None);
			var bytes = new byte[16];
			IdEncoder.GetAlphanumeric(id, bytes);

			var results = ResultForAllDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryGetDecimal_WithTooShortByteInput_ShouldFail()
		{
			var success = IdEncoder.TryGetDecimal(stackalloc byte[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetDecimal_WithTooShortCharInput_ShouldFail()
		{
			var success = IdEncoder.TryGetDecimal(stackalloc char[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetDecimal_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);

			var success = IdEncoder.TryGetDecimal(bytes, out var result);

			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void TryGetDecimal_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericString.AsSpan().CopyTo(chars);

			var success = IdEncoder.TryGetDecimal(chars, out var result);

			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Theory]
		[InlineData("1drbWFYI4a3pLliX", "447835050025542181830910637")] // Alphanumeric
		public void TryGetDecimal_Regularly_ShouldOutputExpectedResult(string input, string expectedResultString)
		{
			var expectedResult = Decimal.Parse(expectedResultString);
			var success = IdEncoder.TryGetDecimal(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
		[InlineData("agbFu5KnEQGxp4QB")]
		[InlineData("00000000004gfFC4")]
		[InlineData("00000000004gfFC5")]
		[InlineData("LygHa16AHYF")] // UInt64.MaxValue
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void GetDecimalOrDefault_Regularly_ShouldReturnSameResultAsTryGetDecimal(string input)
		{
			var expectedResult = IdEncoder.TryGetDecimal(input, out var expectedId)
				? expectedId
				: (decimal?)null;

			var result = IdEncoder.GetDecimalOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[16];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(15)]
		[InlineData(29)]
		[InlineData(64)]
		public void AllDecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData("123456789012345$")]
		[InlineData("1234567890123,00")]
		[InlineData("1234567890123.00")]
		[InlineData("+1234567890123456")]
		[InlineData("-1234567890123456")]
		[InlineData("123456789012345_")]
		public void AllDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Fact]
		public void TryGetDecimal_WithBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetDecimal(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetDecimal_WithChars_ShouldSucceed()
		{
			var success = IdEncoder.TryGetDecimal(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetDecimalOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetDecimalOrDefault(SampleAlphanumericBytes);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void GetDecimalOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetDecimalOrDefault(SampleAlphanumericString);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void TryGetDecimal_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetDecimal(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetDecimal_WithAlphanumericString_ShouldSucceed()
		{
			var success = IdEncoder.TryGetDecimal(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetDecimalOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetDecimalOrDefault(SampleAlphanumericBytes);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void GetDecimalOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetDecimalOrDefault(SampleAlphanumericString);
			Assert.Equal(SampleId, result);
		}
	}
}
