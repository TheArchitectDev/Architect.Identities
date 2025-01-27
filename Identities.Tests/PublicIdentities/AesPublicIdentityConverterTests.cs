using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace Architect.Identities.Tests.PublicIdentities
{
	public sealed class AesPublicIdentityConverterTests : IDisposable
	{
#pragma warning disable CA1859 // Use concrete types when possible for improved performance -- Would prevent the use of interface methods with default implementation
		private IPublicIdentityConverter Converter { get; } = new AesPublicIdentityConverter(new byte[32]);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

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

			var string1 = new AesPublicIdentityConverter(key1).GetPublicRepresentation(0UL);
			var string2 = new AesPublicIdentityConverter(key2).GetPublicRepresentation(0UL);

			Assert.Equal(string1, string2); // A second custom key gives the same result as an identical first custom key
		}

		/// <summary>
		/// Confirms reusability of crypto components.
		/// </summary>
		[Fact]
		public void GetPublicRepresentation_Twice_ShouldYieldSameResult()
		{
			var key = new byte[32];
			var converter = new AesPublicIdentityConverter(key);

			var first = converter.GetPublicRepresentation(1UL);
			var second = converter.GetPublicRepresentation(1UL);

			Assert.Equal(second, first);
		}

		/// <summary>
		/// Confirms reusability of crypto components.
		/// </summary>
		[Fact]
		public void TryGetUlong_Twice_ShouldYieldSameResult()
		{
			var key = new byte[32];
			var converter = new AesPublicIdentityConverter(key);
			var publicId = converter.GetPublicRepresentation(1UL);

			var firstSucceeded = converter.TryGetUlong(publicId, out var first);
			var secondSucceeded = converter.TryGetUlong(publicId, out var second);

			Assert.True(firstSucceeded);
			Assert.True(secondSucceeded);
			Assert.Equal(second, first);
		}

		[Fact]
		public void AddPublicIdentities_WithDifferentKey_ShouldGenerateDifferentResults()
		{
			var key0 = new byte[32];
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key2[31] = 2;

			var string0 = new AesPublicIdentityConverter(key0).GetPublicRepresentation(0UL);
			var string1 = new AesPublicIdentityConverter(key1).GetPublicRepresentation(0UL);
			var string2 = new AesPublicIdentityConverter(key2).GetPublicRepresentation(0UL);

			Assert.NotEqual(string0, string1); // A custom key gives a different result than an empty key
			Assert.NotEqual(string1, string2); // A second custom key gives a different result than the first custom key
		}

		[Fact]
		public void GetPublicRepresentation_WithNegativeLongId_ShouldThrow()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => this.Converter.GetPublicRepresentation(-1));
		}

		[Fact]
		public void GetPublicRepresentation_WithNegativeDecimalId_ShouldThrow()
		{
			// Should throw same exception as for negative longs
			Assert.Throws<ArgumentOutOfRangeException>(() => this.Converter.GetPublicRepresentation(-1m));
		}

		[Fact]
		public void GetPublicRepresentation_WithOversizedDecimalId_ShouldThrow()
		{
			// ArgumentException indicates that input did not come from the expected creation method
			var id = 1 + DistributedIdGenerator.MaxValue;
			Assert.Throws<ArgumentException>(() => this.Converter.GetPublicRepresentation(id));
		}

		[Fact]
		public void GetPublicRepresentation_WithScaledDecimalId_ShouldThrow()
		{
			// ArgumentException indicates that the decimal is wrong, not necessarily out of range
			var id = new decimal(lo: 0, mid: 0, hi: 0, isNegative: false, scale: 1);
			Assert.Throws<ArgumentException>(() => this.Converter.GetPublicRepresentation(id));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(100)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicRepresentation_Regularly_ShouldReturnHighEntropyValue(ulong id)
		{
			var publicId = this.Converter.GetPublicRepresentation(id);
			var publicIdbytes = publicId.ToByteArray();

			var distinctByteCount = publicIdbytes.Distinct().Count();

			// It is likely that the 16 bytes contain at least 14 distinct values (except for 1 in 16M cases)
			Assert.True(distinctByteCount >= 14);
		}

		[Fact]
		public void GetPublicRepresentation_WithSmallDifferenceInInput_ShouldReturnRadicallyDifferentOutput()
		{
			var publicId1 = this.Converter.GetPublicRepresentation(1UL);
			var publicId2 = this.Converter.GetPublicRepresentation(2UL);

			var publicIdBytes1 = publicId1.ToByteArray();
			var publicIdBytes2 = publicId2.ToByteArray();

			var numBytesIn1ButNotIn2 = publicIdBytes1.ToArray().Count(b => !publicIdBytes2.Contains(b));

			// It is likely that the first result contains at least 14 bytes that the second does not
			Assert.True(numBytesIn1ButNotIn2 >= 14);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetPublicRepresentation_WithUlongInput_ShouldReturnZeroKeyAesEncryption(ulong id)
		{
			var publicId = this.Converter.GetPublicRepresentation(id);
			var publicIdBytes = publicId.ToByteArray();

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = new byte[16];
			var aesOutput = new byte[16];
			MemoryMarshal.Write(aesInput.AsSpan()[8..], in id);
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.Equal(aesOutput, publicIdBytes);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicRepresentation_WithLongInput_ShouldReturnZeroKeyAesEncryption(long id)
		{
			var publicId = this.Converter.GetPublicRepresentation(id);
			var publicIdBytes = publicId.ToByteArray();

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = new byte[16];
			var aesOutput = new byte[16];
			MemoryMarshal.Write(aesInput.AsSpan()[8..], in id);
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.Equal(aesOutput, publicIdBytes);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetPublicRepresentation_WithDecimalInput_ShouldReturnZeroKeyAesEncryption(decimal id)
		{
			var publicId = this.Converter.GetPublicRepresentation(id);
			var publicIdBytes = publicId.ToByteArray();

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var ulongId = (ulong)id;
			var aesInput = new byte[16];
			var aesOutput = new byte[16];
			MemoryMarshal.Write(aesInput.AsSpan()[8..], in ulongId);
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.Equal(aesOutput, publicIdBytes);
		}

		[Fact]
		public void GetPublicRepresentation_WithOversizedAkaNegativeLongInput_ShouldThrow()
		{
			var ulongId = UInt64.MaxValue;
			var id = (long)ulongId;

			Assert.Throws<ArgumentOutOfRangeException>(() => this.Converter.GetPublicRepresentation(id));
		}

		[Fact]
		public void GetPublicRepresentation_WithOversizedDecimalInput_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => this.Converter.GetPublicRepresentation(1m + DistributedIdGenerator.MaxValue));
		}

		[Fact]
		public void GetPublicRepresentation_WithLargeDecimalInput_ShouldSucceed()
		{
			var ulongId = UInt64.MaxValue;
			var id = (decimal)ulongId;

			this.Converter.GetPublicRepresentation(id);
		}

		[Fact]
		public void GetPublicRepresentation_WithMaximumDecimalInput_ShouldSucceed()
		{
			this.Converter.GetPublicRepresentation(DistributedIdGenerator.MaxValue);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void GetPublicRepresentation_WithUInt128Input_ShouldReturnZeroKeyAesEncryption(string idString)
		{
			var id = UInt128.Parse(idString);

			var publicId = this.Converter.GetPublicRepresentation(id);
			var publicIdBytes = publicId.ToByteArray();

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = BinaryIdEncoder.Encode(id);
			var aesOutput = new byte[16];
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.Equal(aesOutput, publicIdBytes);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void GetPublicRepresentation_WithGuidInput_ShouldReturnZeroKeyAesEncryption(string idString)
		{
			var id = UInt128.Parse(idString);
			var idBytes = BinaryIdEncoder.Encode(id);
			var guid = BinaryIdEncoder.DecodeGuidOrDefault(idBytes) ?? throw new ArgumentException("This should have succeeded.");

			var publicId = this.Converter.GetPublicRepresentation(guid);
			var publicIdBytes = publicId.ToByteArray();

			using var aes = Aes.Create();
			using var encryptor = aes.CreateEncryptor(new byte[32], new byte[16]);

			var aesInput = idBytes;
			var aesOutput = new byte[16];
			encryptor.TransformBlock(aesInput, 0, 16, aesOutput, 0);

			Assert.Equal(aesOutput, publicIdBytes);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void TryGetUlong_AfterGetPublicRepresentation_ShouldReturnOriginalId(ulong id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingSucceeded = this.Converter.TryGetUlong(guid, out var decodedId);

			Assert.True(decodingSucceeded);
			Assert.Equal(id, decodedId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void TryGetDecimal_AfterGetPublicRepresentation_ShouldReturnOriginalId(decimal id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingSucceeded = this.Converter.TryGetUlong(guid, out var decodedId);

			Assert.True(decodingSucceeded);
			Assert.Equal(id, decodedId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void TryGetUInt128_AfterGetPublicRepresentation_ShouldReturnOriginalId(string idString)
		{
			var id = UInt128.Parse(idString);

			var publicId = this.Converter.GetPublicRepresentation(id);
			var decodingSucceeded = this.Converter.TryGetUInt128(publicId, out var decodedId);

			Assert.True(decodingSucceeded);
			Assert.Equal(id, decodedId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void TryGetGuid_AfterGetPublicRepresentation_ShouldReturnOriginalId(string idString)
		{
			var id = UInt128.Parse(idString);
			var guid = BinaryIdEncoder.DecodeGuidOrDefault(BinaryIdEncoder.Encode(id)) ?? throw new ArgumentException("This should have succeeded.");

			var publicId = this.Converter.GetPublicRepresentation(guid);
			var decodingSucceeded = this.Converter.TryGetUInt128(publicId, out var decodedId);

			Assert.True(decodingSucceeded);
			Assert.Equal(id, decodedId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void TryGetLong_Regularly_ShouldMatchTryGetUlong(long id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingShouldSucceed = this.Converter.TryGetUlong(guid, out var expectedId);
			var decodingSucceeded = this.Converter.TryGetLong(guid, out var decodedId);

			Assert.Equal(decodingShouldSucceed, decodingSucceeded);
			Assert.Equal(expectedId, (ulong)decodedId);
		}

		[Fact]
		public void TryGetLong_WithOverflowingValue_ShouldReturnFalse()
		{
			var guid = this.Converter.GetPublicRepresentation(UInt64.MaxValue);
			var decodingSucceeded = this.Converter.TryGetLong(guid, out var decodedId);

			Assert.False(decodingSucceeded);
			Assert.Equal(0, decodedId);
		}

		[Fact]
		public void TryGetDecimal_WithOverflowingValue_ShouldReturnFalse()
		{
			var guid = Guid.Parse("3625a4bb-7bd6-624d-108d-9d9e599d536b");
			var decodingSucceeded = this.Converter.TryGetDecimal(guid, out var decodedId);

			Assert.False(decodingSucceeded);
			Assert.Equal(0, decodedId);
		}

		[Fact]
		public void TryGetDecimal_WithInvalidValue_ShouldReturnNull()
		{
			var guid = Guid.Parse("378c4fe3-a619-dea4-93b8-489f3160e863");
			var decodingSucceeded = this.Converter.TryGetDecimal(guid, out var decodedId);

			Assert.False(decodingSucceeded);
			Assert.Equal(0, decodedId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetUlongOrDefault_Regularly_ShouldMatchTryGetUlong(ulong id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingShouldSucceed = this.Converter.TryGetUlong(guid, out var expectedId);

			var decodedId = this.Converter.GetUlongOrDefault(guid);

			Assert.Equal(decodingShouldSucceed, decodedId is not null);
			Assert.Equal(expectedId, decodedId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		public void GetLongOrDefault_Regularly_ShouldMatchTryGetLong(long id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingShouldSucceed = this.Converter.TryGetLong(guid, out var expectedId);

			var decodedId = this.Converter.GetLongOrDefault(guid);

			Assert.Equal(decodingShouldSucceed, decodedId is not null);
			Assert.Equal(expectedId, decodedId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(Int64.MaxValue)]
		[InlineData(UInt64.MaxValue)]
		public void GetDecimalOrDefault_Regularly_ShouldMatchTryGetDecimal(ulong id)
		{
			var guid = this.Converter.GetPublicRepresentation(id);
			var decodingShouldSucceed = this.Converter.TryGetDecimal(guid, out var expectedId);

			var decodedId = this.Converter.GetDecimalOrDefault(guid);

			Assert.Equal(decodingShouldSucceed, decodedId is not null);
			Assert.Equal(expectedId, decodedId);
		}

		[Fact]
		public void GetLongOrDefault_WithOverflowingValue_ShouldReturnNull()
		{
			var guid = this.Converter.GetPublicRepresentation(UInt64.MaxValue);

			var decodedId = this.Converter.GetLongOrDefault(guid);

			Assert.Null(decodedId);
		}

		[Fact]
		public void GetDecimalOrDefault_WithOverflowingValue_ShouldReturnNull()
		{
			var guid = Guid.Parse("3625a4bb-7bd6-624d-108d-9d9e599d536b");
			var decodedId = this.Converter.GetDecimalOrDefault(guid);

			Assert.Null(decodedId);
		}

		[Fact]
		public void GetDecimalOrDefault_WithInvalidValue_ShouldReturnNull()
		{
			var guid = Guid.Parse("378c4fe3-a619-dea4-93b8-489f3160e863");
			var decodedId = this.Converter.GetDecimalOrDefault(guid);

			Assert.Null(decodedId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void GetUInt128OrDefault_Regularly_ShouldMatchTryGetUInt128(string idString)
		{
			var id = UInt128.Parse(idString);

			var publicId = this.Converter.GetPublicRepresentation(id);
			var decodingShouldSucceed = this.Converter.TryGetUInt128(publicId, out var expectedId);

			var decodedId = this.Converter.GetUInt128OrDefault(publicId);

			Assert.Equal(decodingShouldSucceed, decodedId is not null);
			Assert.Equal(expectedId, decodedId);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("18446744073709551616")] // UInt64.MaxValue + 1
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void GetGuidOrDefault_Regularly_ShouldMatchTryGetGuid(string idString)
		{
			var id = UInt128.Parse(idString);
			var guid = BinaryIdEncoder.DecodeGuidOrDefault(BinaryIdEncoder.Encode(id)) ?? throw new ArgumentException("This should have succeeded.");

			var publicId = this.Converter.GetPublicRepresentation(guid);
			var decodingShouldSucceed = this.Converter.TryGetGuid(publicId, out var expectedId);

			var decodedId = this.Converter.GetGuidOrDefault(publicId);

			Assert.Equal(decodingShouldSucceed, decodedId is not null);
			Assert.Equal(expectedId, decodedId);
		}
	}
}
