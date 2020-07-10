using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class IdEncodingExtensions
	{
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric representation of the given <see cref="long"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string ToAlphanumeric(this long id) => IdEncoder.GetAlphanumeric(id);
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric representation of the given <see cref="ulong"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		public static string ToAlphanumeric(this ulong id) => IdEncoder.GetAlphanumeric(id);
		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric representation of the given <see cref="decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="CompanyUniqueId.CreateId"/>.</param>
		public static string ToAlphanumeric(this decimal id) => IdEncoder.GetAlphanumeric(id);
		/// <summary>
		/// <para>
		/// Returns a 22-character alphanumeric representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string ToAlphanumeric(this Guid id) => IdEncoder.GetAlphanumeric(id);

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="long"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this long id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="ulong"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this ulong id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric representation of the given <see cref="decimal"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="CompanyUniqueId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this decimal id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 22-character alphanumeric representation of the given <see cref="Guid"/> ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 22 bytes, to write the alphanumeric representation to.</param>
		public static void ToAlphanumeric(this Guid id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
	}
}
