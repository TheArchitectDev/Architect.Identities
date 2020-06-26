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
		public static string ToAlphanumeric(this long id) => IdEncoder.GetAlphanumeric(id);
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric representation of the given <see cref="ulong"/> ID.
		/// </para>
		/// </summary>
		public static string ToAlphanumeric(this ulong id) => IdEncoder.GetAlphanumeric(id);
		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric representation of the given <see cref="decimal"/> ID.
		/// </para>
		/// </summary>
		public static string ToAlphanumeric(this decimal id) => IdEncoder.GetAlphanumeric(id);

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="long"/> ID.
		/// </para>
		/// </summary>
		public static void ToAlphanumeric(this long id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric representation of the given <see cref="ulong"/> ID.
		/// </para>
		/// </summary>
		public static void ToAlphanumeric(this ulong id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric representation of the given <see cref="decimal"/> ID.
		/// </para>
		/// </summary>
		public static void ToAlphanumeric(this decimal id, Span<byte> bytes) => IdEncoder.GetAlphanumeric(id, bytes);
	}
}
