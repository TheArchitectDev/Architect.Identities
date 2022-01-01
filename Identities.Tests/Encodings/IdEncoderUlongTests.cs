using System.Text;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class IdEncoderUlongTests
	{
		private const ulong SampleId = 1234567890123456789UL;
		private const string SampleAlphanumericString = "1TCKi1nFuNh";
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

		private static bool[] CheckIfThrowsForAllEncodings(ulong id, byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => IdEncoder.GetAlphanumeric(id, bytes)),
				Throws(() => IdEncoder.GetAlphanumeric(id)),
			};
		}

		private static ulong?[] ResultForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetUlong(bytes, out var id) ? id : (ulong?)null,
				IdEncoder.TryGetUlong(chars, out id) ? id : (ulong?)null,
				IdEncoder.GetUlongOrDefault(bytes),
				IdEncoder.GetUlongOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetUlong(bytes, out _),
				IdEncoder.TryGetUlong(chars, out _),
				IdEncoder.GetUlongOrDefault(bytes) is not null,
				IdEncoder.GetUlongOrDefault(chars) is not null,
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
			Assert.Throws<IndexOutOfRangeException>(() => IdEncoder.GetAlphanumeric(SampleId, new byte[10]));
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = UInt64.MaxValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[11]);
			Assert.Equal(results.Length, results.Count(didThrow => !didThrow));
		}

		[Fact]
		public void GetAlphanumeric_WithIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1UL;
			var two = 2UL;
			var three = (ulong)UInt32.MaxValue;
			var four = UInt64.MaxValue - 1;
			var five = UInt64.MaxValue;

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
		[InlineData(0, "00000000000")]
		[InlineData(1, "00000000001")]
		[InlineData(61, "0000000000z")]
		[InlineData(62, "00000000010")]
		[InlineData(1UL << 32, "000004gfFC4")]
		[InlineData(1 + (1UL << 32), "000004gfFC5")]
		public void GetAlphanumeric_WithValue_ShouldReturnExpectedResult(ulong input, string expectedOutput)
		{
			var shortString = IdEncoder.GetAlphanumeric(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = IdEncoder.GetAlphanumeric(UInt64.MaxValue);

			Assert.Equal("LygHa16AHYF", shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithByteOutput_ShouldSucceed()
		{
			IdEncoder.GetAlphanumeric(SampleId, stackalloc byte[11]);
		}

		[Fact]
		public void GetAlphanumeric_WithStringReturnValue_ShouldSucceed()
		{
			_ = IdEncoder.GetAlphanumeric(SampleId);
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void GetAlphanumeric_WithValue_ShouldBeReversibleByAllDecoders(ulong id)
		{
			var bytes = new byte[11];
			IdEncoder.GetAlphanumeric(id, bytes);

			var results = ResultForAllDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryGetUlong_WithTooShortByteInput_ShouldFail()
		{
			var success = IdEncoder.TryGetUlong(stackalloc byte[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetUlong_WithTooShortCharInput_ShouldFail()
		{
			var success = IdEncoder.TryGetUlong(stackalloc char[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetUlong_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);

			var success = IdEncoder.TryGetUlong(bytes, out var result); // Cannot know if input is alphanumeric or numeric

			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void TryGetUlong_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericString.AsSpan().CopyTo(chars);

			var success = IdEncoder.TryGetUlong(chars, out var result); // Cannot know if input is alphanumeric or numeric

			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Theory]
		[InlineData("1TCKi1nFuNh", 1234567890123456789)] // Alphanumeric
		[InlineData("LygHa16AHYF", UInt64.MaxValue)] // Alphanumeric
		public void TryGetUlong_Regularly_ShouldOutputExpectedResult(string input, ulong expectedResult)
		{
			var success = IdEncoder.TryGetUlong(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
		[InlineData("00000000001")]
		[InlineData("000004gfFC4")]
		[InlineData("000004gfFC5")]
		[InlineData("LygHa16AHYF")] // UInt64.MaxValue
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void GetUlongOrDefault_Regularly_ShouldReturnSameResultAsTryGetUlong(string input)
		{
			var expectedResult = IdEncoder.TryGetUlong(input, out var expectedId)
				? expectedId
				: (ulong?)null;

			var result = IdEncoder.GetUlongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[11];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(10)]
		[InlineData(21)]
		[InlineData(64)]
		public void AllDecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData("1234567890$")]
		[InlineData("12345678,00")]
		[InlineData("12345678.00")]
		[InlineData("+12345678901")]
		[InlineData("-12345678901")]
		[InlineData("1234567890_")]
		public void AllDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Fact]
		public void TryGetUlong_WithBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetUlong(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetUlong_WithChars_ShouldSucceed()
		{
			var success = IdEncoder.TryGetUlong(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetUlongOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetUlongOrDefault(SampleAlphanumericBytes);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void GetUlongOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetUlongOrDefault(SampleAlphanumericString);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void TryGetUlong_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetUlong(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetUlong_WithAlphanumericString_ShouldSucceed()
		{
			var success = IdEncoder.TryGetUlong(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetUlongOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetUlongOrDefault(SampleAlphanumericBytes);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void GetUlongOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetUlongOrDefault(SampleAlphanumericString);
			Assert.Equal(SampleId, result);
		}
	}
}
