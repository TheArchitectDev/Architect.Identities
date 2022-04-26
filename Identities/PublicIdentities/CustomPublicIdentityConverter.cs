// ReSharper disable once CheckNamespace
using System;

namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A custom <see cref="IPublicIdentityConverter"/>, mainly intended for testing purposes.
	/// </para>
	/// <para>
	/// The inner workings and resulting values of this implementation are subject to change.
	/// </para>
	/// </summary>
	public sealed class CustomPublicIdentityConverter : IPublicIdentityConverter
	{
		private AesPublicIdentityConverter InternalConverter { get; }

		/// <summary>
		/// Constructs a default instance using a zero key.
		/// </summary>
		public CustomPublicIdentityConverter()
			: this(new AesPublicIdentityConverter(aesKey: new byte[16]))
		{
		}

		/// <summary>
		/// Constructs an instance using the given key.
		/// </summary>
		public CustomPublicIdentityConverter(ReadOnlySpan<byte> keyBytes)
			: this(new AesPublicIdentityConverter(keyBytes))
		{
		}

		/// <summary>
		/// Constructs an instance using the given base64-encoded key.
		/// </summary>
		public CustomPublicIdentityConverter(string base64Key)
			: this(new AesPublicIdentityConverter(Convert.FromBase64String(base64Key ?? throw new ArgumentNullException(nameof(base64Key)))))
		{
		}

		private CustomPublicIdentityConverter(AesPublicIdentityConverter internalConverter)
		{
			this.InternalConverter = internalConverter;
		}

		public void Dispose()
		{
			this.InternalConverter.Dispose();
		}

		public Guid GetPublicRepresentation(ulong id)
		{
			return this.InternalConverter.GetPublicRepresentation(id);
		}

		public Guid GetPublicRepresentation(decimal id)
		{
			return this.InternalConverter.GetPublicRepresentation(id);
		}

		public bool TryGetUlong(Guid publicId, out ulong id)
		{
			return this.InternalConverter.TryGetUlong(publicId, out id);
		}

		public bool TryGetDecimal(Guid publicId, out decimal id)
		{
			return this.InternalConverter.TryGetDecimal(publicId, out id);
		}
	}
}
