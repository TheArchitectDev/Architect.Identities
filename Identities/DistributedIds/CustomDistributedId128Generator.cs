using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// A manually configured <see cref="IDistributedId128Generator"/>, mainly intended for testing purposes.
	/// </summary>
	public sealed class CustomDistributedId128Generator : IDistributedId128Generator
	{
		private delegate void IdWriter(Span<byte> bytes);

		private IdWriter IdGenerator { get; }

		private CustomDistributedId128Generator(IdWriter idGenerator)
		{
			this.IdGenerator = idGenerator;
		}

#if NET7_0_OR_GREATER

		/// <summary>
		/// Constructs a new instance that always returns the given ID.
		/// </summary>
		public CustomDistributedId128Generator(UInt128 id)
			: this(bytes => BinaryIdEncoder.Encode(id, bytes))
		{
		}

		/// <summary>
		/// Constructs a new instance that invokes the given generator whenever a new ID is requested.
		/// </summary>
		public CustomDistributedId128Generator(Func<UInt128> idGenerator)
			: this(bytes => BinaryIdEncoder.Encode(idGenerator(), bytes))
		{
		}

		public UInt128 CreateId()
		{
			Span<byte> bytes = stackalloc byte[16];
			this.IdGenerator(bytes);
			BinaryIdEncoder.TryDecodeUInt128(bytes, out var result);
			return result;
		}

#endif

		/// <summary>
		/// Constructs a new instance that always returns the given ID.
		/// </summary>
		public CustomDistributedId128Generator(Guid id)
			: this(bytes => BinaryIdEncoder.Encode(id, bytes))
		{
		}

		/// <summary>
		/// Constructs a new instance that invokes the given generator whenever a new ID is requested.
		/// </summary>
		public CustomDistributedId128Generator(Func<Guid> idGenerator)
			: this(bytes => BinaryIdEncoder.Encode(idGenerator(), bytes))
		{
		}

		public Guid CreateGuid()
		{
			Span<byte> bytes = stackalloc byte[16];
			this.IdGenerator(bytes);
			BinaryIdEncoder.TryDecodeGuid(bytes, out var result);
			return result;
		}
	}
}
