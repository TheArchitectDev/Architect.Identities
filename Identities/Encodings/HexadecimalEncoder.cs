using System;
using System.Linq;
using System.Text;

namespace Architect.Identities.Encodings
{
	/// <summary>
	/// A simple hexadecimal encoder, aimed at simplicity and efficiency.
	/// </summary>
	internal static class HexadecimalEncoder
	{
		private static readonly HexAlphabet DefaultAlphabet = new HexAlphabet(Encoding.ASCII.GetBytes("0123456789ABCDEF"));

		/// <summary>
		/// <para>
		/// Converts the given bytes to hexadecimal chars.
		/// </para>
		/// </summary>
		public static void ToHexChars(ReadOnlySpan<byte> bytes, Span<byte> chars, int inputByteCount)
		{
			System.Diagnostics.Debug.Assert(bytes.Length >= inputByteCount);
			System.Diagnostics.Debug.Assert(chars.Length >= 2 * inputByteCount);

			var alphabet = DefaultAlphabet.ForwardAlphabet;

			// E.g. if inputByteCount = 16, write the first two characters at position (16 * 2) - 1 = 31 and the one before it
			for (var i = (inputByteCount << 1) - 1; i >= 0; i -= 2)
			{
				var byteValue = bytes[i >> 1];

				chars[i - 1] = alphabet[byteValue >> 4];
				chars[i] = alphabet[byteValue & 15];
			}
		}

		/// <summary>
		/// <para>
		/// Converts the given hexadecimal chars to bytes.
		/// </para>
		/// <para>
		/// Throws an <see cref="ArgumentException"/> on invalid input.
		/// </para>
		/// </summary>
		public static void FromHexChars(ReadOnlySpan<byte> chars, Span<byte> bytes, int inputByteCount)
		{
			System.Diagnostics.Debug.Assert(inputByteCount % 1 == 0, "An even number of input bytes is expected.");
			System.Diagnostics.Debug.Assert(chars.Length >= inputByteCount);
			System.Diagnostics.Debug.Assert(bytes.Length >= inputByteCount / 2);

			var alphabet = DefaultAlphabet.ReverseAlphabet;

			for (var i = 0; i < inputByteCount; i += 2)
			{
				int leftValue = alphabet[chars[i]];
				int rightValue = alphabet[chars[i + 1]];

				if (leftValue < 0 || rightValue < 0) throw new ArgumentException("The input encoding is invalid.");

				var value = leftValue << 4 | rightValue;

				System.Diagnostics.Debug.Assert(value <= Byte.MaxValue);

				bytes[i >> 1] = (byte)value;
			}
		}
	}

	internal sealed class HexAlphabet
	{
		public override bool Equals(object? obj) => obj is HexAlphabet other && other.ForwardAlphabet.SequenceEqual(this.ForwardAlphabet);
		public override int GetHashCode() => this.ForwardAlphabet[0].GetHashCode() ^ this.ForwardAlphabet[15].GetHashCode();

		public ReadOnlySpan<byte> ForwardAlphabet => this._alphabet;
		private readonly byte[] _alphabet;

		public ReadOnlySpan<sbyte> ReverseAlphabet => this._reverseAlphabet.AsSpan();
		private readonly sbyte[] _reverseAlphabet;

		/// <summary>
		/// Constructs a Hexadecimal alphabet, including its reverse representation.
		/// The result should be cached for reuse.
		/// </summary>
		public HexAlphabet(ReadOnlySpan<byte> alphabet)
		{
			if (alphabet.Length != 16) throw new ArgumentException("Expected an alphabet of length 16.");

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
		/// When indexing into the slot matching a character's numeric value, the result is the value between 0 and 15 (inclusive) represented by the character.
		/// (Slots not related to any of the alphabet's characters contain -1.)
		/// </para>
		/// <para>
		/// The result should be cached for reuse.
		/// </para>
		/// </summary>
		/// <param name="alphabet">An uppercase alphabet. For each uppercase letter, its lowercase equivalent will be mapped to the same binary value.</param>
		internal static sbyte[] GetReverseAlphabet(ReadOnlySpan<byte> alphabet)
		{
			if (alphabet.Length != 16) throw new ArgumentException("Expected an alphabet of length 16.");

			var result = new sbyte[256];

			Array.Fill(result, (sbyte)-1);

			for (sbyte i = 0; i < alphabet.Length; i++)
			{
				// Map the character to its represented binary value
				var charValue = alphabet[i];

				// If the character is an uppercase letter, map its lowercase equivalent to the same represented binary value
				result[charValue] = i;
				if (charValue >= 'A' && charValue <= 'Z')
					result[charValue + 32] = i;
			}

			return result;
		}
	}
}
