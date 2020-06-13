using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Architect.Identities.Tests.CompanyUniqueIds
{
	public sealed class CompanyUniqueIdEncoderTests
	{
		private static readonly decimal SampleId = 447835050025542181830910637m;
		private static readonly string SampleShortString = "1drbWFYI4a3pLliX";
		private static readonly byte[] SampleShortStringBytes = Encoding.ASCII.GetBytes(SampleShortString);
		private static readonly string SampleDecimalString = CompanyUniqueIdEncoder.MaxDecimalValue.ToString();
		private static readonly byte[] SampleDecimalStringBytes = Encoding.ASCII.GetBytes(SampleDecimalString);

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
				Throws(() => CompanyUniqueIdEncoder.ToShortString(id, bytes)),
				Throws(() => CompanyUniqueIdEncoder.ToShortString(id)),
			};
		}

		private static decimal?[] ResultForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				CompanyUniqueIdEncoder.TryFromShortString(bytes, out var id) ? id : -1m,
				CompanyUniqueIdEncoder.TryFromShortString(chars, out id) ? id : -1m,
				CompanyUniqueIdEncoder.FromShortStringOrDefault(bytes),
				CompanyUniqueIdEncoder.FromShortStringOrDefault(chars),
				CompanyUniqueIdEncoder.TryFromString(bytes, out id) ? id : -1m,
				CompanyUniqueIdEncoder.TryFromString(chars, out id) ? id : -1m,
				CompanyUniqueIdEncoder.FromStringOrDefault(bytes),
				CompanyUniqueIdEncoder.FromStringOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				CompanyUniqueIdEncoder.TryFromShortString(bytes, out _),
				CompanyUniqueIdEncoder.TryFromShortString(chars, out _),
				CompanyUniqueIdEncoder.FromShortStringOrDefault(bytes) != null,
				CompanyUniqueIdEncoder.FromShortStringOrDefault(chars) != null,
				CompanyUniqueIdEncoder.TryFromString(bytes, out _),
				CompanyUniqueIdEncoder.TryFromString(chars, out _),
				CompanyUniqueIdEncoder.FromStringOrDefault(bytes) != null,
				CompanyUniqueIdEncoder.FromStringOrDefault(chars) != null,
			};
		}

		private static decimal?[] ResultForAllFlexibleDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				CompanyUniqueIdEncoder.TryFromString(bytes, out var id) ? id : -1m,
				CompanyUniqueIdEncoder.TryFromString(chars, out id) ? id : -1m,
				CompanyUniqueIdEncoder.FromStringOrDefault(bytes),
				CompanyUniqueIdEncoder.FromStringOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllFlexibleDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				CompanyUniqueIdEncoder.TryFromString(bytes, out _),
				CompanyUniqueIdEncoder.TryFromString(chars, out _),
				CompanyUniqueIdEncoder.FromStringOrDefault(bytes) != null,
				CompanyUniqueIdEncoder.FromStringOrDefault(chars) != null,
			};
		}

		[Fact]
		public void ToShortString_WithTooLongInput_ShouldSucceed()
		{
			CompanyUniqueIdEncoder.ToShortString(SampleId, stackalloc byte[100]);
		}

		[Fact]
		public void ToShortString_WithTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => CompanyUniqueIdEncoder.ToShortString(SampleId, new byte[15]));
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
			var id = 1 + CompanyUniqueIdEncoder.MaxDecimalValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => didThrow));
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumDecimalValue_ShouldSucceed()
		{
			var id = CompanyUniqueIdEncoder.MaxDecimalValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[16]);
			Assert.Equal(results.Length, results.Count(didThrow => !didThrow));
		}

		[Fact]
		public void ToShortString_WithIncreasingDecimals_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = 2m;
			var three = (decimal)UInt64.MaxValue - 1;
			var four = (decimal)UInt64.MaxValue;
			var five = CompanyUniqueIdEncoder.MaxDecimalValue;

			var a = CompanyUniqueIdEncoder.ToShortString(one);
			var b = CompanyUniqueIdEncoder.ToShortString(two);
			var c = CompanyUniqueIdEncoder.ToShortString(three);
			var d = CompanyUniqueIdEncoder.ToShortString(four);
			var e = CompanyUniqueIdEncoder.ToShortString(five);

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
		public void ToShortString_WithValue_ShouldReturnExpectedEncoding(decimal input, string expectedOutput)
		{
			var shortString = CompanyUniqueIdEncoder.ToShortString(input);

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void ToShortString_WithMaximumValue_ShouldReturnExpectedEncoding()
		{
			var shortString = CompanyUniqueIdEncoder.ToShortString(CompanyUniqueIdEncoder.MaxDecimalValue);

			Assert.Equal("agbFu5KnEQGxp4QB", shortString);
		}

		[Fact]
		public void ToShortString_WithByteOutput_ShouldSucceed()
		{
			CompanyUniqueIdEncoder.ToShortString(SampleId, stackalloc byte[16]);
		}

		[Fact]
		public void ToShortString_WithStringReturnValue_ShouldSucceed()
		{
			_ = CompanyUniqueIdEncoder.ToShortString(SampleId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("61")]
		[InlineData("62")]
		[InlineData("447835050025542181830910637")]
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void ToShortString_WithValue_ShouldBeReversibleByAllDecoders(string idToString)
		{
			var id = Decimal.Parse(idToString, NumberStyles.None);
			var bytes = new byte[16];
			CompanyUniqueIdEncoder.ToShortString(id, bytes);

			var results = ResultForAllDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(id, results[i]);
		}

		[Fact]
		public void TryFromShortString_WithTooShortByteInput_ShouldFail()
		{
			var success = CompanyUniqueIdEncoder.TryFromShortString(stackalloc byte[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryFromShortString_WithTooShortCharInput_ShouldFail()
		{
			var success = CompanyUniqueIdEncoder.TryFromShortString(stackalloc char[15], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryFromShortString_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleShortStringBytes.AsSpan().CopyTo(bytes);
			var success = CompanyUniqueIdEncoder.TryFromShortString(bytes, out var result);
			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void TryFromShortString_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleShortString.AsSpan().CopyTo(chars);
			var success = CompanyUniqueIdEncoder.TryFromShortString(chars, out var result);
			Assert.True(success);
			Assert.Equal(SampleId, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidCharacters_ShouldFail()
		{
			var bytes = new byte[16];
			SampleShortStringBytes.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(15)]
		[InlineData(29)]
		[InlineData(64)]
		public void AllFlexibleDecodingMethods_WithInvalidLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllFlexibleDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData("12345678901234567$")]
		[InlineData("12345678901234567a")]
		[InlineData("12345678901234567,00")]
		[InlineData("12345678901234567.00")]
		[InlineData("+12345678901234567")]
		[InlineData("-12345678901234567")]
		[InlineData("12345678901234567E2")]
		[InlineData("12345678901234567_")]
		public void AllFlexibleDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidDecimalString)
		{
			var bytes = new byte[invalidDecimalString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidDecimalString[i];

			var results = SuccessForAllFlexibleDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData("12345678901234567")] // 17 digits
		[InlineData("1234567890123456789012345678")] // 28 digits
		[InlineData("9999999999999999999999999999")] // 28 digits
		public void AllFlexibleDecodingMethods_WithValidValue_ShouldReturnExpectedResult(string validDecimalString)
		{
			var expectedResult = Decimal.Parse(validDecimalString, NumberStyles.None);

			var bytes = new byte[validDecimalString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)validDecimalString[i];

			var results = ResultForAllFlexibleDecodings(bytes);

			foreach (var result in results)
				Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void TryFromShortString_WithBytes_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromShortString(SampleShortStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromShortString_WithChars_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromShortString(SampleShortString, out _);
			Assert.True(success);
		}

		[Fact]
		public void FromShortStringOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromShortStringOrDefault(SampleShortStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromShortStringOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromShortStringOrDefault(SampleShortString);
			Assert.True(result > 0m);
		}

		[Fact]
		public void TryFromString_WithShortBytes_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromString(SampleShortStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithDecimalStringBytes_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromString(SampleDecimalStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithShortString_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromString(SampleShortString, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithDecimalString_ShouldSucceed()
		{
			var success = CompanyUniqueIdEncoder.TryFromString(SampleDecimalString, out _);
			Assert.True(success);
		}

		[Fact]
		public void FromStringOrDefault_WithShortBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromStringOrDefault(SampleShortStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithDecimalStringBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromStringOrDefault(SampleDecimalStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithShortString_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromStringOrDefault(SampleShortString);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithDecimalString_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueIdEncoder.FromStringOrDefault(SampleDecimalString);
			Assert.True(result > 0m);
		}
	}
}
