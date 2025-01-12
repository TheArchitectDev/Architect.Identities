using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// A manually configured <see cref="IDistributedIdGenerator"/>, mainly intended for testing purposes.
	/// </summary>
	public sealed class CustomDistributedIdGenerator : IDistributedIdGenerator
	{
		public Func<decimal> IdGenerator { get; }

		/// <summary>
		/// Constructs a new instance that always returns the given ID.
		/// </summary>
		public CustomDistributedIdGenerator(decimal id)
			: this(() => id)
		{
		}

		/// <summary>
		/// Constructs a new instance that invokes the given generator whenever a new ID is requested.
		/// </summary>
		public CustomDistributedIdGenerator(Func<decimal> idGenerator)
		{
			this.IdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		}

		public decimal CreateId()
		{
			var id = this.IdGenerator();
			return id;
		}
	}
}
