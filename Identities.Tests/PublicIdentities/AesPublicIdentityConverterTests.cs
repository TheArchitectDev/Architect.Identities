using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Architect.Identities.PublicIdentities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.PublicIdentities
{
	public sealed class AesPublicIdentityConverterTests : IDisposable
	{
		private AesPublicIdentityConverter Converter { get; } = new AesPublicIdentityConverter(new byte[32]);

		public void Dispose()
		{
			this.Converter.Dispose();
		}
		
		[Fact]
		public void Construct_With16ByteKey_ShouldSucceed()
		{
			new AesPublicIdentityConverter(new byte[16]).Dispose();
		}
		
		[Fact]
		public void Construct_With32ByteKey_ShouldSucceed()
		{
			new AesPublicIdentityConverter(new byte[32]).Dispose();
		}
		
		[Fact]
		public void Construct_With15ByteKey_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new AesPublicIdentityConverter(new byte[15]));
		}
		
		[Fact]
		public void Construct_With33ByteKey_ShouldThrow()
		{
			Assert.Throws<CryptographicException>(() => new AesPublicIdentityConverter(new byte[33]));
		}

		[Fact]
		public void Construct_WithSameKey_ShouldGenerateSameResults()
		{
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key1.AsSpan().CopyTo(key2);

			var string1 = new AesPublicIdentityConverter(key1).GetPublicString(0);
			var string2 = new AesPublicIdentityConverter(key2).GetPublicString(0);

			Assert.Equal(string1, string2); // A second custom key gives the same result as an identical first custom key
		}

		[Fact]
		public void AddPublicIdentities_WithDifferentKey_ShouldGenerateDifferentResults()
		{
			var key0 = new byte[32];
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key2[31] = 2;

			var string0 = new AesPublicIdentityConverter(key0).GetPublicString(0);
			var string1 = new AesPublicIdentityConverter(key1).GetPublicString(0);
			var string2 = new AesPublicIdentityConverter(key2).GetPublicString(0);

			Assert.NotEqual(string0, string1); // A custom key gives a different result than an empty key
			Assert.NotEqual(string1, string2); // A second custom key gives a different result than the first custom key
		}
		
		[Fact]
		public void GetPublicBytes_With15ByteOutput_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => this.Converter.GetPublicBytes(0, stackalloc byte[15]));
		}

		[Fact]
		public void GetPublicBytes_WithNegativeId_ShouldThrow()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => this.Converter.GetPublicBytes(-1, stackalloc byte[16]));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(100)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicBytes_Regularly_ShouldReturnHighEntropyValue(ulong id)
		{
			var output = new byte[16];
			this.Converter.GetPublicBytes(id, output);

			var distinctByteCount = output.Distinct().Count();

			// It is likely that the 16 bytes contain at least 14 distinct values
			Assert.True(distinctByteCount >= 14);
		}

		[Fact]
		public void GetPublicBytes_WithSmallDifferenceInInput_ShouldReturnRadicallyDifferentOutput()
		{
			var output1 = new byte[16];
			var output2 = new byte[16];
			this.Converter.GetPublicBytes(1UL, output1);
			this.Converter.GetPublicBytes(2UL, output2);

			var numBytesIn1ButNotIn2 = output1.ToArray().Count(b => !output2.Contains(b));

			// It is likely that the first result contains at least 14 bytes that the second does not
			Assert.True(numBytesIn1ButNotIn2 >= 14);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicBytes_WithUlongInput_ShouldReturnZeroKeyAesEncryption(ulong id)
		{
			Span<byte> output = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, output);

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = new byte[16];
			var aesOutput = new byte[16];
			MemoryMarshal.Write(aesInput.AsSpan().Slice(8), ref id);
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.True(output.SequenceEqual(aesOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicBytes_WithLongInput_ShouldReturnZeroKeyAesEncryption(long id)
		{
			Span<byte> output = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, output);

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = new byte[16];
			var aesOutput = new byte[16];
			MemoryMarshal.Write(aesInput.AsSpan().Slice(8), ref id);
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.True(output.SequenceEqual(aesOutput));
		}

		[Fact]
		public void GetPublicBytes_WithOversizedLongInput_ShouldThrow()
		{
			var ulongId = UInt64.MaxValue;
			var id = (long)ulongId;

			Assert.Throws<ArgumentOutOfRangeException>(() => this.Converter.GetPublicBytes(id, stackalloc byte[16]));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicString_WithUlongInputAndByteOutput_ShouldMatchGetPublicBytes(ulong id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> base62Output = stackalloc byte[32];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			this.Converter.GetPublicString(id, base62Output);

			Hexadecimal.FromHexChars(base62Output, byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicString_WithLongInputAndByteOutput_ShouldMatchGetPublicBytes(long id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> base62Output = stackalloc byte[32];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			this.Converter.GetPublicString(id, base62Output);

			Hexadecimal.FromHexChars(base62Output, byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicString_WithUlongInputAndStringOutput_ShouldMatchGetPublicBytes(ulong id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			var stringOutput = this.Converter.GetPublicString(id);

			Hexadecimal.FromHexChars(System.Text.Encoding.ASCII.GetBytes(stringOutput), byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicString_WithLongInputAndStringOutput_ShouldMatchGetPublicBytes(long id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			var stringOutput = this.Converter.GetPublicString(id);

			Hexadecimal.FromHexChars(System.Text.Encoding.ASCII.GetBytes(stringOutput), byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicShortString_WithUlongInputAndByteOutput_ShouldMatchGetPublicBytes(ulong id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> base62Output = stackalloc byte[22];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			this.Converter.GetPublicShortString(id, base62Output);

			Base62.FromBase62Chars(base62Output, byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicShortString_WithLongInputAndByteOutput_ShouldMatchGetPublicBytes(long id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> base62Output = stackalloc byte[22];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			this.Converter.GetPublicShortString(id, base62Output);

			Base62.FromBase62Chars(base62Output, byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicShortString_WithUlongInputAndStringOutput_ShouldMatchGetPublicBytes(ulong id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			var stringOutput = this.Converter.GetPublicShortString(id);

			Base62.FromBase62Chars(System.Text.Encoding.ASCII.GetBytes(stringOutput), byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicShortString_WithLongInputAndStringOutput_ShouldMatchGetPublicBytes(long id)
		{
			Span<byte> expectedOutput = stackalloc byte[16];
			Span<byte> byteOutput = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, expectedOutput);
			var stringOutput = this.Converter.GetPublicShortString(id);

			Base62.FromBase62Chars(System.Text.Encoding.ASCII.GetBytes(stringOutput), byteOutput);

			Assert.True(expectedOutput.SequenceEqual(byteOutput));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void TryGetUlong_AfterGetPublicBytes_ShouldReturnOriginalid(ulong id)
		{
			Span<byte> output = stackalloc byte[16];
			this.Converter.GetPublicBytes(id, output);
			var decodingSucceeded = this.Converter.TryGetUlong(output, out var decodedId);

			Assert.True(decodingSucceeded);
			Assert.Equal(id, decodedId);
		}

		[Theory]
		[InlineData("3JXAeKJAiYmtSKIUkoQghw==")] // 0
		[InlineData("SBbv496zgFZuugwXv1ggkA==")] // 1
		[InlineData("esvpprBcqvc275u+r3rxFw==")] // Int64.MaxValue
		[InlineData("+svpprBcqvc275u+r3rxFw==")] // Invalid
		public void TryGetLong_Regularly_ShouldMatchTryGetUlong(string base64PublicId)
		{
			var inputBytes = Convert.FromBase64String(base64PublicId);
			var decodingShouldSucceed = this.Converter.TryGetUlong(inputBytes, out var expectedId);
			var decodingSucceeded = this.Converter.TryGetLong(inputBytes, out var decodedId);

			Assert.Equal(decodingShouldSucceed, decodingSucceeded);
			Assert.Equal(expectedId, (ulong)decodedId);
		}

		[Fact]
		public void TryGetLong_WithOverflowingValue_ShouldReturnFalse()
		{
			var inputBytes = Convert.FromBase64String("Ve12lI0ohr//UOM1K/40/Q=="); // UInt64.MaxValue
			var decodingSucceeded = this.Converter.TryGetLong(inputBytes, out var decodedId);
			Assert.False(decodingSucceeded);
			Assert.True(decodedId == 0);
		}

		[Theory]
		[InlineData("3JXAeKJAiYmtSKIUkoQghw==")] // 0
		[InlineData("SBbv496zgFZuugwXv1ggkA==")] // 1
		[InlineData("esvpprBcqvc275u+r3rxFw==")] // Int64.MaxValue
		[InlineData("+svpprBcqvc275u+r3rxFw==")] // Invalid
		public void GetUlongOrDefault_Regularly_ShouldMatchTryGetUlong(string base64PublicId)
		{
			var inputBytes = Convert.FromBase64String(base64PublicId);
			this.Converter.TryGetUlong(inputBytes, out var expectedId);
			var decodedId = this.Converter.GetUlongOrDefault(inputBytes);

			Assert.Equal(expectedId, decodedId);
		}

		[Theory]
		[InlineData("3JXAeKJAiYmtSKIUkoQghw==")] // 0
		[InlineData("SBbv496zgFZuugwXv1ggkA==")] // 1
		[InlineData("esvpprBcqvc275u+r3rxFw==")] // Int64.MaxValue
		[InlineData("+svpprBcqvc275u+r3rxFw==")] // Invalid
		public void GetLongOrDefault_Regularly_ShouldMatchTryGetUlong(string base64PublicId)
		{
			var inputBytes = Convert.FromBase64String(base64PublicId);
			this.Converter.TryGetUlong(inputBytes, out var expectedId);
			var decodedId = this.Converter.GetLongOrDefault(inputBytes);

			Assert.Equal(expectedId, (ulong)decodedId);
		}

		[Fact]
		public void GetLongOrDefault_WithOverflowingValue_ShouldReturnFalse()
		{
			var inputBytes = Convert.FromBase64String("Ve12lI0ohr//UOM1K/40/Q=="); // UInt64.MaxValue
			var decodedId = this.Converter.GetLongOrDefault(inputBytes);
			Assert.True(decodedId == 0);
		}

		#region Nonsensical input

		[Theory]
		[InlineData("123456789012345")] // Length 15
		[InlineData("1234567890123456")] // Length 16 (only allowed for binary input)
		[InlineData("______________________")] // 22 non-base64 chars
		[InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")] // 32 non-hex chars
		[InlineData("ëëëëëëëëëëëëëëëëëëëëëëëëëëëëëëëë")] // 16 non-ASCII chars
		[InlineData("123456789012345678901234567890123")] // 33 chars
		public void TryGetLong_WithInvalidInput_ShouldReturnFalse(string input)
		{
			var decodingSucceeded = this.Converter.TryGetLong(input, out _);

			Assert.False(decodingSucceeded);
		}

		#endregion
	}
}
