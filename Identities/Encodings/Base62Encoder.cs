using System;
using System.Linq;
using System.Text;

namespace Architect.Identities.Encodings
{
	/// <summary>
	/// A limited base62 encoder, aimed at simplicity, efficiency, and useful endianness.
	/// </summary>
	internal static class Base62Encoder
	{
		private static Base62Alphabet DefaultAlphabet { get; } = new Base62Alphabet(Encoding.ASCII.GetBytes("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"));

		/// <summary>
		/// <para>
		/// Converts the given 8 bytes to 11 base62 chars.
		/// </para>
		/// </summary>
		public static void ToBase62Chars8(ReadOnlySpan<byte> bytes, Span<byte> chars)
		{
			System.Diagnostics.Debug.Assert(bytes.Length >= 8);
			System.Diagnostics.Debug.Assert(chars.Length >= 11);

			var forwardAlphabet = DefaultAlphabet.ForwardAlphabet;

			EncodeBlock(forwardAlphabet, bytes, chars);
		}

		/// <summary>
		/// <para>
		/// Converts the given 16 bytes to 22 base62 chars.
		/// </para>
		/// <para>
		/// The input and output spans must not overlap. This is asserted in debug mode.
		/// </para>
		/// </summary>
		public static void ToBase62Chars16(ReadOnlySpan<byte> bytes, Span<byte> chars)
		{
			System.Diagnostics.Debug.Assert(bytes.Length >= 16);
			System.Diagnostics.Debug.Assert(chars.Length >= 22);
			System.Diagnostics.Debug.Assert(!bytes.Overlaps(chars), "The input and output spans must not overlap, as the first block's output will overwrite the second block of input.");

			var forwardAlphabet = DefaultAlphabet.ForwardAlphabet;

			EncodeBlock(forwardAlphabet, bytes, chars);
			bytes = bytes[8..];
			chars = chars[11..];
			EncodeBlock(forwardAlphabet, bytes, chars);
		}

		private static void EncodeBlock(ReadOnlySpan<byte> alphabet, ReadOnlySpan<byte> bytes, Span<byte> chars)
		{
			System.Diagnostics.Debug.Assert(alphabet.Length == 62);
			System.Diagnostics.Debug.Assert(bytes.Length >= 8);
			System.Diagnostics.Debug.Assert(chars.Length >= 11);

			var ulongValue = 0UL;
			for (var i = 0; i < 8; i++) ulongValue = (ulongValue << 8) | bytes[i];

			// Can encode 8 bytes as 11 chars
			for (var i = 11 - 1; i >= 0; i--)
			{
				var quotient = ulongValue / 62UL;
				var remainder = ulongValue - 62UL * quotient;
				ulongValue = quotient;
				chars[i] = alphabet[(int)remainder];
			}
		}

		/// <summary>
		/// <para>
		/// Converts the given 11 base62 chars to 8 bytes.
		/// </para>
		/// <para>
		/// Throws an <see cref="ArgumentException"/> on invalid input.
		/// </para>
		/// </summary>
		public static void FromBase62Chars11(ReadOnlySpan<byte> chars, Span<byte> bytes)
		{
			System.Diagnostics.Debug.Assert(chars.Length >= 11);
			System.Diagnostics.Debug.Assert(bytes.Length >= 8);

			var reverseAlphabet = DefaultAlphabet.ReverseAlphabet;

			DecodeBlock(reverseAlphabet, chars, bytes);
		}

		/// <summary>
		/// <para>
		/// Converts the given 22 base62 chars to 16 bytes.
		/// </para>
		/// <para>
		/// Throws an <see cref="ArgumentException"/> on invalid input.
		/// </para>
		/// </summary>
		public static void FromBase62Chars22(ReadOnlySpan<byte> chars, Span<byte> bytes)
		{
			System.Diagnostics.Debug.Assert(chars.Length >= 22);
			System.Diagnostics.Debug.Assert(bytes.Length >= 16);

			var reverseAlphabet = DefaultAlphabet.ReverseAlphabet;

			DecodeBlock(reverseAlphabet, chars, bytes);
			chars = chars[11..];
			bytes = bytes[8..];
			DecodeBlock(reverseAlphabet, chars, bytes);
		}

		private static void DecodeBlock(ReadOnlySpan<sbyte> reverseAlphabet, ReadOnlySpan<byte> chars11, Span<byte> bytes)
		{
			System.Diagnostics.Debug.Assert(reverseAlphabet.Length == 256);
			System.Diagnostics.Debug.Assert(chars11.Length >= 11);
			System.Diagnostics.Debug.Assert(bytes.Length >= 8);

			// Can decode 11 chars back into 8 bytes
			var ulongValue = 0UL;
			for (var i = 0; i < 11; i++)
			{
				var chr = chars11[i];
				var value = (ulong)reverseAlphabet[chr]; // -1 (invalid character) becomes UInt64.MaxValue
				if (value >= 62) throw new ArgumentException("The input encoding is invalid.");

				ulongValue = ulongValue * 62 + value;
			}

			for (var i = 8 - 1; i >= 0; i--)
			{
				bytes[i] = (byte)ulongValue;
				ulongValue >>= 8;
			}
		}
	}

	internal sealed class Base62Alphabet
	{
		public override bool Equals(object? obj) => obj is Base62Alphabet other && other.ForwardAlphabet.SequenceEqual(this.ForwardAlphabet);
		public override int GetHashCode() => this.ForwardAlphabet[0].GetHashCode() ^ this.ForwardAlphabet[61].GetHashCode();

		public ReadOnlySpan<byte> ForwardAlphabet => this._alphabet;
		private readonly byte[] _alphabet;

		public ReadOnlySpan<sbyte> ReverseAlphabet => this._reverseAlphabet.AsSpan();
		private readonly sbyte[] _reverseAlphabet;

		/// <summary>
		/// Constructs a Base62 alphabet, including its reverse representation.
		/// The result should be cached for reuse.
		/// </summary>
		public Base62Alphabet(ReadOnlySpan<byte> alphabet)
		{
			if (alphabet.Length != 62) throw new ArgumentException("Expected an alphabet of length 62.");

			this._alphabet = alphabet.ToArray();

			if (this._alphabet.Any(chr => chr == 0))
				throw new ArgumentException("The NULL character is not allowed.");
			if (this._alphabet.Any(chr => chr > 127))
				throw new ArgumentException("Non-ASCII characters are not allowed.");
			if (this._alphabet.Distinct().Count() != this._alphabet.Length)
				throw new ArgumentException("All characters in the alphabet must be distinct.");

			this._reverseAlphabet = GetReverseAlphabet(this.ForwardAlphabet);

			System.Diagnostics.Debug.Assert(this.ReverseAlphabet.Length == 256);
		}

		/// <summary>
		/// <para>
		/// Creates a reverse alphabet for the given alphabet.
		/// </para>
		/// <para>
		/// When indexing into the slot matching a character's numeric value, the result is the value between 0 and 61 (inclusive) represented by the character.
		/// (Slots not related to any of the alphabet's characters contain -1.)
		/// </para>
		/// <para>
		/// The result should be cached for reuse.
		/// </para>
		/// </summary>
		internal static sbyte[] GetReverseAlphabet(ReadOnlySpan<byte> alphabet)
		{
			if (alphabet.Length != 62) throw new ArgumentException("Expected an alphabet of length 62.");

			var result = new sbyte[256];
			Array.Fill(result, (sbyte)-1);
			for (sbyte i = 0; i < alphabet.Length; i++) result[alphabet[i]] = i;
			return result;
		}
	}
}

