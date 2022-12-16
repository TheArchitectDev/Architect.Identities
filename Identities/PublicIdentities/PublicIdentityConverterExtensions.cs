using System;

namespace Architect.Identities
{
	/// <summary>
	/// Provides additional methods on <see cref="IPublicIdentityConverter"/>.
	/// </summary>
	public static class PublicIdentityConverterExtensions
	{
		/// <summary>
		/// <para>
		/// Returns the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns null if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public static long? GetLongOrDefault(this IPublicIdentityConverter converter, Guid publicId)
		{
			if (converter is null) throw new ArgumentNullException(nameof(converter));
			return converter.TryGetLong(publicId, out var id) ? id : null;
		}
		/// <summary>
		/// <para>
		/// Returns the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns null if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public static ulong? GetUlongOrDefault(this IPublicIdentityConverter converter, Guid publicId)
		{
			if (converter is null) throw new ArgumentNullException(nameof(converter));
			return converter.TryGetUlong(publicId, out var id) ? id : null;
		}
		/// <summary>
		/// <para>
		/// Returns the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns null if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public static decimal? GetDecimalOrDefault(this IPublicIdentityConverter converter, Guid publicId)
		{
			if (converter is null) throw new ArgumentNullException(nameof(converter));
			return converter.TryGetDecimal(publicId, out var id) ? id : null;
		}
	}
}
