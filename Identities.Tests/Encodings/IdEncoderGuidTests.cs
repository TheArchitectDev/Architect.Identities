using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Architect.Identities.Helpers;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class IdEncoderGuidTests
	{
		private const decimal SampleId = 1234567890123456789012345678m;
		private const string SampleAlphanumericString = "0000004WoWZ9OjHPSzq3Ju";
		private static readonly byte[] SampleAlphanumericBytes = Encoding.ASCII.GetBytes(SampleAlphanumericString);

		private static Guid Guid(decimal value)
		{
			var decimals = MemoryMarshal.CreateSpan(ref value, length: 1);
			var components = MemoryMarshal.Cast<decimal, int>(decimals);
			var lo = DecimalStructure.GetLo(components);
			var mid = DecimalStructure.GetMid(components);
			var hi = DecimalStructure.GetHi(components);
			var signAndScale = DecimalStructure.GetSignAndScale(components);
			Span<byte> bytes = stackalloc byte[16];
			BinaryPrimitives.TryWriteInt32BigEndian(bytes[0..], signAndScale);
			BinaryPrimitives.TryWriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.TryWriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.TryWriteInt32BigEndian(bytes[12..], lo);
			return new Guid(bytes);
		}

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
			var guid = Guid(id);

			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				Throws(() => IdEncoder.GetAlphanumeric(guid, bytes)),
				Throws(() => IdEncoder.GetAlphanumeric(guid)),
			};
		}

		private static Guid?[] ResultForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetGuid(bytes, out var id) ? id : (Guid?)null,
				IdEncoder.TryGetGuid(chars, out id) ? id : (Guid?)null,
				IdEncoder.GetGuidOrDefault(bytes),
				IdEncoder.GetGuidOrDefault(chars),
			};
		}

		private static bool[] SuccessForAllDecodings(byte[] bytes)
		{
			Span<char> chars = stackalloc char[bytes.Length];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return new[]
			{
				IdEncoder.TryGetGuid(bytes, out _),
				IdEncoder.TryGetGuid(chars, out _),
				IdEncoder.GetGuidOrDefault(bytes) != null,
				IdEncoder.GetGuidOrDefault(chars) != null,
			};
		}

		[Theory]
		[InlineData(0, "0000000000000000000000")]
		[InlineData(1, "0000000000000000000001")]
		[InlineData(Int64.MaxValue, "00000000000AzL8n0Y58m7")]
		[InlineData(UInt64.MaxValue, "00000000000LygHa16AHYF")]
		public void GetAlphanumeric_Regularly_ShouldReturnExpectedResult(decimal id, string expectedResult)
		{
			var result = IdEncoder.GetAlphanumeric(Guid(id));

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAlphanumeric_WithTooLongInput_ShouldSucceed()
		{
			IdEncoder.GetAlphanumeric(Guid(SampleId), stackalloc byte[100]);
		}

		[Fact]
		public void GetAlphanumeric_WithTooShortInput_ShouldThrow()
		{
			Assert.Throws<IndexOutOfRangeException>(() => IdEncoder.GetAlphanumeric(Guid(SampleId), new byte[21]));
		}

		[Fact]
		public void AllEncodingMethods_WithMaximumValue_ShouldSucceed()
		{
			var id = DistributedIdGenerator.MaxValue;
			var results = CheckIfThrowsForAllEncodings(id, new byte[22]);
			Assert.Equal(results.Length, results.Count(didThrow => !didThrow));
		}

		[Fact]
		public void GetAlphanumeric_WithIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var one = 1m;
			var two = 2m;
			var three = (decimal)UInt32.MaxValue;
			var four = (decimal)UInt64.MaxValue;
			var five = SampleId;

			var a = IdEncoder.GetAlphanumeric(Guid(one));
			var b = IdEncoder.GetAlphanumeric(Guid(two));
			var c = IdEncoder.GetAlphanumeric(Guid(three));
			var d = IdEncoder.GetAlphanumeric(Guid(four));
			var e = IdEncoder.GetAlphanumeric(Guid(five));

			var expectedOrder = new[] { a, b, c, d, e };
			var sortedOrder = new[] { d, a, c, b, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Theory]
		[InlineData(0, "0000000000000000000000")]
		[InlineData(1, "0000000000000000000001")]
		[InlineData(61, "000000000000000000000z")]
		[InlineData(62, "0000000000000000000010")]
		[InlineData(1UL << 32, "00000000000000004gfFC4")]
		[InlineData(1 + (1UL << 32), "00000000000000004gfFC5")]
		public void GetAlphanumeric_WithValue_ShouldReturnExpectedResult(decimal input, string expectedOutput)
		{
			var shortString = IdEncoder.GetAlphanumeric(Guid(input));

			Assert.Equal(expectedOutput, shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithMaximumValue_ShouldReturnExpectedResult()
		{
			var bytes = new byte[16];
			Array.Fill(bytes, Byte.MaxValue);
			var guid = new Guid(bytes);

			var shortString = IdEncoder.GetAlphanumeric(guid);

			Assert.Equal("LygHa16AHYFLygHa16AHYF", shortString);
		}

		[Fact]
		public void GetAlphanumeric_WithByteOutput_ShouldSucceed()
		{
			IdEncoder.GetAlphanumeric(Guid(SampleId), stackalloc byte[22]);
		}

		[Fact]
		public void GetAlphanumeric_WithStringReturnValue_ShouldSucceed()
		{
			_ = IdEncoder.GetAlphanumeric(Guid(SampleId));
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void GetAlphanumeric_WithValue_ShouldBeReversibleByAllDecoders(decimal id)
		{
			var bytes = new byte[22];
			IdEncoder.GetAlphanumeric(Guid(id), bytes);

			var results = ResultForAllDecodings(bytes);

			for (var i = 0; i < results.Length; i++)
				Assert.Equal(Guid(id), results[i]);
		}

		[Fact]
		public void TryGetGuid_WithTooShortByteInput_ShouldFail()
		{
			var success = IdEncoder.TryGetGuid(stackalloc byte[21], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetGuid_WithTooShortCharInput_ShouldFail()
		{
			var success = IdEncoder.TryGetGuid(stackalloc char[10], out _);
			Assert.False(success);
		}

		[Fact]
		public void TryGetGuid_WithTooLongByteInput_ShouldReturnExpectedResult()
		{
			Span<byte> bytes = stackalloc byte[100];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);

			var success = IdEncoder.TryGetGuid(bytes, out var result); // Cannot know if input is alphanumeric or numeric

			Assert.True(success);
			Assert.Equal(Guid(SampleId), result);
		}

		[Fact]
		public void TryGetGuid_WithTooLongCharInput_ShouldReturnExpectedResult()
		{
			Span<char> chars = stackalloc char[100];
			SampleAlphanumericString.AsSpan().CopyTo(chars);

			var success = IdEncoder.TryGetGuid(chars, out var result); // Cannot know if input is alphanumeric or numeric

			Assert.True(success);
			Assert.Equal(Guid(SampleId), result);
		}

		[Theory]
		[InlineData(0, "0000000000000000000000")]
		[InlineData(1, "0000000000000000000001")]
		[InlineData(Int64.MaxValue, "00000000000AzL8n0Y58m7")]
		[InlineData(UInt64.MaxValue, "00000000000LygHa16AHYF")]
		public void TryGetGuid_Regularly_ShouldOutputExpectedResult(decimal expectedResult, string input)
		{
			var success = IdEncoder.TryGetGuid(input, out var result);
			Assert.True(success);
			Assert.Equal(Guid(expectedResult), result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
		[InlineData("0000000000000000000000")]
		[InlineData("0000000000000000000001")]
		[InlineData("00000000000AzL8n0Y58m7")]
		[InlineData("00000000000LygHa16AHYF")]
		public void GetGuidOrDefault_Regularly_ShouldReturnSameResultAsTryGetGuid(string input)
		{
			var expectedResult = IdEncoder.TryGetGuid(input, out var expectedId)
				? expectedId
				: (Guid?)null;

			var result = IdEncoder.GetGuidOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void AllDecodingMethods_WithInvalidBase62Characters_ShouldFail()
		{
			var bytes = new byte[22];
			SampleAlphanumericBytes.AsSpan().CopyTo(bytes);
			bytes[0] = (byte)'$';

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(11)]
		[InlineData(16)]
		[InlineData(21)]
		public void AllDecodingMethods_WithInsufficientLength_ShouldFail(ushort length)
		{
			var bytes = new byte[length];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Theory]
		[InlineData("123456789012345678901$")]
		[InlineData("123456789012345678901,00")]
		[InlineData("123456789012345678901.00")]
		[InlineData("+1234567890123456789012")]
		[InlineData("-1234567890123456789012")]
		[InlineData("123456789012345678901_")]
		public void AllDecodingMethods_WithInvalidCharacters_ShouldFail(string invalidNumericString)
		{
			var bytes = new byte[invalidNumericString.Length];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)invalidNumericString[i];

			var results = SuccessForAllDecodings(bytes);

			Assert.Equal(results.Length, results.Count(success => !success));
		}

		[Fact]
		public void TryGetGuid_WithBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetGuid(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetGuid_WithChars_ShouldSucceed()
		{
			var success = IdEncoder.TryGetGuid(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetGuidOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetGuidOrDefault(SampleAlphanumericBytes);
			Assert.Equal(Guid(SampleId), result);
		}

		[Fact]
		public void GetGuidOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetGuidOrDefault(SampleAlphanumericString);
			Assert.Equal(Guid(SampleId), result);
		}

		[Fact]
		public void TryGetGuid_WithAlphanumericBytes_ShouldSucceed()
		{
			var success = IdEncoder.TryGetGuid(SampleAlphanumericBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryGetGuid_WithAlphanumericString_ShouldSucceed()
		{
			var success = IdEncoder.TryGetGuid(SampleAlphanumericString, out _);
			Assert.True(success);
		}

		[Fact]
		public void GetGuidOrDefault_WithAlphanumericBytes_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetGuidOrDefault(SampleAlphanumericBytes);
			Assert.Equal(Guid(SampleId), result);
		}

		[Fact]
		public void GetGuidOrDefault_WithAlphanumericString_ShouldReturnExpectedValue()
		{
			var result = IdEncoder.GetGuidOrDefault(SampleAlphanumericString);
			Assert.Equal(Guid(SampleId), result);
		}
	}
}
