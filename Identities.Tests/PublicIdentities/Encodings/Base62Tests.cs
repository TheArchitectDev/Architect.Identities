using System;
using Architect.Identities.PublicIdentities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.PublicIdentities.Encodings
{
	public sealed class Base62Tests
	{
		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ84" + "DruweP3xQ9")]
		public void ToBase62Chars_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars(bytes, outputChars);
			var outputString = System.Text.Encoding.UTF8.GetString(outputChars);

			Assert.Equal(base62String, outputString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ84" + "DruweP3xQ9")]
		public void FromBase62Chars_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var chars = System.Text.Encoding.UTF8.GetBytes(base62String);
			Span<byte> outputBytes = stackalloc byte[text.Length];
			Base62.FromBase62Chars(chars, outputBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(outputBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ84" + "DruweP3xQ9")]
		public void FromBase62Chars_AfterToBase62Chars_ShouldReturnOriginalInput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars(bytes, outputChars);
			Span<byte> decodedBytes = stackalloc byte[bytes.Length];
			Base62.FromBase62Chars(outputChars, decodedBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(decodedBytes);

			Assert.Equal(text, originalString);
		}
	}
}
