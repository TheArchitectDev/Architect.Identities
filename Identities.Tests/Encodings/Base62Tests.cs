using Architect.Identities.Encodings;
using Xunit;

namespace Architect.Identities.Tests.Encodings
{
	public sealed class Base62Tests
	{
		[Theory]
		[InlineData("12345678", "4DruweP3xQ8")]
		[InlineData("12345679", "4DruweP3xQ9")]
		public void ToBase62Chars8_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars8(bytes, outputChars);
			var outputString = System.Text.Encoding.UTF8.GetString(outputChars);

			Assert.Equal(base62String, outputString);
		}

		[Theory]
		[InlineData("12345678", "4DruweP3xQ8")]
		[InlineData("12345679", "4DruweP3xQ9")]
		public void ToBase62Chars8_WithOverlappingInputAndOutputSpans_ShouldReturnExpectedOutput(string text, string base62String)
		{
			Span<byte> bytes = stackalloc byte[11];
			System.Text.Encoding.UTF8.GetBytes(text, bytes);
			Base62.ToBase62Chars8(bytes, bytes);
			var outputString = System.Text.Encoding.UTF8.GetString(bytes);

			Assert.Equal(base62String, outputString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ8" + "4DruweP3xQ9")]
		public void ToBase62Chars16_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars16(bytes, outputChars);
			var outputString = System.Text.Encoding.UTF8.GetString(outputChars);

			Assert.Equal(base62String, outputString);
		}

		[Theory]
		[InlineData("12345678", "4DruweP3xQ8")]
		[InlineData("12345679", "4DruweP3xQ9")]
		public void FromBase62Chars11_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var chars = System.Text.Encoding.UTF8.GetBytes(base62String);
			Span<byte> outputBytes = stackalloc byte[text.Length];
			Base62.FromBase62Chars11(chars, outputBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(outputBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678", "4DruweP3xQ8")]
		[InlineData("12345679", "4DruweP3xQ9")]
		public void FromBase62Chars11_WithOverlappingInputAndOutputSpans_ShouldReturnExpectedOutput(string text, string base62String)
		{
			Span<byte> bytes = stackalloc byte[11];
			System.Text.Encoding.UTF8.GetBytes(base62String, bytes);
			Base62.FromBase62Chars11(bytes, bytes);
			var originalString = System.Text.Encoding.UTF8.GetString(bytes[..8]);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678", "4DruweP3xQ8")]
		[InlineData("12345679", "4DruweP3xQ9")]
		public void FromBase62Chars11_AfterToBase62Chars_ShouldReturnOriginalInput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars8(bytes, outputChars);
			Span<byte> decodedBytes = stackalloc byte[bytes.Length];
			Base62.FromBase62Chars11(outputChars, decodedBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(decodedBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ8" + "4DruweP3xQ9")]
		public void FromBase62Chars22_Regularly_ShouldReturnExpectedOutput(string text, string base62String)
		{
			var chars = System.Text.Encoding.UTF8.GetBytes(base62String);
			Span<byte> outputBytes = stackalloc byte[text.Length];
			Base62.FromBase62Chars22(chars, outputBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(outputBytes);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ8" + "4DruweP3xQ9")]
		public void FromBase62Chars22_WithOverlappingInputAndOutputSpans_ShouldReturnExpectedOutput(string text, string base62String)
		{
			Span<byte> bytes = stackalloc byte[22];
			System.Text.Encoding.UTF8.GetBytes(base62String, bytes);
			Base62.FromBase62Chars22(bytes, bytes);
			var originalString = System.Text.Encoding.UTF8.GetString(bytes[..16]);

			Assert.Equal(text, originalString);
		}

		[Theory]
		[InlineData("12345678" + "12345678", "4DruweP3xQ8" + "4DruweP3xQ8")]
		[InlineData("12345679" + "12345679", "4DruweP3xQ9" + "4DruweP3xQ9")]
		[InlineData("12345678" + "12345679", "4DruweP3xQ8" + "4DruweP3xQ9")]
		public void FromBase62Chars22_AfterToBase62Chars_ShouldReturnOriginalInput(string text, string base62String)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
			Span<byte> outputChars = stackalloc byte[base62String.Length];
			Base62.ToBase62Chars16(bytes, outputChars);
			Span<byte> decodedBytes = stackalloc byte[bytes.Length];
			Base62.FromBase62Chars22(outputChars, decodedBytes);
			var originalString = System.Text.Encoding.UTF8.GetString(decodedBytes);

			Assert.Equal(text, originalString);
		}
	}
}
