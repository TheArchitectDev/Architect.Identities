using Architect.Identities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class HexadecimalEncoderTests
	{
		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		[InlineData("ZZZZZZZZ" + "ZZZZZZZZ", "5A5A5A5A5A5A5A5A" + "5A5A5A5A5A5A5A5A")]
		[InlineData("zzzzzzzz" + "zzzzzzzz", "7A7A7A7A7A7A7A7A" + "7A7A7A7A7A7A7A7A")]
		public void ToHexChars_Regularly_ShouldReturnExpectedOutput(string text, string hexString)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[hexString.Length];
			HexadecimalEncoder.ToHexChars(bytes, outputChars, inputByteCount: 16);
			var outputString = System.Text.Encoding.UTF8.GetString(outputChars);

			Assert.Equal(hexString, outputString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		[InlineData("ZZZZZZZZ" + "ZZZZZZZZ", "5A5A5A5A5A5A5A5A" + "5A5A5A5A5A5A5A5A")]
		[InlineData("zzzzzzzz" + "zzzzzzzz", "7A7A7A7A7A7A7A7A" + "7A7A7A7A7A7A7A7A")]
		[InlineData("zzzzzzzz" + "zzzzzzzz", "7a7a7a7a7a7a7a7a" + "7a7a7a7a7a7a7a7a")]
		public void FromHexChars_Regularly_ShouldReturnExpectedOutput(string text, string hexString)
		{
			var chars = System.Text.Encoding.UTF8.GetBytes(hexString);
			Span<byte> outputBytes = stackalloc byte[text.Length];
			HexadecimalEncoder.FromHexChars(chars, outputBytes, inputByteCount: 32);
			var originalString = System.Text.Encoding.UTF8.GetString(outputBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		[InlineData("ZZZZZZZZ" + "ZZZZZZZZ", "5A5A5A5A5A5A5A5A" + "5A5A5A5A5A5A5A5A")]
		[InlineData("zzzzzzzz" + "zzzzzzzz", "7A7A7A7A7A7A7A7A" + "7A7A7A7A7A7A7A7A")]
		public void FromHexChars_AfterToHexChars_ShouldReturnOriginalInput(string text, string hexString)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[hexString.Length];
			HexadecimalEncoder.ToHexChars(bytes, outputChars, inputByteCount: 16);
			Span<byte> decodedBytes = stackalloc byte[bytes.Length];
			HexadecimalEncoder.FromHexChars(outputChars, decodedBytes, inputByteCount: 32);
			var originalString = System.Text.Encoding.UTF8.GetString(decodedBytes);

			Assert.Equal(text, originalString);
		}
	}
}
