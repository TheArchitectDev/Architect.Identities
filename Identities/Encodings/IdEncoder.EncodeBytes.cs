using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Architect.Identities.Encodings;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			GetAlphanumeric((ulong)id, bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 11) throw new IndexOutOfRangeException("At least 11 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryPrimitives.WriteUInt64BigEndian(bytes, id);

			Base62.ToBase62Chars8(bytes, bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="CompanyUniqueId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			if (id < 0m) throw new ArgumentOutOfRangeException();

			// Extract the components
			var decimals = MemoryMarshal.CreateReadOnlySpan(ref id, length: 1);
			var components = MemoryMarshal.Cast<decimal, int>(decimals);
			var signAndScale = DecimalStructure.GetSignAndScale(components);
			var hi = DecimalStructure.GetHi(components);
			var lo = DecimalStructure.GetLo(components);
			var mid = DecimalStructure.GetMid(components);

			// Validate format and range
			if (id > CompanyUniqueIdGenerator.MaxValue || signAndScale != 0m)
				throw new ArgumentException($"The ID must be positive, have no decimal places, and consist of no more than 28 digits.", nameof(id));

			// Abuse the caller's output span as input space
			BinaryPrimitives.WriteInt32BigEndian(bytes, 0);
			BinaryPrimitives.WriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			Span<byte> charBytes = stackalloc byte[22];

			Base62.ToBase62Chars16(bytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..6].TrimStart((byte)'0').Length == 0, "The first 6 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^16..].CopyTo(bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs a 22-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 22 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(Guid id, Span<byte> bytes)
		{
			if (bytes.Length < 22) throw new IndexOutOfRangeException("At least 22 output bytes are required.");

			Span<byte> inputBytes = stackalloc byte[16];
			if (!id.TryWriteBytes(inputBytes)) throw new InvalidOperationException($"The ID's bytes could not be extracted: {id}.");

			Base62.ToBase62Chars16(inputBytes, bytes);
		}
	}
}
