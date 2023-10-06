using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Architect.Identities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public class HexadecimalIdEncoderTests
	{
		private const ulong SampleUlongId = 1234567890123456789UL;
		private const string SampleHexStringOfUlongId = "112210F47DE98115";
		private static readonly byte[] SampleHexBytesOfUlongId = Encoding.ASCII.GetBytes(SampleHexStringOfUlongId);

		private const decimal SampleDecimalId = 447835050025542181830910637m;
		private const string SampleHexStringOfDecimalId = "00017270C2B5280CE29739E2AD";
		private static readonly byte[] SampleHexBytesOfDecimalId = Encoding.ASCII.GetBytes(SampleHexStringOfDecimalId);

		private const decimal SampleGuidIdInDecimalForm = 1234567890123456789012345678m;
		private static readonly Guid SampleGuidId = Guid(SampleGuidIdInDecimalForm);
		private const string SampleHexStringOfGuidId = "0000000003FD35EB6D797A91BE38F34E";
		private static readonly byte[] SampleHexBytesOfGuidId = Encoding.ASCII.GetBytes(SampleHexStringOfGuidId);

		private const decimal SampleUInt128IdInDecimalForm = 1234567890123456789012345678m;
		private static readonly UInt128 SampleUInt128Id = (UInt128)SampleGuidIdInDecimalForm;
		private const string SampleHexStringOfUInt128Id = "0000000003FD35EB6D797A91BE38F34E";
		private static readonly byte[] SampleHexBytesOfUInt128Id = Encoding.ASCII.GetBytes(SampleHexStringOfUInt128Id);

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

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(Int64.MaxValue)]
		public void Encode_Regularly_ShouldMatchBuiltInEncoder(long id)
		{
			var longResult = HexadecimalIdEncoder.Encode(id);
			var ulongResult = HexadecimalIdEncoder.Encode((ulong)id);
			var decimalResult = HexadecimalIdEncoder.Encode((decimal)id);
			var guidResult = HexadecimalIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id));
			var uint128Result = HexadecimalIdEncoder.Encode((UInt128)id);

			var longBytes = new byte[8];
			var ulongBytes = new byte[8];
			var decimalBytes = new byte[16];
			var guidBytes = new byte[16];
			var uint128Bytes = new byte[16];
			BinaryIdEncoder.Encode(id, longBytes);
			BinaryIdEncoder.Encode((ulong)id, ulongBytes);
			BinaryIdEncoder.Encode((decimal)id, decimalBytes);
			BinaryIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id), guidBytes);
			BinaryIdEncoder.Encode((UInt128)id, uint128Bytes);
			var expectedLongResult = Convert.ToHexString(longBytes);
			var expectedUlongResult = Convert.ToHexString(ulongBytes);
			var expectedDecimalResult = Convert.ToHexString(decimalBytes[3..]); // Without the 3 fixed leading zeros
			var expectedGuidResult = Convert.ToHexString(guidBytes);
			var expectedUInt128Result = Convert.ToHexString(uint128Bytes);

			Assert.Equal(expectedLongResult, longResult);
			Assert.Equal(expectedUlongResult, ulongResult);
			Assert.Equal(expectedDecimalResult, decimalResult);
			Assert.Equal(expectedGuidResult, guidResult);
			Assert.Equal(expectedUInt128Result, uint128Result);
		}

		#region Long

		[Theory]
		[InlineData(-1L)]
		[InlineData(Int64.MinValue)]
		public void Encode_WithLongInputThatIsNegative_ShouldThrow(long negativeValue)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => HexadecimalIdEncoder.Encode(negativeValue));
			Assert.Throws<ArgumentOutOfRangeException>(() => HexadecimalIdEncoder.Encode(negativeValue, stackalloc byte[16]));
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
			var expectedResult = HexadecimalIdEncoder.Encode((ulong)id);

			var result = HexadecimalIdEncoder.Encode(id);

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
			Span<byte> expectedResult = stackalloc byte[16];
			var expectedResultString = HexadecimalIdEncoder.Encode(id);
			for (var i = 0; i < expectedResult.Length; i++)
				expectedResult[i] = (byte)expectedResultString[i];

			Span<byte> result = stackalloc byte[16];
			HexadecimalIdEncoder.Encode(id, result);

			Assert.True(result.SequenceEqual(expectedResult));
		}

		[Theory]
		[InlineData(SampleHexStringOfUlongId)]
		[InlineData("0000000000000001")]
		[InlineData("000000000000000F")]
		[InlineData("000000000000000f")]
		[InlineData("7FFFFFFFFFFFFFFF")] // Int64.MaxValue
		[InlineData("10000000000000000")] // UInt64.MaxValue + 1 invalid
		[InlineData("0000000000000001G")] // Invalid char
		[InlineData("0000000000000001,00")] // Invalid char
		[InlineData("0000000000000001.00")] // Invalid char
		[InlineData("+0000000000000001")] // Invalid char
		[InlineData("-0000000000000001")] // Invalid char
		[InlineData("0000000000000001_")] // Invalid char
		public void TryDecodeLong_WithCharInput_ShouldReturnSameResultAsTryDecodeUlong(string input)
		{
			var expectedSuccess = HexadecimalIdEncoder.TryDecodeUlong(input, out var expectedResult);

			var success = HexadecimalIdEncoder.TryDecodeLong(input, out var result);
			Assert.Equal(expectedSuccess, success);
			Assert.True(result >= 0L);
			Assert.Equal(expectedResult, (ulong)result);
		}
		
		[Theory]
		[InlineData(SampleHexStringOfUlongId)]
		[InlineData("0000000000000001")]
		[InlineData("000000000000000F")]
		[InlineData("000000000000000f")]
		[InlineData("7FFFFFFFFFFFFFFF")] // Int64.MaxValue
		[InlineData("10000000000000000")] // UInt64.MaxValue + 1 invalid
		[InlineData("000000000000001G")] // Invalid char
		[InlineData("00,0000000000001")] // Invalid char
		[InlineData("00.0000000000001")] // Invalid char
		[InlineData("+000000000000001")] // Invalid char
		[InlineData("-000000000000001")] // Invalid char
		[InlineData("000000000000001_")] // Invalid char
		public void TryDecodeLong_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedSuccess = HexadecimalIdEncoder.TryDecodeLong(inputString, out var expectedResult);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var success = HexadecimalIdEncoder.TryDecodeLong(input, out var result);

			Assert.Equal(expectedSuccess, success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleHexStringOfUlongId)]
		[InlineData("0000000000000001")]
		[InlineData("000000000000000F")]
		[InlineData("000000000000000f")]
		[InlineData("FFFFFFFFFFFFFFFF")] // UInt64.MaxValue
		[InlineData("10000000000000000")] // UInt64.MaxValue + 1 invalid
		[InlineData("0000000000000001G")] // Invalid char
		[InlineData("0000000000000001,00")] // Invalid char
		[InlineData("0000000000000001.00")] // Invalid char
		[InlineData("+0000000000000001")] // Invalid char
		[InlineData("-0000000000000001")] // Invalid char
		[InlineData("0000000000000001_")] // Invalid char
		public void DecodeLongOrDefault_WithCharInput_ShouldReturnSameResultAsTryDecodeLong(string input)
		{
			var expectedResult = HexadecimalIdEncoder.TryDecodeLong(input, out var expectedId)
				? expectedId
				: (long?)null;

			var result = HexadecimalIdEncoder.DecodeLongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleHexStringOfUlongId)]
		[InlineData("0000000000000001")]
		[InlineData("000000000000000F")]
		[InlineData("000000000000000f")]
		[InlineData("FFFFFFFFFFFFFFFF")] // UInt64.MaxValue
		[InlineData("10000000000000000")] // UInt64.MaxValue + 1 invalid
		[InlineData("000000000000001G")] // Invalid char
		[InlineData("00,0000000000001")] // Invalid char
		[InlineData("00.0000000000001")] // Invalid char
		[InlineData("+000000000000001")] // Invalid char
		[InlineData("-000000000000001")] // Invalid char
		[InlineData("000000000000001_")] // Invalid char
		public void DecodeLongOrDefault_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedResult = HexadecimalIdEncoder.DecodeLongOrDefault(inputString);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var result = HexadecimalIdEncoder.DecodeLongOrDefault(input);

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
				Throws(() => HexadecimalIdEncoder.Encode(id, bytes)),
				Throws(() => HexadecimalIdEncoder.Encode(id)),
			};
		}

		private static ulong?[] ResultForAllUlongDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeUlong(bytes, out var id) ? id : null,
				HexadecimalIdEncoder.TryDecodeUlong(chars, out id) ? id : null,
				HexadecimalIdEncoder.DecodeUlongOrDefault(bytes),
				HexadecimalIdEncoder.DecodeUlongOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllUlongDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++)
				chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeUlong(bytes, out _),
				HexadecimalIdEncoder.TryDecodeUlong(chars, out _),
				HexadecimalIdEncoder.DecodeUlongOrDefault(bytes) is not null,
				HexadecimalIdEncoder.DecodeUlongOrDefault(chars) is not null,
			};
		}

		[Fact]
		public void Encode_WithUlongAndTooLongOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleUlongId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithUlongAndTooShortOutput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => HexadecimalIdEncoder.Encode(SampleUlongId, stackalloc byte[15]));
		}

		[Fact]
		public void AllUlongEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = UInt64.MaxValue;
			var results = CheckIfThrowsForAllUlongEncodings(id, new byte[16]);
			Assert.All(results, Assert.False);
		}

		[Fact]
		public void Encode_WithUlongAndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1UL;
			var two = 2UL;
			var three = (ulong)UInt32.MaxValue;
			var four = UInt64.MaxValue - 1;
			var five = UInt64.MaxValue;

			var a = HexadecimalIdEncoder.Encode(one);
			var b = HexadecimalIdEncoder.Encode(two);
			var c = HexadecimalIdEncoder.Encode(three);
			var d = HexadecimalIdEncoder.Encode(four);
			var e = HexadecimalIdEncoder.Encode(five);

			var expectedOrder = new[] { a, b, c, d, e };
			var sortedOrder = new[] { d, a, c, b, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Theory]
		[InlineData(0, "0000000000000000")]
		[InlineData(1, "0000000000000001")]
		[InlineData(15, "000000000000000F")]
		[InlineData(16, "0000000000000010")]
		[InlineData(17, "0000000000000011")]
		[InlineData(1UL << 32, "0000000100000000")]
		[InlineData(1 + (1UL << 32), "0000000100000001")]
		public void Encode_WithUlongAndValue_ShouldReturnExpectedResult(ulong input, string expectedOutput)
		{
			var shortString = HexadecimalIdEncoder.Encode(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void Encode_WithUlongAndMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = HexadecimalIdEncoder.Encode(UInt64.MaxValue);

			Assert.Equal("FFFFFFFFFFFFFFFF", shortString);
		}

		[Fact]
		public void Encode_WithUlongAndByteOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleUlongId, stackalloc byte[16]);
		}

		[Fact]
		public void Encode_WithUlongAndStringReturnValue_ShouldSucceed()
		{
			_ = HexadecimalIdEncoder.Encode(SampleUlongId);
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
			var bytes = new byte[16];
			HexadecimalIdEncoder.Encode(id, bytes);

			var results = ResultForAllUlongDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryDecodeUlong_WithTooShortByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(stackalloc byte[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUlong_WithTooShortCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(stackalloc char[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUlong_WithTooLongByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(stackalloc byte[100], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUlong_WithTooLongCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(stackalloc char[100], out _);
			Assert.False(success);
		}

		[Theory]
		[InlineData("112210F47DE98115", 1234567890123456789)]
		[InlineData("FFFFFFFFFFFFFFFF", UInt64.MaxValue)]
		[InlineData("ffffffffffffffff", UInt64.MaxValue)]
		public void TryDecodeUlong_Regularly_ShouldOutputExpectedResult(string input, ulong expectedResult)
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleHexStringOfUlongId)]
		[InlineData("0000000000000001")]
		[InlineData("000000000000000F")]
		[InlineData("000000000000000f")]
		[InlineData("FFFFFFFFFFFFFFFF")] // UInt64.MaxValue
		[InlineData("10000000000000000")] // UInt64.MaxValue + 1 invalid
		[InlineData("0000000000000001G")] // Invalid char
		[InlineData("0000000000000001,00")] // Invalid char
		[InlineData("0000000000000001.00")] // Invalid char
		[InlineData("+0000000000000001")] // Invalid char
		[InlineData("-0000000000000001")] // Invalid char
		[InlineData("0000000000000001_")] // Invalid char
		public void DecodeUlongOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeUlong(string input)
		{
			var expectedResult = HexadecimalIdEncoder.TryDecodeUlong(input, out var expectedId)
				? expectedId
				: (ulong?)null;

			var result = HexadecimalIdEncoder.DecodeUlongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllUlongDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[16];
			SampleHexBytesOfUlongId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllUlongDecodings(bytes);

			Assert.All(results, Assert.False);
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

			Assert.All(results, Assert.False);
		}

		[Theory]
		[InlineData("FFFFFFFFFFFFFFFG")]
		[InlineData("000000000000001$")]
		[InlineData("0000000000001,00")]
		[InlineData("0000000000001.00")]
		[InlineData("+0000000000000001")]
		[InlineData("-0000000000000001")]
		[InlineData("000000000000001_")]
		public void AllUlongDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllUlongDecodings(bytes);

			Assert.All(results, Assert.False);
		}

		[Fact]
		public void TryDecodeUlong_WithBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(SampleHexBytesOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUlong_WithChars_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(SampleHexStringOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUlongOrDefault(SampleHexBytesOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUlongOrDefault(SampleHexStringOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void TryDecodeUlong_WithHexadecimalBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(SampleHexBytesOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUlong_WithHexadecimalString_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUlong(SampleHexStringOfUlongId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithHexadecimalBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUlongOrDefault(SampleHexBytesOfUlongId);
			Assert.Equal(SampleUlongId, result);
		}

		[Fact]
		public void DecodeUlongOrDefault_WithHexadecimalString_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUlongOrDefault(SampleHexStringOfUlongId);
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
				Throws(() => HexadecimalIdEncoder.Encode(id, bytes)),
				Throws(() => HexadecimalIdEncoder.Encode(id)),
			};
		}

		private static decimal?[] ResultForAllDecimalDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeDecimal(bytes, out var id) ? id : -1m,
				HexadecimalIdEncoder.TryDecodeDecimal(chars, out id) ? id : -1m,
				HexadecimalIdEncoder.DecodeDecimalOrDefault(bytes),
				HexadecimalIdEncoder.DecodeDecimalOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecimalDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeDecimal(bytes, out _),
				HexadecimalIdEncoder.TryDecodeDecimal(chars, out _),
				HexadecimalIdEncoder.DecodeDecimalOrDefault(bytes) is not null,
				HexadecimalIdEncoder.DecodeDecimalOrDefault(chars) is not null,
			};
		}

		[Fact]
		public void Encode_WithDecimalAndTooLongOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleDecimalId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithDecimalAndTooShortOutput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => HexadecimalIdEncoder.Encode(SampleDecimalId, stackalloc byte[25]));
		}

		[Fact]
		public void AllEncodingMethods_WithSign_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: true, scale: 0);
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[32]);
			Assert.All(results, Assert.True);
		}

		[Fact]
		public void AllEncodingMethods_WithNonzeroScale_ShouldThrow()
		{
			var id = new decimal(lo: 1, mid: 1, hi: 1, isNegative: false, scale: 1);
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[32]);
			Assert.All(results, Assert.True);
		}

		[Fact]
		public void AllEncodingMethods_WithOverflow_ShouldThrow()
		{
			var id = 1 + DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[32]);
			Assert.All(results, Assert.True);
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllDecimalEncodings(id, new byte[32]);
			Assert.All(results, Assert.False);
		}

		[Fact]
		public void Encode_WithDecimalAndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = 2m;
			var three = (decimal)UInt64.MaxValue - 1;
			var four = (decimal)UInt64.MaxValue;
			var five = DistributedIdGenerator.MaxValue;

			var a = HexadecimalIdEncoder.Encode(one);
			var b = HexadecimalIdEncoder.Encode(two);
			var c = HexadecimalIdEncoder.Encode(three);
			var d = HexadecimalIdEncoder.Encode(four);
			var e = HexadecimalIdEncoder.Encode(five);

			var expectedOrder = new[] { a, b, c, d, e };
			var sortedOrder = new[] { d, a, c, b, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Theory]
		[InlineData(0, "00000000000000000000000000")]
		[InlineData(1, "00000000000000000000000001")]
		[InlineData(10, "0000000000000000000000000A")]
		[InlineData(15, "0000000000000000000000000F")]
		[InlineData(16, "00000000000000000000000010")]
		[InlineData(17, "00000000000000000000000011")]
		[InlineData(1UL << 32, "00000000000000000100000000")]
		[InlineData(1 + (1UL << 32), "00000000000000000100000001")]
		public void Encode_WithDecimal_ShouldReturnExpectedResult(decimal input, string expectedOutput)
		{
			var shortString = HexadecimalIdEncoder.Encode(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void Encode_WithDecimalAndMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = HexadecimalIdEncoder.Encode(DistributedIdGenerator.MaxValue);

			Assert.Equal("00204FCE5E3E2502610FFFFFFF", shortString);
		}

		[Fact]
		public void Encode_WithDecimalAndByteOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleDecimalId, stackalloc byte[26]);
		}

		[Fact]
		public void Encode_WithDecimalAndStringReturnValue_ShouldSucceed()
		{
			_ = HexadecimalIdEncoder.Encode(SampleDecimalId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("10")]
		[InlineData("15")]
		[InlineData("16")]
		[InlineData("17")]
		[InlineData("447835050025542181830910637")]
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void Encode_WithDecimal_ShouldBeReversibleByAllDecoders(string idToString)
		{
			var id = Decimal.Parse(idToString, NumberStyles.None);
			var bytes = new byte[26];
			HexadecimalIdEncoder.Encode(id, bytes);

			var results = ResultForAllDecimalDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooShortByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(stackalloc byte[25], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooShortCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(stackalloc char[25], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooLongByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(stackalloc byte[100], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithTooLongCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(stackalloc char[100], out _);
			Assert.False(success);
		}

		[Theory]
		[InlineData("00017270C2B5280CE29739E2AD", "447835050025542181830910637")]
		public void TryDecodeDecimal_Regularly_ShouldOutputExpectedResult(string input, string expectedResultString)
		{
			var expectedResult = Decimal.Parse(expectedResultString);
			var success = HexadecimalIdEncoder.TryDecodeDecimal(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleHexStringOfDecimalId)]
		[InlineData("00000000000000000000000000")]
		[InlineData("00000000000000000000000001")]
		[InlineData("F0000000000000000000000000")]
		[InlineData("f0000000000000000000000000")]
		[InlineData("0000000000FFFFFFFFFFFFFFFF")] // UInt64.MaxValue
		[InlineData("F0000000010000000000000000")] // UInt64.MaxValue + 1
		[InlineData("0000000000FFFFFFFFFFFFFFFG")] // Invalid char
		[InlineData("00000000000000000000000001,00")] // Invalid char
		[InlineData("00000000000000000000000001.00")] // Invalid char
		[InlineData("+00000000000000000000000001")] // Invalid char
		[InlineData("-00000000000000000000000001")] // Invalid char
		[InlineData("00000000000000000000000001_")] // Invalid char
		public void DecodeDecimalOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeDecimal(string input)
		{
			var expectedResult = HexadecimalIdEncoder.TryDecodeDecimal(input, out var expectedId)
				? expectedId
				: (decimal?)null;

			var result = HexadecimalIdEncoder.DecodeDecimalOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[26];
			SampleHexBytesOfDecimalId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecimalDecodings(bytes);

			Assert.All(results, Assert.False);
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

			Assert.All(results, Assert.False);
		}

		[Theory]
		[InlineData("0000000000000000000000000G")]
		[InlineData("0000000000000000000000001$")]
		[InlineData("00000000000000000000001,00")]
		[InlineData("00000000000000000000001.00")]
		[InlineData("+00000000000000000000000001")]
		[InlineData("-00000000000000000000000001")]
		[InlineData("000000000000000000000001_")]
		public void AllDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllDecimalDecodings(bytes);

			Assert.All(results, Assert.False);
		}

		[Fact]
		public void TryDecodeDecimal_WithBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(SampleHexBytesOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithChars_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(SampleHexStringOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeDecimalOrDefault(SampleHexBytesOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeDecimalOrDefault(SampleHexStringOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void TryDecodeDecimal_WithHexadecimalBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(SampleHexBytesOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeDecimal_WithHexadecimalString_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeDecimal(SampleHexStringOfDecimalId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithHexadecimalBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeDecimalOrDefault(SampleHexBytesOfDecimalId);
			Assert.Equal(SampleDecimalId, result);
		}

		[Fact]
		public void DecodeDecimalOrDefault_WithHexadecimalString_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeDecimalOrDefault(SampleHexStringOfDecimalId);
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
				Throws(() => HexadecimalIdEncoder.Encode(guid, bytes)),
				Throws(() => HexadecimalIdEncoder.Encode(guid)),
			};
		}

		private static Guid?[] ResultForAllGuidDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++)
				chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeGuid(bytes, out var id) ? id : null,
				HexadecimalIdEncoder.TryDecodeGuid(chars, out id) ? id : null,
				HexadecimalIdEncoder.DecodeGuidOrDefault(bytes),
				HexadecimalIdEncoder.DecodeGuidOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllGuidDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeGuid(bytes, out _),
				HexadecimalIdEncoder.TryDecodeGuid(chars, out _),
				HexadecimalIdEncoder.DecodeGuidOrDefault(bytes) is not null,
				HexadecimalIdEncoder.DecodeGuidOrDefault(chars) is not null,
			};
		}

		[Theory]
		[InlineData(0, "00000000000000000000000000000000")]
		[InlineData(1, "00000000000000000000000000000001")]
		[InlineData(15, "0000000000000000000000000000000F")]
		[InlineData(16, "00000000000000000000000000000010")]
		[InlineData(17, "00000000000000000000000000000011")]
		[InlineData(1UL << 32, "00000000000000000000000100000000")]
		[InlineData(1 + (1UL << 32), "00000000000000000000000100000001")]
		[InlineData(Int64.MaxValue, "00000000000000007FFFFFFFFFFFFFFF")]
		[InlineData(UInt64.MaxValue, "0000000000000000FFFFFFFFFFFFFFFF")]
		public void Encode_WithGuid_ShouldReturnExpectedResult(decimal id, string expectedResult)
		{
			var result = HexadecimalIdEncoder.Encode(Guid(id));

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Encode_WithGuidAndTooLongOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleGuidId, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithGuidAndTooShortOutput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => HexadecimalIdEncoder.Encode(SampleGuidId, stackalloc byte[31]));
		}

		[Fact]
		public void AllGuidEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllGuidEncodings(id, new byte[32]);
			Assert.All(results, Assert.False);
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

			var a = HexadecimalIdEncoder.Encode(Guid(one));
			var b = HexadecimalIdEncoder.Encode(Guid(two));
			var c = HexadecimalIdEncoder.Encode(Guid(three));
			var d = HexadecimalIdEncoder.Encode(Guid(four));
			var e = HexadecimalIdEncoder.Encode(Guid(five));
			var f = HexadecimalIdEncoder.Encode(Guid(six));
			var g = HexadecimalIdEncoder.Encode(Guid(seven));
			var h = HexadecimalIdEncoder.Encode(Guid(eight));
			var i = HexadecimalIdEncoder.Encode(nine);
			var j = HexadecimalIdEncoder.Encode(ten);
			var k = HexadecimalIdEncoder.Encode(eleven);
			var l = HexadecimalIdEncoder.Encode(twelve);

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
			Assert.Equal(guids.Select(HexadecimalIdEncoder.Encode), sortedGuids.Select(HexadecimalIdEncoder.Encode));
		}

		[Fact]
		public void Encode_WithGuidAndRandomizedIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			for (var x = 0; x < 100; x++)
			{
				var guidOne = System.Guid.NewGuid();
				var guidTwo = System.Guid.NewGuid();
				var resultOne = HexadecimalIdEncoder.Encode(guidOne);
				var resultTwo = HexadecimalIdEncoder.Encode(guidTwo);
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

			var shortString = HexadecimalIdEncoder.Encode(guid);

			Assert.Equal("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", shortString);
		}

		[Fact]
		public void Encode_WithGuidAndByteOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleGuidId, stackalloc byte[32]);
		}

		[Fact]
		public void Encode_WithGuidAndStringReturnValue_ShouldSucceed()
		{
			_ = HexadecimalIdEncoder.Encode(SampleGuidId);
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
			var bytes = new byte[32];
			HexadecimalIdEncoder.Encode(Guid(id), bytes);

			var results = ResultForAllGuidDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(Guid(id), results[i]);
		}

		[Fact]
		public void TryDecodeGuid_WithTooShortByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(stackalloc byte[31], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeGuid_WithTooShortCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(stackalloc char[31], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeGuid_WithTooLongByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(stackalloc byte[100], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeGuid_WithTooLongCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(stackalloc char[100], out _);
			Assert.False(success);
		}

		[Theory]
		[InlineData(0, "00000000000000000000000000000000")]
		[InlineData(1, "00000000000000000000000000000001")]
		[InlineData(Int64.MaxValue, "00000000000000007FFFFFFFFFFFFFFF")]
		[InlineData(UInt64.MaxValue, "0000000000000000FFFFFFFFFFFFFFFF")]
		public void TryDecodeGuid_Regularly_ShouldOutputExpectedResult(decimal expectedResult, string input)
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(input, out var result);
			Assert.True(success);
			Assert.Equal(Guid(expectedResult), result);
		}

		[Theory]
		[InlineData(SampleHexStringOfGuidId)]
		[InlineData("00000000000000000000000000000000")]
		[InlineData("00000000000000000000000000000001")]
		[InlineData("00000000000000007FFFFFFFFFFFFFFF")]
		[InlineData("0000000000000000FFFFFFFFFFFFFFFF")]
		public void DecodeGuidOrDefault_Regularly_ShouldReturnSameResultAsTryDecodeGuid(string input)
		{
			var expectedResult = HexadecimalIdEncoder.TryDecodeGuid(input, out var expectedId)
				? expectedId
				: (Guid?)null;

			var result = HexadecimalIdEncoder.DecodeGuidOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllGuidDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[32];
			SampleHexBytesOfGuidId.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllGuidDecodings(bytes);

			Assert.All(results, Assert.False);
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

			Assert.All(results, Assert.False);
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

			Assert.All(results, Assert.False);
		}

		[Fact]
		public void TryDecodeGuid_WithBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(SampleHexBytesOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeGuid_WithChars_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(SampleHexStringOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeGuidOrDefault(SampleHexBytesOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeGuidOrDefault(SampleHexStringOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void TryDecodeGuid_WithHexadecimalBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(SampleHexBytesOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeGuid_AfterEncodeShouldReverseOperation()
		{
			var guids = new List<Guid>();
			Span<byte> bytes = stackalloc byte[32];

			// All except high 32 bits
			decimal i;
			for (i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = Guid(i);
				HexadecimalIdEncoder.Encode(guid, bytes);
				var result = HexadecimalIdEncoder.TryDecodeGuid(bytes, out var decoded);
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

				HexadecimalIdEncoder.Encode(guid, bytes);
				var result = HexadecimalIdEncoder.TryDecodeGuid(bytes, out var decoded);
				Assert.True(result);
				Assert.Equal(guid, decoded);
			}
		}

		[Fact]
		public void TryDecodeGuid_WithHexadecimalString_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeGuid(SampleHexStringOfGuidId, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithHexadecimalBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeGuidOrDefault(SampleHexBytesOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		[Fact]
		public void DecodeGuidOrDefault_WithHexadecimalString_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeGuidOrDefault(SampleHexStringOfGuidId);
			Assert.Equal(SampleGuidId, result);
		}

		#endregion

		#region UInt128

		private static bool[] CheckIfThrowsForAllUInt128Encodings(UInt128 id, byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => HexadecimalIdEncoder.Encode(id, bytes)),
				Throws(() => HexadecimalIdEncoder.Encode(id)),
			};
		}

		private static UInt128?[] ResultForAllUInt128Decodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeUInt128(bytes, out var id) ? id : null,
				HexadecimalIdEncoder.TryDecodeUInt128(chars, out id) ? id : null,
				HexadecimalIdEncoder.DecodeUInt128OrDefault(bytes),
				HexadecimalIdEncoder.DecodeUInt128OrDefault(chars),
			};
		}

		private static bool[] SuccessForAllUInt128Decodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				HexadecimalIdEncoder.TryDecodeUInt128(bytes, out _),
				HexadecimalIdEncoder.TryDecodeUInt128(chars, out _),
				HexadecimalIdEncoder.DecodeUInt128OrDefault(bytes) is not null,
				HexadecimalIdEncoder.DecodeUInt128OrDefault(chars) is not null,
			};
		}

		[Fact]
		public void Encode_WithUInt128AndTooLongOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleUInt128Id, stackalloc byte[100]);
		}

		[Fact]
		public void Encode_WithUInt128AndTooShortOutput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => HexadecimalIdEncoder.Encode(SampleUInt128Id, stackalloc byte[31]));
		}

		[Fact]
		public void AllUInt128EncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = UInt128.MaxValue;
			var results = CheckIfThrowsForAllUInt128Encodings(id, new byte[32]);
			Assert.All(results, Assert.False);
		}

		[Fact]
		public void Encode_WithUInt128AndIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = (UInt128)1m;
			var two = (UInt128)2m;
			var three = (UInt128)UInt64.MaxValue - 1;
			var four = (UInt128)UInt64.MaxValue;
			var five = UInt128.MaxValue;

			var a = HexadecimalIdEncoder.Encode(one);
			var b = HexadecimalIdEncoder.Encode(two);
			var c = HexadecimalIdEncoder.Encode(three);
			var d = HexadecimalIdEncoder.Encode(four);
			var e = HexadecimalIdEncoder.Encode(five);

			var expectedOrder = new[] { a, b, c, d, e };
			var sortedOrder = new[] { d, a, c, b, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Theory]
		[InlineData(0, "00000000000000000000000000")]
		[InlineData(1, "00000000000000000000000001")]
		[InlineData(10, "0000000000000000000000000A")]
		[InlineData(15, "0000000000000000000000000F")]
		[InlineData(16, "00000000000000000000000010")]
		[InlineData(17, "00000000000000000000000011")]
		[InlineData(1UL << 32, "00000000000000000100000000")]
		[InlineData(1 + (1UL << 32), "00000000000000000100000001")]
		public void Encode_WithUInt128_ShouldReturnExpectedResult(decimal input, string expectedOutput)
		{
			var shortString = HexadecimalIdEncoder.Encode(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void Encode_WithUInt128AndMaximumValue_ShouldReturnExpectedResult()
		{
			var shortString = HexadecimalIdEncoder.Encode(UInt128.MaxValue);

			Assert.Equal("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", shortString);
		}

		[Fact]
		public void Encode_WithUInt128AndByteOutput_ShouldSucceed()
		{
			HexadecimalIdEncoder.Encode(SampleUInt128Id, stackalloc byte[32]);
		}

		[Fact]
		public void Encode_WithUInt128AndStringReturnValue_ShouldSucceed()
		{
			_ = HexadecimalIdEncoder.Encode(SampleUInt128Id);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("10")]
		[InlineData("15")]
		[InlineData("16")]
		[InlineData("17")]
		[InlineData("447835050025542181830910637")]
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void Encode_WithUInt128_ShouldBeReversibleByAllDecoders(string idToString)
		{
			var id = UInt128.Parse(idToString, NumberStyles.None);
			var bytes = new byte[32];
			HexadecimalIdEncoder.Encode(id, bytes);

			var results = ResultForAllUInt128Decodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryDecodeUInt128_WithTooShortByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(stackalloc byte[31], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUInt128_WithTooShortCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(stackalloc char[31], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUInt128_WithTooLongByteInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(stackalloc byte[100], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryDecodeUInt128_WithTooLongCharInput_ShouldFail()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(stackalloc char[100], out _);
			Assert.False(success);
		}

		[Theory]
		[InlineData("00000000017270C2B5280CE29739E2AD", "447835050025542181830910637")]
		public void TryDecodeUInt128_Regularly_ShouldOutputExpectedResult(string input, string expectedResultString)
		{
			var expectedResult = UInt128.Parse(expectedResultString);
			var success = HexadecimalIdEncoder.TryDecodeUInt128(input, out var result);
			Assert.True(success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleHexStringOfUInt128Id)]
		[InlineData("00000000000000000000000000")]
		[InlineData("00000000000000000000000001")]
		[InlineData("F0000000000000000000000000")]
		[InlineData("f0000000000000000000000000")]
		[InlineData("0000000000FFFFFFFFFFFFFFFF")] // UInt64.MaxValue
		[InlineData("F0000000010000000000000000")] // UInt64.MaxValue + 1
		[InlineData("0000000000FFFFFFFFFFFFFFFG")] // Invalid char
		[InlineData("00000000000000000000000001,00")] // Invalid char
		[InlineData("00000000000000000000000001.00")] // Invalid char
		[InlineData("+00000000000000000000000001")] // Invalid char
		[InlineData("-00000000000000000000000001")] // Invalid char
		[InlineData("00000000000000000000000001_")] // Invalid char
		public void DecodeUInt128OrDefault_Regularly_ShouldReturnSameResultAsTryDecodeUInt128(string input)
		{
			var expectedResult = HexadecimalIdEncoder.TryDecodeUInt128(input, out var expectedId)
				? expectedId
				: (UInt128?)null;

			var result = HexadecimalIdEncoder.DecodeUInt128OrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllUInt128DecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[32];
			SampleHexBytesOfUInt128Id.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllUInt128Decodings(bytes);

			Assert.All(results, Assert.False);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(15)]
		[InlineData(29)]
		[InlineData(64)]
		public void AllUInt128DecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllUInt128Decodings(bytes);

			Assert.All(results, Assert.False);
		}

		[Theory]
		[InlineData("0000000000000000000000000G")]
		[InlineData("0000000000000000000000001$")]
		[InlineData("00000000000000000000001,00")]
		[InlineData("00000000000000000000001.00")]
		[InlineData("+00000000000000000000000001")]
		[InlineData("-00000000000000000000000001")]
		[InlineData("000000000000000000000001_")]
		public void AllUInt128DecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllUInt128Decodings(bytes);

			Assert.All(results, Assert.False);
		}

		[Fact]
		public void TryDecodeUInt128_WithBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(SampleHexBytesOfUInt128Id, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUInt128_WithChars_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(SampleHexStringOfUInt128Id, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUInt128OrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUInt128OrDefault(SampleHexBytesOfUInt128Id);
			Assert.Equal(SampleUInt128Id, result);
		}

		[Fact]
		public void DecodeUInt128OrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUInt128OrDefault(SampleHexStringOfUInt128Id);
			Assert.Equal(SampleUInt128Id, result);
		}

		[Fact]
		public void TryDecodeUInt128_WithHexadecimalBytes_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(SampleHexBytesOfUInt128Id, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryDecodeUInt128_WithHexadecimalString_ShouldSucceed()
		{
			var success = HexadecimalIdEncoder.TryDecodeUInt128(SampleHexStringOfUInt128Id, out _);
			Assert.True(success);
		}

		[Fact]
		public void DecodeUInt128OrDefault_WithHexadecimalBytes_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUInt128OrDefault(SampleHexBytesOfUInt128Id);
			Assert.Equal(SampleUInt128Id, result);
		}

		[Fact]
		public void DecodeUInt128OrDefault_WithHexadecimalString_ShouldReturnExpectedValue()
		{
			var result = HexadecimalIdEncoder.DecodeUInt128OrDefault(SampleHexStringOfUInt128Id);
			Assert.Equal(SampleUInt128Id, result);
		}

		#endregion
	}
}
