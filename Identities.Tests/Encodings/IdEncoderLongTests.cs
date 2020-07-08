using System;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class IdEncoderLongTests
	{
		private const string SampleAlphanumericString = "1TCKi1nFuNh";

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void GetAlphanumeric_Regularly_ShouldReturnSameResultAsForUlong(long id)
		{
			var expectedResult = IdEncoder.GetAlphanumeric((ulong)id);

			var result = IdEncoder.GetAlphanumeric(id);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0L)]
		[InlineData(1L)]
		[InlineData(999999999999999999L)] // 18 digits
		[InlineData(Int16.MaxValue)]
		[InlineData(Int32.MaxValue)]
		[InlineData(Int64.MaxValue)]
		public void GetAlphanumeric_WithByteOutput_ShouldReturnSameResultAsWithStringOutput(long id)
		{
			Span<byte> expectedResult = stackalloc byte[11];
			var expectedResultString = IdEncoder.GetAlphanumeric(id);
			for (var i = 0; i < expectedResult.Length; i++) expectedResult[i] = (byte)expectedResultString[i];

			Span<byte> result = stackalloc byte[11];
			IdEncoder.GetAlphanumeric(id, result);

			Assert.True(result.SequenceEqual(expectedResult));
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
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
		public void TryGetLong_Regularly_ShouldReturnSameResultAsTryGetUlong(string input)
		{
			var expectedSuccess = IdEncoder.TryGetUlong(input, out var expectedResult);

			var success = IdEncoder.TryGetLong(input, out var result);
			Assert.Equal(expectedSuccess, success);
			Assert.True(result >= 0L);
			Assert.Equal(expectedResult, (ulong)result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
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
		public void GetLongOrDefault_Regularly_ShouldReturnSameResultAsTryGetLong(string input)
		{
			var expectedResult = IdEncoder.TryGetLong(input, out var expectedId)
				? expectedId
				: (long?)null;

			var result = IdEncoder.GetLongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
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
		public void TryGetLong_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedSuccess = IdEncoder.TryGetLong(inputString, out var expectedResult);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var success = IdEncoder.TryGetLong(input, out var result);

			Assert.Equal(expectedSuccess, success);
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(SampleAlphanumericString)]
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
		public void GetLongOrDefault_WithByteInput_ShouldReturnSameResultAsWithStringInput(string inputString)
		{
			var expectedResult = IdEncoder.GetLongOrDefault(inputString);

			Span<byte> input = stackalloc byte[inputString.Length];
			for (var i = 0; i < input.Length; i++) input[i] = (byte)inputString[i];

			var result = IdEncoder.GetLongOrDefault(input);

			Assert.Equal(expectedResult, result);
		}
	}
}
