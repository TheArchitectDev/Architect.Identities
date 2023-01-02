using Xunit;

namespace Architect.Identities.Tests.Encodings;

public class BinaryIdEncoderTests
{
	[Theory]
	[InlineData(0UL, "0000000000000000")]
	[InlineData(1UL, "0000000000000001")]
	[InlineData(Int64.MaxValue, "7FFFFFFFFFFFFFFF")]
	public void Encode_Regularly_ShouldReturnExpectedResult(decimal id, string expectedHexResult)
	{
		var longResult = new byte[8];
		var ulongResult = new byte[8];
		var decimalResult = new byte[16];
		var guidResult = new byte[16];

		BinaryIdEncoder.Encode((long)id, longResult);
		BinaryIdEncoder.Encode((ulong)id, ulongResult);
		BinaryIdEncoder.Encode(id, decimalResult);
		BinaryIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id), guidResult);

		Assert.Equal(expectedHexResult.PadLeft(2 * 8, '0'), Convert.ToHexString(longResult));
		Assert.Equal(expectedHexResult.PadLeft(2 * 8, '0'), Convert.ToHexString(ulongResult));
		Assert.Equal(expectedHexResult.PadLeft(2 * 16, '0'), Convert.ToHexString(decimalResult));
		Assert.Equal(expectedHexResult.PadLeft(2 * 16, '0'), Convert.ToHexString(guidResult));
	}

	[Fact]
	public void Encode_WithTooLongOutput_ShouldSucceed()
	{
		BinaryIdEncoder.Encode(0U, stackalloc byte[100]);
		BinaryIdEncoder.Encode(0UL, stackalloc byte[100]);
		BinaryIdEncoder.Encode(0m, stackalloc byte[100]);
		BinaryIdEncoder.Encode(default(Guid), stackalloc byte[100]);
	}

	[Fact]
	public void Encode_WithTooShortOutput_ShouldThrow()
	{
		Assert.Throws<IndexOutOfRangeException>(() => BinaryIdEncoder.Encode(0U, stackalloc byte[7]));
		Assert.Throws<IndexOutOfRangeException>(() => BinaryIdEncoder.Encode(0UL, stackalloc byte[7]));
		Assert.Throws<IndexOutOfRangeException>(() => BinaryIdEncoder.Encode(0m, stackalloc byte[15]));
		Assert.Throws<IndexOutOfRangeException>(() => BinaryIdEncoder.Encode(default(Guid), stackalloc byte[15]));
	}

	[Theory]
	[InlineData(0UL)]
	[InlineData(1UL)]
	[InlineData(Int64.MaxValue)]
	public void TryDecode_AfterEncode_ShouldBeReversible(decimal id)
	{
		var longBytes = new byte[8];
		var ulongBytes = new byte[8];
		var decimalBytes = new byte[16];
		var guidBytes = new byte[16];

		BinaryIdEncoder.Encode((long)id, longBytes);
		BinaryIdEncoder.Encode((ulong)id, ulongBytes);
		BinaryIdEncoder.Encode(id, decimalBytes);
		BinaryIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id), guidBytes);

		var longSuccess = BinaryIdEncoder.TryDecodeLong(longBytes, out var longResult);
		var ulongSuccess = BinaryIdEncoder.TryDecodeUlong(ulongBytes, out var ulongResult);
		var decimalSuccess = BinaryIdEncoder.TryDecodeDecimal(decimalBytes, out var decimalResult);
		var guidSuccess = BinaryIdEncoder.TryDecodeGuid(guidBytes, out var guidResult);

		Assert.True(longSuccess);
		Assert.True(ulongSuccess);
		Assert.True(decimalSuccess);
		Assert.True(guidSuccess);

		Assert.Equal((long)id, longResult);
		Assert.Equal((ulong)id, ulongResult);
		Assert.Equal(id, decimalResult);
		Assert.Equal(AlphanumericIdEncoderTests.Guid(id), guidResult);

		Assert.Equal(longResult, BinaryIdEncoder.DecodeLongOrDefault(longBytes));
		Assert.Equal(ulongResult, BinaryIdEncoder.DecodeUlongOrDefault(ulongBytes));
		Assert.Equal(decimalResult, BinaryIdEncoder.DecodeDecimalOrDefault(decimalBytes));
		Assert.Equal(guidResult, BinaryIdEncoder.DecodeGuidOrDefault(guidBytes));
	}

	[Theory]
	[InlineData(0UL)]
	[InlineData(1UL)]
	[InlineData(Int64.MaxValue)]
	public void DecodeOrDefault_Regularly_ShouldMatchTryDecode(decimal id)
	{
		var longBytes = new byte[8];
		var ulongBytes = new byte[8];
		var decimalBytes = new byte[16];
		var guidBytes = new byte[16];

		BinaryIdEncoder.Encode((long)id, longBytes);
		BinaryIdEncoder.Encode((ulong)id, ulongBytes);
		BinaryIdEncoder.Encode(id, decimalBytes);
		BinaryIdEncoder.Encode(AlphanumericIdEncoderTests.Guid(id), guidBytes);

		BinaryIdEncoder.TryDecodeLong(longBytes, out var expectedLongResult);
		BinaryIdEncoder.TryDecodeUlong(ulongBytes, out var expectedUlongResult);
		BinaryIdEncoder.TryDecodeDecimal(decimalBytes, out var expectedDecimalResult);
		BinaryIdEncoder.TryDecodeGuid(guidBytes, out var expectedGuidResult);

		var longResult = BinaryIdEncoder.DecodeLongOrDefault(longBytes);
		var ulongResult = BinaryIdEncoder.DecodeUlongOrDefault(ulongBytes);
		var decimalResult = BinaryIdEncoder.DecodeDecimalOrDefault(decimalBytes);
		var guidResult = BinaryIdEncoder.DecodeGuidOrDefault(guidBytes);

		Assert.Equal(expectedLongResult, longResult.Value);
		Assert.Equal(expectedUlongResult, ulongResult.Value);
		Assert.Equal(expectedDecimalResult, decimalResult.Value);
		Assert.Equal(expectedGuidResult, guidResult.Value);

		Assert.Equal(BinaryIdEncoder.TryDecodeLong(stackalloc byte[1], out _), BinaryIdEncoder.DecodeLongOrDefault(stackalloc byte[1]) is not null);
		Assert.Equal(BinaryIdEncoder.TryDecodeUlong(stackalloc byte[1], out _), BinaryIdEncoder.DecodeUlongOrDefault(stackalloc byte[1]) is not null);
		Assert.Equal(BinaryIdEncoder.TryDecodeDecimal(stackalloc byte[1], out _), BinaryIdEncoder.DecodeDecimalOrDefault(stackalloc byte[1]) is not null);
		Assert.Equal(BinaryIdEncoder.TryDecodeGuid(stackalloc byte[1], out _), BinaryIdEncoder.DecodeGuidOrDefault(stackalloc byte[1]) is not null);
	}

	[Fact]
	public void TryDecode_WithTooLongInput_ShouldFail()
	{
		Assert.False(BinaryIdEncoder.TryDecodeLong(stackalloc byte[9], out _));
		Assert.False(BinaryIdEncoder.TryDecodeUlong(stackalloc byte[9], out _));
		Assert.False(BinaryIdEncoder.TryDecodeDecimal(stackalloc byte[17], out _));
		Assert.False(BinaryIdEncoder.TryDecodeGuid(stackalloc byte[17], out _));
	}

	[Fact]
	public void TryDecode_WithTooShortInput_ShouldThrow()
	{
		Assert.False(BinaryIdEncoder.TryDecodeLong(stackalloc byte[7], out _));
		Assert.False(BinaryIdEncoder.TryDecodeUlong(stackalloc byte[7], out _));
		Assert.False(BinaryIdEncoder.TryDecodeDecimal(stackalloc byte[15], out _));
		Assert.False(BinaryIdEncoder.TryDecodeGuid(stackalloc byte[15], out _));
	}
}
