using System;
using Architect.Identities.PublicIdentities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// A base class to help implement <see cref="IPublicIdentityConverter"/>.
	/// Provides components that are generally required.
	/// </summary>
	public abstract class BasePublicIdentityConverter
	{
		/// <summary>
		/// Defines an encoding function, such as from binary to ASCII bytes.
		/// </summary>
		protected delegate void EncodingFunction(ReadOnlySpan<byte> input, Span<byte> output);
		/// <summary>
		/// Defines a decoding function, such as from ASCII bytes to binary.
		/// </summary>
		protected delegate void DecodingFunction(ReadOnlySpan<byte> input, Span<byte> output);

		/// <summary>
		/// An encoding function that merely copies the bytes over as-is.
		/// </summary>
		protected static readonly EncodingFunction CopyOnlyEncoder = (input, output) => input.CopyTo(output);
		/// <summary>
		/// A decoding function that merely copies the bytes over as-is.
		/// </summary>
		protected static readonly DecodingFunction CopyOnlyDecoder = (input, output) => input.CopyTo(output);

		/// <summary>
		/// An encoding function that converts from binary to the long ASCII representation.
		/// </summary>
		protected static readonly EncodingFunction LongAsciiEncoder = LongAsciiEncoderImplementation;
		/// <summary>
		/// A decoding function that converts from the long ASCII representation back into binary.
		/// </summary>
		protected static readonly DecodingFunction LongAsciiDecoder = LongAsciiDecoderImplementation;

		/// <summary>
		/// An encoding function that converts from binary to the short ASCII representation.
		/// </summary>
		protected static readonly EncodingFunction ShortAsciiEncoder = ShortAsciiEncoderImplementation;
		/// <summary>
		/// A decoding function that converts from the short ASCII representation back into binary.
		/// </summary>
		protected static readonly DecodingFunction ShortAsciiDecoder = ShortAsciiDecoderImplementation;

		protected static void LongAsciiEncoderImplementation(ReadOnlySpan<byte> input, Span<byte> output)
		{
			System.Diagnostics.Debug.Assert(input.Length == 16);
			System.Diagnostics.Debug.Assert(output.Length == 32);
			Hexadecimal.ToHexChars(input, output);
		}

		protected static void LongAsciiDecoderImplementation(ReadOnlySpan<byte> input, Span<byte> output)
		{
			System.Diagnostics.Debug.Assert(output.Length == 16);
			Hexadecimal.FromHexChars(input, output);
		}

		protected static void ShortAsciiEncoderImplementation(ReadOnlySpan<byte> input, Span<byte> output)
		{
			System.Diagnostics.Debug.Assert(input.Length == 16);
			System.Diagnostics.Debug.Assert(output.Length == 22);
			Base62.ToBase62Chars(input, output);
		}

		protected static void ShortAsciiDecoderImplementation(ReadOnlySpan<byte> input, Span<byte> output)
		{
			System.Diagnostics.Debug.Assert(output.Length == 16);
			Base62.FromBase62Chars(input, output);
		}
	}
}
