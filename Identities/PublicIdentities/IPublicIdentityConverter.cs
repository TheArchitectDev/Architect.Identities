using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Provides deterministic conversions between local and public IDs.
	/// This allows local IDs to be kept hidden, with public IDs directly based on them, without the bookkeeping that comes with unrelated public IDs.
	/// </summary>
	public interface IPublicIdentityConverter : IDisposable
	{
		#region Encode to bytes

		/// <summary>
		/// <para>
		/// Encodes the given ID as 32 uppercase hexadecimal characters.
		/// The output format looks like a UUID v4 to the naked eye.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicString(ulong id, Span<byte> asciiOutput);
		/// <summary>
		/// <para>
		/// Encodes the given ID as 32 uppercase hexadecimal characters.
		/// The output format looks like a UUID v4 to the naked eye.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicString(long id, Span<byte> asciiOutput);

		/// <summary>
		/// <para>
		/// Encodes the given ID as 22 alphanumeric characters (case-sensitive).
		/// The output is shorter than that of the regular (non-short) method, but the casing matters, the format is less common, and it is less efficient to produce.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicShortString(ulong id, Span<byte> asciiOutput);
		/// <summary>
		/// <para>
		/// Encodes the given ID as 22 alphanumeric characters (case-sensitive).
		/// The output is shorter than that of the regular (non-short) method, but the casing matters, the format is less common, and it is less efficient to produce.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicShortString(long id, Span<byte> asciiOutput);

		/// <summary>
		/// <para>
		/// Encodes the given ID as 16 bytes.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicBytes(ulong id, Span<byte> outputBytes);
		/// <summary>
		/// <para>
		/// Encodes the given ID as 16 bytes.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload is allocation-free.
		/// </para>
		/// </summary>
		public void GetPublicBytes(long id, Span<byte> outputBytes);

		#endregion

		#region Encode to string

		/// <summary>
		/// <para>
		/// Encodes the given ID as 32 uppercase hexadecimal characters.
		/// The output format looks like a UUID v4 to the naked eye.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload allocates (only) the resulting string.
		/// </para>
		/// </summary>
		public string GetPublicString(ulong id);
		/// <summary>
		/// <para>
		/// Encodes the given ID as 32 uppercase hexadecimal characters.
		/// The output format looks like a UUID v4 to the naked eye.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload allocates (only) the resulting string.
		/// </para>
		/// </summary>
		public string GetPublicString(long id);

		/// <summary>
		/// <para>
		/// Encodes the given ID as 22 alphanumeric characters (case-sensitive).
		/// The output is shorter than that of the regular (non-short) method, but the casing matters, the format is less common, and it is less efficient to produce.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload allocates (only) the resulting string.
		/// </para>
		/// </summary>
		public string GetPublicShortString(ulong id);
		/// <summary>
		/// <para>
		/// Encodes the given ID as 22 alphanumeric characters (case-sensitive).
		/// The output is shorter than that of the regular (non-short) method, but the casing matters, the format is less common, and it is less efficient to produce.
		/// </para>
		/// <para>
		/// The output is deterministic and reversible given the key, but otherwise indistinguishable from random noise (i.e. crypto-random).
		/// </para>
		/// <para>
		/// This overload allocates (only) the resulting string.
		/// </para>
		/// </summary>
		public string GetPublicShortString(long id);

		#endregion

		#region Decode from bytes

		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns the source value on success, or 0 on failure. It should be used only where source value 0 is a nonexistent value.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of 32 ASCII characters (regular representation) or 22 ASCII characters (short representation) or 16 bytes.</param>
		public ulong GetUlongOrDefault(ReadOnlySpan<byte> bytes);
		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns the source value on success, or 0 on failure. It should be used only where source value 0 is a nonexistent value.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of 32 ASCII characters (regular representation) or 22 ASCII characters (short representation) or 16 bytes.</param>
		public long GetLongOrDefault(ReadOnlySpan<byte> bytes);

		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns whether the operation succeeded, and outputs the source value on success.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of 32 ASCII characters (regular representation) or 22 ASCII characters (short representation) or 16 bytes.</param>
		/// <param name="value">The interpreted value, or 0.</param>
		public bool TryGetUlong(ReadOnlySpan<byte> bytes, out ulong value);
		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns whether the operation succeeded, and outputs the source value on success.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of 32 ASCII characters (regular representation) or 22 ASCII characters (short representation) or 16 bytes.</param>
		/// <param name="value">The interpreted value, or 0.</param>
		public bool TryGetLong(ReadOnlySpan<byte> bytes, out long value);

		#endregion

		#region Decode from chars

		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns the source value on success, or 0 on failure. It should be used only where source value 0 is a nonexistent value.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of 32 characters (regular representation) or 22 characters (short representation).</param>
		public ulong GetUlongOrDefault(ReadOnlySpan<char> chars);
		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns the source value on success, or 0 on failure. It should be used only where source value 0 is a nonexistent value.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of 32 characters (regular representation) or 22 characters (short representation).</param>
		public long GetLongOrDefault(ReadOnlySpan<char> chars);

		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns whether the operation succeeded, and outputs the source value on success.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of 32 characters (regular representation) or 22 characters (short representation).</param>
		/// <param name="value">The interpreted value, or 0.</param>
		public bool TryGetUlong(ReadOnlySpan<char> chars, out ulong value);
		/// <summary>
		/// <para>
		/// Decodes the given public identifier back to its source value, if it is valid.
		/// </para>
		/// <para>
		/// The public identifier contains a checksum that is used to securely identify its validity.
		/// Without the key, one random valid input value can be guessed for every 2^64 values attempted.
		/// The resulting output value is then random, i.e. with very high probability it is not an existing ID.
		/// </para>
		/// <para>
		/// This overload returns whether the operation succeeded, and outputs the source value on success.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of 32 characters (regular representation) or 22 characters (short representation).</param>
		/// <param name="value">The interpreted value, or 0.</param>
		public bool TryGetLong(ReadOnlySpan<char> chars, out long value);

		#endregion
	}
}
