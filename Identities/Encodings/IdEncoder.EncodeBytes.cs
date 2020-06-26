using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Architect.Identities.Helpers;
using Architect.Identities.PublicIdentities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	// #TODO: Summaries (all partials)
	// #TODO: Test entire class (all partials)
	public static partial class IdEncoder
	{
		public static void GetAlphanumeric(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			GetAlphanumeric((ulong)id, bytes);
		}

		public static void GetAlphanumeric(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 11) throw new IndexOutOfRangeException("At least 8 output bytes are required.");

			Span<byte> charBytes = stackalloc byte[22];
			Span<byte> idBytes = stackalloc byte[16];
			BinaryPrimitives.WriteUInt64BigEndian(idBytes, 0UL);
			BinaryPrimitives.WriteUInt64BigEndian(idBytes[^8..], id);

			Base62.ToBase62Chars(idBytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..11].TrimStart((byte)'0').Length == 0, "The first 11 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^11..].CopyTo(bytes);
		}

		public static void GetAlphanumeric(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");
			bytes = bytes[..16];

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
				throw new ArgumentException($"Unexpected input value. Pass only values created by {nameof(CompanyUniqueId)}.{nameof(CompanyUniqueId.CreateId)}.", nameof(id));

			// Abuse the caller's output span as input space
			BinaryPrimitives.WriteInt32BigEndian(bytes, 0);
			BinaryPrimitives.WriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			Span<byte> charBytes = stackalloc byte[22];

			Base62.ToBase62Chars(bytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..6].TrimStart((byte)'0').Length == 0, "The first 6 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^16..].CopyTo(bytes);
		}
	}
}
