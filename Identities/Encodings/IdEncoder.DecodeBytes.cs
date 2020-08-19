using System;
using System.Buffers.Binary;
using Architect.Identities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(long, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 11 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetLong(ReadOnlySpan<byte> bytes, out long id)
		{
			if (!TryGetUlong(bytes, out var ulongId) || ulongId > Int64.MaxValue)
			{
				id = default;
				return false;
			}

			id = (long)ulongId;
			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(ulong, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 11 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetUlong(ReadOnlySpan<byte> bytes, out ulong id)
		{
			// Alphanumeric encodings are exactly 11 characters long
			if (bytes.Length < 11)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[8];

			try
			{
				Base62.FromBase62Chars11(bytes, outputBytes);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			id = BinaryPrimitives.ReadUInt64BigEndian(outputBytes);
			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(decimal, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetDecimal(ReadOnlySpan<byte> bytes, out decimal id)
		{
			// Alphanumeric encodings are exactly 16 characters long
			if (bytes.Length < 16)
			{
				id = default;
				return false;
			}

			Span<byte> paddedInputBytes = stackalloc byte[22];
			paddedInputBytes[..^16].Fill((byte)'0'); // Fill with '0' characters
			bytes[..16].CopyTo(paddedInputBytes[^16..]);

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				Base62.FromBase62Chars22(paddedInputBytes, outputBytes);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			var signAndScale = BinaryPrimitives.ReadInt32BigEndian(outputBytes);
			var hi = BinaryPrimitives.ReadInt32BigEndian(outputBytes[4..]);
			var mid = BinaryPrimitives.ReadInt32BigEndian(outputBytes[8..]);
			var lo = BinaryPrimitives.ReadInt32BigEndian(outputBytes[12..]);

			id = new decimal(lo: lo, mid: mid, hi: hi, isNegative: false, scale: 0);

			if (signAndScale != 0 || id > DistributedIdGenerator.MaxValue)
			{
				id = default;
				return false;
			}

			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(Guid, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 22 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetGuid(ReadOnlySpan<byte> bytes, out Guid id)
		{
			// Alphanumeric encodings are exactly 22 characters long
			if (bytes.Length < 22)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				Base62.FromBase62Chars22(bytes, outputBytes);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			id = new Guid(outputBytes);
			return true;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(long, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 11 input characters.</param>
		public static long? GetLongOrDefault(ReadOnlySpan<byte> bytes)
		{ 
			return TryGetLong(bytes, out var id) ? id : (long?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(ulong, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 11 input characters.</param>
		public static ulong? GetUlongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryGetUlong(bytes, out var id) ? id : (ulong?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(decimal, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input characters.</param>
		public static decimal? GetDecimalOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryGetDecimal(bytes, out var id) ? id : (decimal?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given UTF-8 alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(Guid, Span{byte})"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 22 input characters.</param>
		public static Guid? GetGuidOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryGetGuid(bytes, out var id) ? id : (Guid?)null;
		}
	}
}
