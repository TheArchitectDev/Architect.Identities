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
}
