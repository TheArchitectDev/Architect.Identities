using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
			var hi = (uint)DecimalStructure.GetHi(components);
			var signAndScale = DecimalStructure.GetSignAndScale(components);

			Span<byte> bytes = stackalloc byte[16];
			BinaryPrimitives.TryWriteInt32LittleEndian(bytes, 0);
			BinaryPrimitives.TryWriteUInt16LittleEndian(bytes[4..], (ushort)(hi >> 16));
			BinaryPrimitives.TryWriteUInt16LittleEndian(bytes[6..], (ushort)hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			var result = new Guid(bytes); // A GUID can be constructed from a big-endian span of bytes

			// For correctness, confirm that the result is the same as that of IdEncoder.GetGuid()
			var check1 = IdEncoder.GetGuid(value);
			Assert.Equal(check1, result);

			// For correctness, confirm that the result is the same as encoding the decimal into alphanumeric (prepended with some 0s) and then decoding into a GUID
			var check2 = IdEncoder.GetGuidOrDefault(value.ToAlphanumeric().PadLeft(22, '0')).Value;
			Assert.Equal(check2, result);

			return result;
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
				IdEncoder.GetGuidOrDefault(bytes) is not null,
				IdEncoder.GetGuidOrDefault(chars) is not null,
			};
		}

		#region Encode GUIDs to/from alphanumeric

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
			var two = (decimal)UInt32.MaxValue;
			var three = UInt32.MaxValue + 1m;
			var four = (decimal)UInt64.MaxValue;
			var five = 18446744073709551616m; // hi=1
			var six = 4722366482869645213696m; // hi=16*16
			var seven = SampleId;
			var eight = DistributedIdGenerator.MaxValue;
			var nine = System.Guid.Parse("00000000-5000-ffff-ffff-ffffffffffff");
			var ten = System.Guid.Parse("00000000-ffff-ffff-ffff-ffffffffffff");
			var eleven = System.Guid.Parse("00000001-ffff-ffff-ffff-ffffffffffff");
			var twelve = System.Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

			var a = IdEncoder.GetAlphanumeric(Guid(one));
			var b = IdEncoder.GetAlphanumeric(Guid(two));
			var c = IdEncoder.GetAlphanumeric(Guid(three));
			var d = IdEncoder.GetAlphanumeric(Guid(four));
			var e = IdEncoder.GetAlphanumeric(Guid(five));
			var f = IdEncoder.GetAlphanumeric(Guid(six));
			var g = IdEncoder.GetAlphanumeric(Guid(seven));
			var h = IdEncoder.GetAlphanumeric(Guid(eight));
			var i = IdEncoder.GetAlphanumeric(nine);
			var j = IdEncoder.GetAlphanumeric(ten);
			var k = IdEncoder.GetAlphanumeric(eleven);
			var l = IdEncoder.GetAlphanumeric(twelve);

			var expectedOrder = new[] { a, b, c, d, e, f, g, h, i, j, k, l };
			var sortedOrder = new[] { k, d, a, g, i, c, h, f, l, b, j, e }; // Start shuffled
			Array.Sort(sortedOrder, StringComparer.Ordinal);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Fact]
		public void GetAlphanumeric_WithOtherIncreasingValues_ShouldReturnOrdinallyIncreasingStrings()
		{
			var guids = new List<Guid>();

			// All except high 32 bits
			for (var i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = IdEncoder.GetGuid(i);
				guids.Add(guid);
			}

			// Into high 32 bits
			for (var x = 1U; x <= UInt32.MaxValue / 2; x *= 2)
			{
				var guid = IdEncoder.GetGuid(0m);
				var uints = MemoryMarshal.Cast<Guid, uint>(MemoryMarshal.CreateSpan(ref guid, length: 1));
				System.Diagnostics.Debug.Assert(uints[0] == 0);
				uints[0] = x;

				guids.Add(guid);
			}

			var sortedGuids = guids.ToList();
			sortedGuids.Sort();

			Assert.Equal(guids.AsEnumerable(), sortedGuids.AsEnumerable());
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
		public void TryGetGuid_AfterGetAlphanumeric_ShouldReverseOperation()
		{
			var guids = new List<Guid>();
			Span<byte> bytes = stackalloc byte[22];

			// All except high 32 bits
			decimal i;
			for (i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = IdEncoder.GetGuid(i);
				IdEncoder.GetAlphanumeric(guid, bytes);
				var result = IdEncoder.TryGetGuid(bytes, out var decoded);
				Assert.True(result);
				Assert.Equal(guid, decoded);
			}

			// Into high 32 bits
			for (var x = 1U; x <= UInt32.MaxValue / 2; x *= 2)
			{
				var guid = IdEncoder.GetGuid(i);
				var uints = MemoryMarshal.Cast<Guid, uint>(MemoryMarshal.CreateSpan(ref guid, length: 1));
				System.Diagnostics.Debug.Assert(uints[0] == 0);
				uints[0] = x;

				IdEncoder.GetAlphanumeric(guid, bytes);
				var result = IdEncoder.TryGetGuid(bytes, out var decoded);
				Assert.True(result);
				Assert.Equal(guid, decoded);
			}
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

		#endregion

		#region Encode numeric IDs to/from GUID

		[Fact]
		public void GetGuid_FromUlongWithIncreasingValues_ShouldReturnIncreasingGuids()
		{
			var one = UInt32.MaxValue - 1UL;
			var two = (ulong)UInt32.MaxValue;
			var three = UInt32.MaxValue + 1UL;
			var four = (1UL << 48) - 1UL;
			var five = 1UL << 48;
			var six = (ulong)Int64.MaxValue;
			var seven = UInt64.MaxValue;

			var a = IdEncoder.GetGuid(one);
			var b = IdEncoder.GetGuid(two);
			var c = IdEncoder.GetGuid(three);
			var d = IdEncoder.GetGuid(four);
			var e = IdEncoder.GetGuid(five);
			var f = IdEncoder.GetGuid(six);
			var g = IdEncoder.GetGuid(seven);

			var expectedOrder = new[] { a, b, c, d, e, f, g };
			var sortedOrder = new[] { d, a, g, c, f, b, e }; // Start shuffled
			Array.Sort(sortedOrder);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Fact]
		public void GetGuid_FromDecimalWithIncreasingValues_ShouldReturnIncreasingGuids()
		{
			var one = (decimal)UInt32.MaxValue;
			var two = UInt32.MaxValue + 1m;
			var three  = (1UL << 48) - 1m;
			var four = (decimal)(1UL << 48);
			var five = 18446744073709551616m; // hi=1
			var six = 4722366482869645213696m; // hi=16*16
			var seven = DistributedIdGenerator.MaxValue;

			var a = IdEncoder.GetGuid(one);
			var b = IdEncoder.GetGuid(two);
			var c = IdEncoder.GetGuid(three);
			var d = IdEncoder.GetGuid(four);
			var e = IdEncoder.GetGuid(five);
			var f = IdEncoder.GetGuid(six);
			var g = IdEncoder.GetGuid(seven);

			var expectedOrder = new[] { a, b, c, d, e, f, g };
			var sortedOrder = new[] { d, a, g, c, f, b, e }; // Start shuffled
			Array.Sort(sortedOrder);

			Assert.Equal(expectedOrder, sortedOrder);
		}

		[Fact]
		public void GetGuid_FromDecimalWithOtherIncreasingValues_ShouldReturnIncreasingGuids()
		{
			var guids = new List<Guid>();
			for (var i = 1m; i <= DistributedIdGenerator.MaxValue / 16; i *= 16)
			{
				var guid = IdEncoder.GetGuid(i);
				guids.Add(guid);
			}
			var sortedGuids = guids.ToList();
			sortedGuids.Sort();

			Assert.Equal(guids.AsEnumerable(), sortedGuids.AsEnumerable());
		}

		[Theory]
		[InlineData(1UL)]
		[InlineData(2UL)]
		[InlineData(UInt32.MaxValue - 1UL)]
		[InlineData(UInt32.MaxValue)]
		[InlineData(UInt32.MaxValue + 1UL)]
		[InlineData((1UL << 48) - 1UL)]
		[InlineData(1UL << 48)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetGuid_FromUlong_ShouldMatchRightHalfOfGuidFromDecimal(ulong value)
		{
			var result = IdEncoder.GetGuid(value);
			var resultRightHalf = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateReadOnlySpan(ref result, length: 1))[1];

			var expectedResult = IdEncoder.GetGuid((decimal)value);
			var expectedResultRightHalf = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateReadOnlySpan(ref expectedResult, length: 1))[1];

			Assert.Equal(expectedResultRightHalf, resultRightHalf);
		}

		[Theory]
		[InlineData(Int64.MaxValue - 1UL, Int64.MaxValue)] // Left and right differ (and left is non-zero)
		[InlineData(1111111111111111UL, 0000000000000001UL)] // Wrong half set to zero
		public void TryGetUlong_WithInvalidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetUlong(guid, out var decoded);

			Assert.False(result);
			Assert.Equal(default, decoded);
		}

		[Theory]
		[InlineData(Int64.MaxValue + 1UL, Int64.MaxValue + 1UL)] // Over Int64.MaxValue (i.e. negative long)
		[InlineData(1111111111111111UL, 0000000000000001UL)] // Wrong half set to zero
		public void TryGetLong_WithInvalidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetLong(guid, out var decoded);

			Assert.False(result);
			Assert.Equal(default, decoded);
		}

		[Theory]
		[InlineData(UInt64.MaxValue >> 32, UInt64.MaxValue)] // Exceeds max value
		[InlineData(UInt64.MaxValue >> 31, UInt64.MaxValue)] // Has upper 32 bits (SignAndScale)
		[InlineData(UInt64.MaxValue, UInt64.MaxValue)] // Has upper 32 bits (SignAndScale)
		public void TryGetDecimal_WithInvalidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetDecimal(guid, out var decoded);

			Assert.False(result);
			Assert.Equal(default, decoded);
		}

		[Theory]
		[InlineData(0UL, 0UL)] // Min value
		[InlineData(0UL, 1UL)] // Near-min
		[InlineData(0UL, Int64.MaxValue)] // Near-max value
		[InlineData(0UL, UInt64.MaxValue)] // Max value
		public void TryGetUlong_WithValidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetUlong(guid, out _);

			Assert.True(result);
		}

		[Theory]
		[InlineData(0UL, 0UL)] // Min value
		[InlineData(0UL, 1UL)] // Near-min
		[InlineData(0UL, Int64.MaxValue)] // Max value
		public void TryGetLong_WithValidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetLong(guid, out _);

			Assert.True(result);
		}

		[Theory]
		[InlineData(0UL, 0UL)]
		[InlineData(0UL, 1UL)]
		[InlineData(0UL, UInt64.MaxValue)]
		public void TryGetDecimal_WithValidInput_ShouldReturnExpectedResult(ulong firstHalf, ulong secondHalf)
		{
			var ulongs = new[] { firstHalf, BinaryPrimitives.ReverseEndianness(secondHalf) };
			var guid = MemoryMarshal.Cast<ulong, Guid>(ulongs)[0];

			var result = IdEncoder.TryGetDecimal(guid, out _);

			Assert.True(result);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void TryGetLong_AfterGetGuid_ShouldReverseOperation(long id)
		{
			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.TryGetLong(encoded, out var decoded);

			Assert.True(result);
			Assert.Equal(id, decoded);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void TryGetUlong_AfterGetGuid_ShouldReverseOperation(long longId)
		{
			var id = (ulong)longId;

			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.TryGetUlong(encoded, out var decoded);

			Assert.True(result);
			Assert.Equal(id, decoded);
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void TryGetDecimal_AfterGetGuid_ShouldReverseOperation(ulong ulongId)
		{
			var id = (decimal)ulongId;

			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.TryGetUlong(encoded, out var decoded);

			Assert.True(result);
			Assert.Equal(id, decoded);
		}

		[Fact]
		public void TryGetDecimal_AfterGetGuidWithLargeDecimal_ShouldReverseOperation()
		{
			var id = 4722366482869645213696m; // hi=16*16

			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.TryGetDecimal(encoded, out var decoded);

			Assert.True(result);
			Assert.Equal(id, decoded);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void GetLongOrDefault_AfterGetGuid_ShouldReverseOperation(long id)
		{
			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.GetLongOrDefault(encoded);

			Assert.Equal(id, result);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void GetUlongOrDefault_AfterGetGuid_ShouldReverseOperation(long longId)
		{
			var id = (ulong)longId;

			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.GetUlongOrDefault(encoded);

			Assert.Equal(id, result);
		}

		[Theory]
		[InlineData(0UL)]
		[InlineData(1UL)]
		[InlineData(61UL)]
		[InlineData(62UL)]
		[InlineData(9999999999999999999UL)] // 19 digits
		[InlineData(10000000000000000000UL)] // 20 digits
		public void GetDecimalOrDefault_AfterGetGuid_ShouldReverseOperation(ulong ulongId)
		{
			var id = (decimal)ulongId;

			var encoded = IdEncoder.GetGuid(id);
			var result = IdEncoder.GetDecimalOrDefault(encoded);

			Assert.Equal(id, result);
		}

		#endregion
	}
}
