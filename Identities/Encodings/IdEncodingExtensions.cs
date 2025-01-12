using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	public static class IdEncodingExtensions
	{
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric representation of the given <see cref="Int64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string ToAlphanumeric(this long id) => AlphanumericIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric representation of the given <see cref="UInt64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		public static string ToAlphanumeric(this ulong id) => AlphanumericIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric representation of the given <see cref="Decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public static string ToAlphanumeric(this decimal id) => AlphanumericIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns a 22-character alphanumeric representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string ToAlphanumeric(this Guid id) => AlphanumericIdEncoder.Encode(id);

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="Int64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this long id, Span<byte> bytes) => AlphanumericIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="UInt64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this ulong id, Span<byte> bytes) => AlphanumericIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric representation of the given <see cref="Decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this decimal id, Span<byte> bytes) => AlphanumericIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 22-character alphanumeric representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 22 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this Guid id, Span<byte> bytes) => AlphanumericIdEncoder.Encode(id, bytes);

		/// <summary>
		/// <para>
		/// Returns a 16-character hexadecimal representation of the given <see cref="Int64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string ToHexadecimal(this long id) => HexadecimalIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns a 16-character hexadecimal representation of the given <see cref="UInt64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		public static string ToHexadecimal(this ulong id) => HexadecimalIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns a 26-character hexadecimal representation of the given <see cref="Decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public static string ToHexadecimal(this decimal id) => HexadecimalIdEncoder.Encode(id);
		/// <summary>
		/// <para>
		/// Returns a 32-character hexadecimal representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string ToHexadecimal(this Guid id) => HexadecimalIdEncoder.Encode(id);

		/// <summary>
		/// <para>
		/// Outputs a 16-character hexadecimal representation of the given <see cref="Int64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 16 bytes, to write the hexadecimal representation to.</param>
		public static void ToHexadecimal(this long id, Span<byte> bytes) => HexadecimalIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 16-character hexadecimal representation of the given <see cref="UInt64"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		/// <param name="bytes">At least 16 bytes, to write the hexadecimal representation to.</param>
		public static void ToHexadecimal(this ulong id, Span<byte> bytes) => HexadecimalIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 26-character hexadecimal representation of the given <see cref="Decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 26 bytes, to write the hexadecimal representation to.</param>
		public static void ToHexadecimal(this decimal id, Span<byte> bytes) => HexadecimalIdEncoder.Encode(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 32-character hexadecimal representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 32 bytes, to write the hexadecimal representation to.</param>
		public static void ToHexadecimal(this Guid id, Span<byte> bytes) => HexadecimalIdEncoder.Encode(id, bytes);

		#region Transcoding

#if NET7_0_OR_GREATER

		/// <summary>
		/// <para>
		/// Transcodes the given <see cref="Guid"/> into a <see cref="UInt128"/>, retaining the lexicographical ordering.
		/// </para>
		/// <para>
		/// Input values and their respective output values have the same relative ordering.
		/// The same is true of their string representations.
		/// The same is also true of their binary representations obtained through <see cref="BinaryIdEncoder"/>.
		/// </para>
		/// </summary>
		public static UInt128 ToUInt128(this Guid id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);
			BinaryIdEncoder.TryDecodeUInt128(bytes, out var result);
			return result;
		}

		/// <summary>
		/// <para>
		/// Transcodes the given <see cref="UInt128"/> into a <see cref="Guid"/>, retaining the lexicographical ordering.
		/// </para>
		/// <para>
		/// Input values and their respective output values have the same relative ordering.
		/// The same is true of their string representations.
		/// The same is also true of their binary representations obtained through <see cref="BinaryIdEncoder"/>.
		/// </para>
		/// </summary>
		public static Guid ToGuid(this UInt128 id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);
			BinaryIdEncoder.TryDecodeGuid(bytes, out var result);
			return result;
		}

#endif

		#endregion
	}
}
