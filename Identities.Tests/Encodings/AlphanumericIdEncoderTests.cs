using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Architect.Identities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public class AlphanumericIdEncoderTests
	{
		private const ulong SampleUlongId = 1234567890123456789UL;
		private const string SampleAlphanumericStringOfUlongId = "1TCKi1nFuNh";
		private static readonly byte[] SampleAlphanumericBytesOfUlongId = Encoding.ASCII.GetBytes(SampleAlphanumericStringOfUlongId);

		private const decimal SampleDecimalId = 447835050025542181830910637m;
		private const string SampleAlphanumericStringOfDecimalId = "1drbWFYI4a3pLliX";
		private static readonly byte[] SampleAlphanumericBytesOfDecimalId = Encoding.ASCII.GetBytes(SampleAlphanumericStringOfDecimalId);

		private const decimal SampleGuidIdInDecimalForm = 1234567890123456789012345678m;
		private static readonly Guid SampleGuidId = Guid(SampleGuidIdInDecimalForm);
		private const string SampleAlphanumericStringOfGuidId = "0000004WoWZ9OjHPSzq3Ju";
		private static readonly byte[] SampleAlphanumericBytesOfGuidId = Encoding.ASCII.GetBytes(SampleAlphanumericStringOfGuidId);

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

		#region Long

		[Theory]
		[InlineData(-1L)]
		[InlineData(Int64.MinValue)]
		public void Encode_WithLongInputThatIsNegative_ShouldThrow(long negativeValue)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => AlphanumericIdEncoder.Encode(negativeValue));
			Assert.Throws<ArgumentOutOfRangeException>(() => AlphanumericIdEncoder.Encode(negativeValue, stackalloc byte[11]));
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void Encode_WithLongInputAndStringOutput_ShouldReturnSameResultAsForUlong(long id)
		{
			var expectedResult = AlphanumericIdEncoder.Encode((ulong)id);

			var result = AlphanumericIdEncoder.Encode(id);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void Encode_WithLongInputAndByteOutput_ShouldReturnSameResultAsWithStringOutput(long id)
		{
			Span<byte> expectedResult = stackalloc byte[11];
			var expectedResultString = AlphanumericIdEncoder.Encode(id);
			for (var i = 0; i < expectedResult.Length; i++)
				expectedResult[i] = (byte)expectedResultString[i];

			Span<byte> result = stackalloc byte[11];
			AlphanumericIdEncoder.Encode(id, result);

			Assert.True(result.SequenceEqual(expectedResult));
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfUlongId)]
		[InlineData("00000000001")]
		[InlineData("000004gfFC4")]
		[InlineData("000004gfFC5")]
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void TryDecodeLong_WithCharInput_ShouldReturnSameResultAsTryDecodeUlong(string input)
		{
			var expectedSuccess = AlphanumericIdEncoder.TryDecodeUlong(input, out var expectedResult);

			var success = AlphanumericIdEncoder.TryDecodeLong(input, out var result);
			Assert.Equal(expectedSuccess, success);
			Assert.True(result >= 0L);
			Assert.Equal(expectedResult, (ulong)result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfUlongId)]
		[InlineData("00000000001")]
		[InlineData("000004gfFC4")]
		[InlineData("000004gfFC5")]
		[InlineData("AzL8n0Y58m7")] // Int64.MaxValue
		[InlineData("LygHa16AHYF")] // UInt64.MaxValue
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void TryDecodeLong_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedSuccess = AlphanumericIdEncoder.TryDecodeLong(inputString, out var expectedResult);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var success = AlphanumericIdEncoder.TryDecodeLong(input, out var result);

			Assert.Equal(expectedSuccess, success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfUlongId)]
		[InlineData("00000000001")]
		[InlineData("000004gfFC4")]
		[InlineData("000004gfFC5")]
		[InlineData("AzL8n0Y58m7")] // Int64.MaxValue
		[InlineData("LygHa16AHYF")] // UInt64.MaxValue
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void DecodeLongOrDefault_WithCharInput_ShouldReturnSameResultAsTryDecodeLong(string input)
		{
			var expectedResult = AlphanumericIdEncoder.TryDecodeLong(input, out var expectedId)
				? expectedId
				: (long?)null;

			var result = AlphanumericIdEncoder.DecodeLongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfUlongId)]
		[InlineData("00000000001")]
		[InlineData("000004gfFC4")]
		[InlineData("000004gfFC5")]
		[InlineData("AzL8n0Y58m7")] // Int64.MaxValue
		[InlineData("LygHa16AHYF")] // UInt64.MaxValue
		[InlineData("LygHa16AHYG")] // UInt64.MaxValue + 1 invalid
		[InlineData("1234567890$")] // Invalid char
		[InlineData("12345678,00")] // Invalid char
		[InlineData("12345678.00")] // Invalid char
		[InlineData("+12345678901")] // Invalid char
		[InlineData("-12345678901")] // Invalid char
		[InlineData("1234567890_")] // Invalid char
		public void DecodeLongOrDefault_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedResult = AlphanumericIdEncoder.DecodeLongOrDefault(inputString);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var result = AlphanumericIdEncoder.DecodeLongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		#endregion

		#region Ulong

		private static bool[] CheckIfThrowsForAllUlongEncodings(ulong id, byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => AlphanumericIdEncoder.Encode(id, bytes)),
				Throws(() => AlphanumericIdEncoder.Encode(id)),
			};
		}

		private static ulong?[] ResultForAllUlongDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeUlong(bytes, out var id) ? id : null,
				AlphanumericIdEncoder.TryDecodeUlong(chars, out id) ? id : null,
				AlphanumericIdEncoder.DecodeUlongOrDefault(bytes),
				AlphanumericIdEncoder.DecodeUlongOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllUlongDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++)
				chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeUlong(bytes, out _),
				AlphanumericIdEncoder.TryDecodeUlong(chars, out _),
				AlphanumericIdEncoder.DecodeUlongOrDefault(bytes) is not null,
				AlphanumericIdEncoder.DecodeUlongOrDefault(chars) is not null,
			};
		}

		[Fact]
		public void Encode_WithUlongAndTooLongInput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleUlongId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithUlongAndTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => AlphanumericIdEncoder.Encode(SampleUlongId, new byte[10]));
		}

		[Fact]
		public void AllUlongEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = UInt64.MaxValue;
			var results = CheckIfThrowsForAllUlongEncodings(id, new byte[11]);
			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void Encode_WithUlongAndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1UL;
			var two = 2UL;
			var three = (ulong)UInt32.MaxValue;
			var four = UInt64.MaxValue - 1;
			var five = UInt64.MaxValue;

			var a = AlphanumericIdEncoder.Encode(one);
			var b = AlphanumericIdEncoder.Encode(two);
			var c = AlphanumericIdEncoder.Encode(three);
			var d = AlphanumericIdEncoder.Encode(four);
			var e = AlphanumericIdEncoder.Encode(five);

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
		public void Encode_WithUlongAndValue_ShouldReturnExpectedResult(ulong input, string expectedOutput)
		{
			var shortString = AlphanumericIdEncoder.Encode(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void Encode_WithUlongAndMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = AlphanumericIdEncoder.Encode(UInt64.MaxValue);

			Assert.Equal("LygHa16AHYF", shortString);
		}

		[Fact]
		public void Encode_WithUlongAndByteOutput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleUlongId, stackalloc byte[11]);
		}

		[Fact]
		public void Encode_WithUlongAndStringReturnValue_ShouldSucceed()
		{
			_ = AlphanumericIdEncoder.Encode(SampleUlongId);
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void Encode_WithUlongAndValue_ShouldBeReversibleByAllDecoders(ulong id)
		{
			var bytes = new byte[11];
			AlphanumericIdEncoder.Encode(id, bytes);

			var results = ResultForAllUlongDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryDecodeUlong_WithTooShortByteInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(stackalloc byte[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUlong_WithTooShortCharInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(stackalloc char[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUlong_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytesOfUlongId.AsSpan().CopyTo(bytes);

			var success = AlphanumericIdEncoder.TryDecodeUlong(bytes, out var result);

			Assert.True(success);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void TryDecodeUlong_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericStringOfUlongId.AsSpan().CopyTo(chars);

			var success = AlphanumericIdEncoder.TryDecodeUlong(chars, out var result);

			Assert.True(success);
			Assert.Equal(SampleUlongId, result);
		}

		[Theory]
		[InlineData("1TCKi1nFuNh", 1234567890123456789)]
		[InlineData("LygHa16AHYF", UInt64.MaxValue)]
		public void TryDecodeUlong_Regularly_ShouldOutputExpectedResult(string input, ulong expectedResult)
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfUlongId)]
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
		public void DecodeUlongOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeUlong(string input)
		{
			var expectedResult = AlphanumericIdEncoder.TryDecodeUlong(input, out var expectedId)
				? expectedId
				: (ulong?)null;

			var result = AlphanumericIdEncoder.DecodeUlongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllUlongDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[11];
			SampleAlphanumericBytesOfUlongId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllUlongDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(10)]
		[InlineData(21)]
		[InlineData(64)]
		public void AllUlongDecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllUlongDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Theory]
		[InlineData("1234567890$")]
		[InlineData("12345678,00")]
		[InlineData("12345678.00")]
		[InlineData("+12345678901")]
		[InlineData("-12345678901")]
		[InlineData("1234567890_")]
		public void AllUlongDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllUlongDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void TryDecodeUlong_WithBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(SampleAlphanumericBytesOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUlong_WithChars_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(SampleAlphanumericStringOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeUlongOrDefault(SampleAlphanumericBytesOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeUlongOrDefault(SampleAlphanumericStringOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void TryDecodeUlong_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(SampleAlphanumericBytesOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUlong_WithAlphanumericString_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeUlong(SampleAlphanumericStringOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeUlongOrDefault(SampleAlphanumericBytesOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeUlongOrDefault(SampleAlphanumericStringOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		#endregion

		#region Decimal
		
		private static bool[] CheckIfThrowsForAllDecimalEncodings(decimal id, byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => AlphanumericIdEncoder.Encode(id, bytes)),
				Throws(() => AlphanumericIdEncoder.Encode(id)),
			};
		}

		private static decimal?[] ResultForAllDecimalDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeDecimal(bytes, out var id) ? id : -1m,
				AlphanumericIdEncoder.TryDecodeDecimal(chars, out id) ? id : -1m,
				AlphanumericIdEncoder.DecodeDecimalOrDefault(bytes),
				AlphanumericIdEncoder.DecodeDecimalOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecimalDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeDecimal(bytes, out _),
				AlphanumericIdEncoder.TryDecodeDecimal(chars, out _),
				AlphanumericIdEncoder.DecodeDecimalOrDefault(bytes) is not null,
				AlphanumericIdEncoder.DecodeDecimalOrDefault(chars) is not null,
			};
		}

		[Fact]
		public void Encode_WithDecimalAndTooLongInput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleDecimalId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithDecimalAndTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => AlphanumericIdEncoder.Encode(SampleDecimalId, new byte[15]));
		}

		[Fact]
		public void AllEncodingMethods_WithSign_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: true, scale: 0);
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[16]);
			Assert.All(results, result => Assert.True(result));
		}

		[Fact]
		public void AllEncodingMethods_WithNonzeroScale_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: false, scale: 1);
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[16]);
			Assert.All(results, result => Assert.True(result));
		}

		[Fact]
		public void AllEncodingMethods_WithOverflow_ShouldThrow()
		{
			var id = 1 + DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[16]);
			Assert.All(results, result => Assert.True(result));
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[16]);
			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void Encode_WithDecimalAndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = 2m;
			var three = (decimal)UInt64.MaxValue - 1;
			var four = (decimal)UInt64.MaxValue;
			var five = DistributedIdGenerator.MaxValue;

			var a = AlphanumericIdEncoder.Encode(one);
			var b = AlphanumericIdEncoder.Encode(two);
			var c = AlphanumericIdEncoder.Encode(three);
			var d = AlphanumericIdEncoder.Encode(four);
			var e = AlphanumericIdEncoder.Encode(five);

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
		public void Encode_WithDecimal_ShouldReturnExpectedResult(decimal input, string expectedOutput)
		{
			var shortString = AlphanumericIdEncoder.Encode(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void Encode_WithDecimalAndMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = AlphanumericIdEncoder.Encode(DistributedIdGenerator.MaxValue);

			Assert.Equal("agbFu5KnEQGxp4QB", shortString);
		}

		[Fact]
		public void Encode_WithDecimalAndByteOutput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleDecimalId, stackalloc byte[16]);
		}

		[Fact]
		public void Encode_WithDecimalAndStringReturnValue_ShouldSucceed()
		{
			_ = AlphanumericIdEncoder.Encode(SampleDecimalId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("61")]
		[InlineData("62")]
		[InlineData("447835050025542181830910637")]
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void Encode_WithDecimal_ShouldBeReversibleByAllDecoders(string idToString)
		{
			var id = Decimal.Parse(idToString, NumberStyles.None);
			var bytes = new byte[16];
			AlphanumericIdEncoder.Encode(id, bytes);

			var results = ResultForAllDecimalDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooShortByteInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(stackalloc byte[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooShortCharInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(stackalloc char[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytesOfDecimalId.AsSpan().CopyTo(bytes);

			var success = AlphanumericIdEncoder.TryDecodeDecimal(bytes, out var result);

			Assert.True(success);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericStringOfDecimalId.AsSpan().CopyTo(chars);

			var success = AlphanumericIdEncoder.TryDecodeDecimal(chars, out var result);

			Assert.True(success);
			Assert.Equal(SampleDecimalId, result);
		}

		[Theory]
		[InlineData("1drbWFYI4a3pLliX", "447835050025542181830910637")]
		public void TryDecodeDecimal_Regularly_ShouldOutputExpectedResult(string input, string expectedResultString)
		{
			var expectedResult = Decimal.Parse(expectedResultString);
			var success = AlphanumericIdEncoder.TryDecodeDecimal(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfDecimalId)]
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
		public void DecodeDecimalOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeDecimal(string input)
		{
			var expectedResult = AlphanumericIdEncoder.TryDecodeDecimal(input, out var expectedId)
				? expectedId
				: (decimal?)null;

			var result = AlphanumericIdEncoder.DecodeDecimalOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[16];
			SampleAlphanumericBytesOfDecimalId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecimalDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(15)]
		[InlineData(29)]
		[InlineData(64)]
		public void AllDecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllDecimalDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
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

			var results = SuccessForAllDecimalDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void TryDecodeDecimal_WithBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(SampleAlphanumericBytesOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithChars_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(SampleAlphanumericStringOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeDecimalOrDefault(SampleAlphanumericBytesOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeDecimalOrDefault(SampleAlphanumericStringOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void TryDecodeDecimal_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(SampleAlphanumericBytesOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithAlphanumericString_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeDecimal(SampleAlphanumericStringOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeDecimalOrDefault(SampleAlphanumericBytesOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeDecimalOrDefault(SampleAlphanumericStringOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}
		
		#endregion

		#region Guid

		/// <summary>
		/// Encodes a large decimal represention as a GUID.
		/// This lets us reuse decimal test data for the GUID overloads.
		/// </summary>
		internal static Guid Guid(decimal value)
		{
			var decimals = MemoryMarshal.CreateSpan(ref value, length: 1);
			var components = MemoryMarshal.Cast<decimal, int>(decimals);

			var lo = DecimalStructure.GetLo(components);
			var mid = DecimalStructure.GetMid(components);
			var hi = (uint)DecimalStructure.GetHi(components);
			var signAndScale = DecimalStructure.GetSignAndScale(components);

			Span<byte> bytes = stackalloc byte[16];
			BinaryPrimitives.TryWriteInt32LittleEndian(bytes, 0);
			BinaryPrimitives.TryWriteUInt16LittleEndian(bytes[4..], (ushort)(hi >> 16));
			BinaryPrimitives.TryWriteUInt16LittleEndian(bytes[6..], (ushort)hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			var result = new Guid(bytes); // A GUID can be constructed from a big-endian span of bytes
			Assert.Equal(MemoryMarshal.Cast<byte, Guid>(bytes)[0], result); // Which should be the same as reinterpreting the bytes as a GUID

			// For correctness, confirm that the result of encoding the GUID into bytes is the same as encoding the decimal value into bytes
			Span<byte> binaryEncodedDecimal = stackalloc byte[16];
			Span<byte> binaryEncodedGuid = stackalloc byte[16];
			BinaryIdEncoder.Encode(value, binaryEncodedDecimal);
			BinaryIdEncoder.Encode(result, binaryEncodedGuid);
			Assert.True(binaryEncodedGuid.SequenceEqual(binaryEncodedDecimal));

			return result;
		}

		private static bool[] CheckIfThrowsForAllGuidEncodings(decimal id, byte[] bytes)
		{
			var guid = Guid(id);

			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => AlphanumericIdEncoder.Encode(guid, bytes)),
				Throws(() => AlphanumericIdEncoder.Encode(guid)),
			};
		}

		private static Guid?[] ResultForAllGuidDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++)
				chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeGuid(bytes, out var id) ? id : null,
				AlphanumericIdEncoder.TryDecodeGuid(chars, out id) ? id : null,
				AlphanumericIdEncoder.DecodeGuidOrDefault(bytes),
				AlphanumericIdEncoder.DecodeGuidOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllGuidDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				AlphanumericIdEncoder.TryDecodeGuid(bytes, out _),
				AlphanumericIdEncoder.TryDecodeGuid(chars, out _),
				AlphanumericIdEncoder.DecodeGuidOrDefault(bytes) is not null,
				AlphanumericIdEncoder.DecodeGuidOrDefault(chars) is not null,
			};
		}

		[Theory]
		[InlineData(0, "0000000000000000000000")]
		[InlineData(1, "0000000000000000000001")]
		[InlineData(61, "000000000000000000000z")]
		[InlineData(62, "0000000000000000000010")]
		[InlineData(1UL << 32, "00000000000000004gfFC4")]
		[InlineData(1 + (1UL << 32), "00000000000000004gfFC5")]
		[InlineData(Int64.MaxValue, "00000000000AzL8n0Y58m7")]
		[InlineData(UInt64.MaxValue, "00000000000LygHa16AHYF")]
		public void Encode_WithGuid_ShouldReturnExpectedResult(decimal id, string expectedResult)
		{
			var result = AlphanumericIdEncoder.Encode(Guid(id));

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Encode_WithGuidAndTooLongInput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleGuidId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithGuidAndTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => AlphanumericIdEncoder.Encode(SampleGuidId, new byte[21]));
		}

		[Fact]
		public void AllGuidEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllGuidEncodings(id, new byte[22]);
			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void Encode_WithGuidAndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = (decimal)UInt32.MaxValue;
			var three = UInt32.MaxValue + 1m;
			var four = (decimal)UInt64.MaxValue;
			var five = 18446744073709551616m; // hi=1
			var six = 4722366482869645213696m; // hi=16*16
			var seven = SampleGuidIdInDecimalForm;
			var eight = DistributedIdGenerator.MaxValue;
			var nine = System.Guid.Parse("00000000-5000-ffff-ffff-ffffffffffff");
			var ten = System.Guid.Parse("00000000-ffff-ffff-ffff-ffffffffffff");
			var eleven = System.Guid.Parse("00000001-ffff-ffff-ffff-ffffffffffff");
			var twelve = System.Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

			var a = AlphanumericIdEncoder.Encode(Guid(one));
			var b = AlphanumericIdEncoder.Encode(Guid(two));
			var c = AlphanumericIdEncoder.Encode(Guid(three));
			var d = AlphanumericIdEncoder.Encode(Guid(four));
			var e = AlphanumericIdEncoder.Encode(Guid(five));
			var f = AlphanumericIdEncoder.Encode(Guid(six));
			var g = AlphanumericIdEncoder.Encode(Guid(seven));
			var h = AlphanumericIdEncoder.Encode(Guid(eight));
			var i = AlphanumericIdEncoder.Encode(nine);
			var j = AlphanumericIdEncoder.Encode(ten);
			var k = AlphanumericIdEncoder.Encode(eleven);
			var l = AlphanumericIdEncoder.Encode(twelve);

			var expectedOrder = new[] { a, b, c, d, e, f, g, h, i, j, k, l };
			var sortedOrder = new[] { k, d, a, g, i, c, h, f, l, b, j, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Fact]
		public void Encode_WithGuidAndOtherIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var guids = new List<Guid>();

			// All except high 32 bits
			for (var i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = Guid(i);
				guids.Add(guid);
			}

			// Into high 32 bits
			for (var x = 1U; x <= UInt32.MaxValue / 2; x *= 2)
			{
				var guid = Guid(0m);
				var uints = MemoryMarshal.Cast<Guid, uint>(MemoryMarshal.CreateSpan(ref guid, length: 1));
				System.Diagnostics.Debug.Assert(uints[0] == 0);
				uints[0] = x;

				guids.Add(guid);
			}

			var sortedGuids = guids.ToList();
			sortedGuids.Sort();

			Assert.Equal(guids.AsEnumerable(), sortedGuids.AsEnumerable());
			Assert.Equal(guids.Select(guid => AlphanumericIdEncoder.Encode(guid)), sortedGuids.Select(guid => AlphanumericIdEncoder.Encode(guid)));
		}

		[Fact]
		public void Encode_WithGuidAndRandomizedIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			for (var x = 0; x < 100; x++)
			{
				var guidOne = System.Guid.NewGuid();
				var guidTwo = System.Guid.NewGuid();
				var resultOne = AlphanumericIdEncoder.Encode(guidOne);
				var resultTwo = AlphanumericIdEncoder.Encode(guidTwo);
				Assert.Equal(guidOne.CompareTo(guidTwo) > 0, String.Compare(resultOne, resultTwo, StringComparison.Ordinal) > 0);
				Assert.Equal(guidTwo.CompareTo(guidOne) > 0, String.Compare(resultTwo, resultOne, StringComparison.Ordinal) > 0);
			}
		}

		[Fact]
		public void Encode_WithGuidAndMaximumValue_ShouldReturnExpectedResult()
		{
			var bytes = new byte[16];
			Array.Fill(bytes, Byte.MaxValue);
			var guid = new Guid(bytes);

			var shortString = AlphanumericIdEncoder.Encode(guid);

			Assert.Equal("LygHa16AHYFLygHa16AHYF", shortString);
		}

		[Fact]
		public void Encode_WithGuidAndByteOutput_ShouldSucceed()
		{
			AlphanumericIdEncoder.Encode(SampleGuidId, stackalloc byte[22]);
		}

		[Fact]
		public void Encode_WithGuidAndStringReturnValue_ShouldSucceed()
		{
			_ = AlphanumericIdEncoder.Encode(SampleGuidId);
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void Encode_WithGuidAndValue_ShouldBeReversibleByAllDecoders(decimal id)
		{
			var bytes = new byte[22];
			AlphanumericIdEncoder.Encode(Guid(id), bytes);

			var results = ResultForAllGuidDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(Guid(id), results[i]);
		}

		[Fact]
		public void TryDecodeGuid_WithTooShortByteInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(stackalloc byte[21], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeGuid_WithTooShortCharInput_ShouldFail()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(stackalloc char[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeGuid_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytesOfGuidId.AsSpan().CopyTo(bytes);

			var success = AlphanumericIdEncoder.TryDecodeGuid(bytes, out var result);

			Assert.True(success);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void TryDecodeGuid_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericStringOfGuidId.AsSpan().CopyTo(chars);

			var success = AlphanumericIdEncoder.TryDecodeGuid(chars, out var result);

			Assert.True(success);
			Assert.Equal(SampleGuidId, result);
		}

		[Theory]
		[InlineData(0, "0000000000000000000000")]
		[InlineData(1, "0000000000000000000001")]
		[InlineData(Int64.MaxValue, "00000000000AzL8n0Y58m7")]
		[InlineData(UInt64.MaxValue, "00000000000LygHa16AHYF")]
		public void TryDecodeGuid_Regularly_ShouldOutputExpectedResult(decimal expectedResult, string input)
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(input, out var result);
			Assert.True(success);
			Assert.Equal(Guid(expectedResult), result);
		}

		[Theory]
		[InlineData(SampleAlphanumericStringOfGuidId)]
		[InlineData("0000000000000000000000")]
		[InlineData("0000000000000000000001")]
		[InlineData("00000000000AzL8n0Y58m7")]
		[InlineData("00000000000LygHa16AHYF")]
		public void DecodeGuidOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeGuid(string input)
		{
			var expectedResult = AlphanumericIdEncoder.TryDecodeGuid(input, out var expectedId)
				? expectedId
				: (Guid?)null;

			var result = AlphanumericIdEncoder.DecodeGuidOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllGuidDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[22];
			SampleAlphanumericBytesOfGuidId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllGuidDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(11)]
		[InlineData(16)]
		[InlineData(21)]
		public void AllGuidDecodingMethods_WithInsufficientLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllGuidDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Theory]
		[InlineData("123456789012345678901$")]
		[InlineData("123456789012345678901,00")]
		[InlineData("123456789012345678901.00")]
		[InlineData("+1234567890123456789012")]
		[InlineData("-1234567890123456789012")]
		[InlineData("123456789012345678901_")]
		public void AllGuidDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllGuidDecodings(bytes);

			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public void TryDecodeGuid_WithBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(SampleAlphanumericBytesOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeGuid_WithChars_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(SampleAlphanumericStringOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeGuidOrDefault(SampleAlphanumericBytesOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeGuidOrDefault(SampleAlphanumericStringOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void TryDecodeGuid_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(SampleAlphanumericBytesOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeGuid_AfterEncodeShouldReverseOperation()
		{
			var guids = new List<Guid>();
			Span<byte> bytes = stackalloc byte[22];

			// All except high 32 bits
			decimal i;
			for (i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = Guid(i);
				AlphanumericIdEncoder.Encode(guid, bytes);
				var result = AlphanumericIdEncoder.TryDecodeGuid(bytes, out var decoded);
				Assert.True(result);
				Assert.Equal(guid, decoded);
			}

			// Into high 32 bits
			for (var x = 1U; x <= UInt32.MaxValue / 2; x *= 2)
			{
				var guid = Guid(i);
				var uints = MemoryMarshal.Cast<Guid, uint>(MemoryMarshal.CreateSpan(ref guid, length: 1));
				System.Diagnostics.Debug.Assert(uints[0] == 0);
				uints[0] = x;

				AlphanumericIdEncoder.Encode(guid, bytes);
				var result = AlphanumericIdEncoder.TryDecodeGuid(bytes, out var decoded);
				Assert.True(result);
				Assert.Equal(guid, decoded);
			}
		}

		[Fact]
		public void TryDecodeGuid_WithAlphanumericString_ShouldSucceed()
		{
			var success = AlphanumericIdEncoder.TryDecodeGuid(SampleAlphanumericStringOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeGuidOrDefault(SampleAlphanumericBytesOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = AlphanumericIdEncoder.DecodeGuidOrDefault(SampleAlphanumericStringOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		#endregion
	}
}
