using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// <para>
		/// Encodes the given ID into a <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Note that certain databases, such as SQL Server, do not get the performance benefits of incremental GUIDs (unless the bytes are meticulously rearranged to match its sorting logic).
		/// This makes <see cref="long"/>, <see cref="ulong"/>, and <see cref="decimal"/> more portable types.
		/// </para>
		/// </summary>
		public static Guid GetGuid(long id)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			return GetGuid((ulong)id);
		}

		/// <summary>
		/// <para>
		/// Encodes the given ID into a <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Note that certain databases, such as SQL Server, do not get the performance benefits of incremental GUIDs (unless the bytes are meticulously rearranged to match its sorting logic).
		/// This makes <see cref="long"/>, <see cref="ulong"/>, and <see cref="decimal"/> more portable types.
		/// </para>
		/// </summary>
		public static Guid GetGuid(ulong id)
		{
			Span<byte> bytes = stackalloc byte[16];

			// Note that the high 32 bits of the left ulong must never be used, since GUID sorting behavior is extremely hard to work with in the corresponding bits
			// This makes it infeasible to populate both halves with a copy of the value, which would looked nice and random to the human eye

			var ulongs = MemoryMarshal.Cast<byte, ulong>(bytes);

			ulongs[1] = BinaryPrimitives.ReverseEndianness(id); // The second half of a GUID is interpreted as big-endian
			ulongs[0] = 0UL;

			var result = new Guid(bytes);
			return result;
		}

		/// <summary>
		/// <para>
		/// Encodes the given ID into a <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Note that certain databases, such as SQL Server, do not get the performance benefits of incremental GUIDs (unless the bytes are meticulously rearranged to match its sorting logic).
		/// This makes <see cref="long"/>, <see cref="ulong"/>, and <see cref="decimal"/> more portable types.
		/// </para>
		/// </summary>
		public static Guid GetGuid(decimal id)
		{
			Span<byte> bytes = stackalloc byte[16];

			var (signAndScale, hi, mid, lo) = ExtractAndValidateIdComponents(id);
			var hiUnsigned = (uint)hi;

			// The first half of a GUID is interpreted as little-endian
			System.Diagnostics.Debug.Assert(signAndScale == 0); // Double check
			Unsafe.WriteUnaligned(ref bytes[0], 0); // Bytes 0-3 are the most significant (and endianness for 0 is irrelevant, so use unaligned for performance)
			BinaryPrimitives.WriteUInt16LittleEndian(bytes[4..], (ushort)(hiUnsigned >> 16)); // Bytes 4-5 are the next most significant
			BinaryPrimitives.WriteUInt16LittleEndian(bytes[6..], (ushort)hiUnsigned); // Bytes 6-7 are the next most significant

			// The second half of a GUID is interpreted as big-endian
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			var result = new Guid(bytes);
			return result;
		}

		/// <summary>
		/// <para>
		/// Extracts an ID that was encoded into the given <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		public static bool TryGetLong(Guid guid, out long id)
		{
			if (!TryGetUlong(guid, out var unsignedResult) || unsignedResult > Int64.MaxValue)
			{
				id = default;
				return false;
			}

			id = (long)unsignedResult;
			return true;
		}

		/// <summary>
		/// <para>
		/// Extracts an ID that was encoded into the given <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		public static bool TryGetUlong(Guid guid, out ulong id)
		{
			var guids = MemoryMarshal.CreateSpan(ref guid, length: 1);
			var ulongs = MemoryMarshal.Cast<Guid, ulong>(guids);

			if (ulongs[0] != 0UL)
			{
				id = default;
				return false;
			}

			id = BinaryPrimitives.ReverseEndianness(ulongs[1]); // The second half of a GUID is interpreted as big-endian
			return true;
		}

		/// <summary>
		/// <para>
		/// Extracts an ID that was encoded into the given <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		public static bool TryGetDecimal(Guid guid, out decimal id)
		{
			var guids = MemoryMarshal.CreateSpan(ref guid, length: 1);
			var ushorts = MemoryMarshal.Cast<Guid, ushort>(guids);
			var bytes = MemoryMarshal.AsBytes(guids);

			// The first half of a GUID is interpreted as little-endian
			var signAndScale = Unsafe.ReadUnaligned<int>(ref bytes[0]); // Bytes 0-3 are the most significant
			var hi1 = (uint)ushorts[2]; // Bytes 4-5 are the next most significant
			var hi2 = (uint)ushorts[3]; // Bytes 6-7 are the next most significant
			var hi = (hi1 << 16) | hi2;

			// The second half of a GUID is interpreted as big-endian
			var mid = BinaryPrimitives.ReadInt32BigEndian(bytes[8..]);
			var lo = BinaryPrimitives.ReadInt32BigEndian(bytes[12..]);

			id = new decimal(lo: lo, mid: mid, hi: (int)hi, isNegative: false, scale: 0);

			if (signAndScale != 0 || id > DistributedIdGenerator.MaxValue)
			{
				id = default;
				return false;
			}

			return true;
		}

		public static long? GetLongOrDefault(Guid guid)
		{
			return TryGetLong(guid, out var result) ? result : (long?)null;
		}

		public static ulong? GetUlongOrDefault(Guid guid)
		{
			return TryGetUlong(guid, out var result) ? result : (ulong?)null;
		}

		public static decimal? GetDecimalOrDefault(Guid guid)
		{
			return TryGetDecimal(guid, out var result) ? result : (decimal?)null;
		}
	}
}
