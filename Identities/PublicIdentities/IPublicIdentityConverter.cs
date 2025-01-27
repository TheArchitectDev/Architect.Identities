using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// Provides deterministic conversions between local and public IDs.
	/// This allows local IDs to be kept hidden, with public IDs directly based on them, without the bookkeeping that comes with unrelated public IDs.
	/// </summary>
	public interface IPublicIdentityConverter : IDisposable
	{
		/// <summary>
		/// <para>
		/// Returns a 16-byte public representation of the given ID.
		/// </para>
		/// <para>
		/// The public representation is shaped much like a <see cref="Guid"/> and is indistinguishable from random noise.
		/// Only with possession of the configured key can it be converted back to the original ID.
		/// </para>
		/// <para>
		/// The various ID encoders in the package provide methods to encode the resulting object in various ways, such as in binary, alphanumeric, or hexadecimal form.
		/// </para>
		/// </summary>
		public Guid GetPublicRepresentation(long id)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
			return this.GetPublicRepresentation((ulong)id);
		}
		/// <summary>
		/// <para>
		/// Returns a 16-byte public representation of the given ID.
		/// </para>
		/// <para>
		/// The public representation is shaped much like a <see cref="Guid"/> and is indistinguishable from random noise.
		/// Only with possession of the configured key can it be converted back to the original ID.
		/// </para>
		/// <para>
		/// The various ID encoders in the package provide methods to encode the resulting object in various ways, such as in binary, alphanumeric, or hexadecimal form.
		/// </para>
		/// </summary>
		public Guid GetPublicRepresentation(ulong id);
		/// <summary>
		/// <para>
		/// Returns a 16-byte public representation of the given ID.
		/// </para>
		/// <para>
		/// The public representation is shaped much like a <see cref="Guid"/> and is indistinguishable from random noise.
		/// Only with possession of the configured key can it be converted back to the original ID.
		/// </para>
		/// <para>
		/// The various ID encoders in the package provide methods to encode the resulting object in various ways, such as in binary, alphanumeric, or hexadecimal form.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public Guid GetPublicRepresentation(decimal id);
#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Returns a 16-byte public representation of the given ID.
		/// </para>
		/// <para>
		/// The public representation is shaped much like a <see cref="Guid"/> and is indistinguishable from random noise.
		/// Only with possession of the configured key can it be converted back to the original ID.
		/// </para>
		/// <para>
		/// The various ID encoders in the package provide methods to encode the resulting object in various ways, such as in binary, alphanumeric, or hexadecimal form.
		/// </para>
		/// </summary>
		/// <param name="id">Any unsigned 128-bit numeric ID.</param>
		public Guid GetPublicRepresentation(UInt128 id);
#endif
		/// <summary>
		/// <para>
		/// Returns a 16-byte public representation of the given ID.
		/// </para>
		/// <para>
		/// The public representation is shaped much like a <see cref="Guid"/> and is indistinguishable from random noise.
		/// Only with possession of the configured key can it be converted back to the original ID.
		/// </para>
		/// <para>
		/// The various ID encoders in the package provide methods to encode the resulting object in various ways, such as in binary, alphanumeric, or hexadecimal form.
		/// </para>
		/// </summary>
		/// <param name="id">Any 128-bit ID.</param>
		public Guid GetPublicRepresentation(Guid id);

		/// <summary>
		/// <para>
		/// Outputs the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns false if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public bool TryGetLong(Guid publicId, out long id)
		{
			if (!this.TryGetUlong(publicId, out var ulongId) || ulongId > Int64.MaxValue)
			{
				id = default;
				return false;
			}
			id = (long)ulongId;
			return true;
		}
		/// <summary>
		/// <para>
		/// Outputs the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns false if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public bool TryGetUlong(Guid publicId, out ulong id);
		/// <summary>
		/// <para>
		/// Outputs the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method returns false if the input value was not created by the same converter using the same configuration.
		/// </para>
		/// </summary>
		public bool TryGetDecimal(Guid publicId, out decimal id);
#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Outputs the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method always returns true. It follows the "Try*" API shape for consistency with other overloads.
		/// </para>
		/// </summary>
		public bool TryGetUInt128(Guid publicId, out UInt128 id);
#endif
		/// <summary>
		/// <para>
		/// Outputs the original ID represented by the given public ID.
		/// </para>
		/// <para>
		/// This method always returns true. It follows the "Try*" API shape for consistency with other overloads.
		/// </para>
		/// </summary>
		public bool TryGetGuid(Guid publicId, out Guid id);
	}
}
