using System;
using Architect.Identities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class HexadecimalTests
	{
		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		public void ToHexChars_Regularly_ShouldReturnExpectedOutput(string text, string hexString)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[hexString.Length];
			Hexadecimal.ToHexChars(bytes, outputChars);
			var outputString = System.Text.Encoding.UTF8.GetString(outputChars);

			Assert.Equal(hexString, outputString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		public void FromHexChars_Regularly_ShouldReturnExpectedOutput(string text, string hexString)
		{
			var chars = System.Text.Encoding.UTF8.GetBytes(hexString);
			Span<byte> outputBytes = stackalloc byte[text.Length];
			Hexadecimal.FromHexChars(chars, outputBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(outputBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "3132333435363738" + "3132333435363738")]
		[InlineData("12345679" + "12345679", "3132333435363739" + "3132333435363739")]
		[InlineData("12345678" + "12345679", "3132333435363738" + "3132333435363739")]
		public void FromHexChars_AfterToHexChars_ShouldReturnOriginalInput(string text, string hexString)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[hexString.Length];
			Hexadecimal.ToHexChars(bytes, outputChars);
			Span<byte> decodedBytes = stackalloc byte[bytes.Length];
			Hexadecimal.FromHexChars(outputChars, decodedBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(decodedBytes);

			Assert.Equal(text, originalString);
		}
	}
}
