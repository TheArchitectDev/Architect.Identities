using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// A manually configured <see cref="IIdGenerator"/>, mainly intended for testing purposes.
	/// </summary>
	public sealed class CustomIdGenerator : IIdGenerator
	{
		private Func<ulong> Generator { get; }

		/// <summary>
		/// Constructs a new instance that always returns the given ID.
		/// </summary>
		public CustomIdGenerator(ulong id)
			: this(() => id)
		{
		}

		/// <summary>
		/// Constructs a new instance that invokes the given generator whenever a new ID is requested.
		/// </summary>
		public CustomIdGenerator(Func<ulong> generator)
		{
			this.Generator = generator ?? throw new ArgumentNullException(nameof(generator));
		}

		public long CreateId()
		{
			return (long)this.Generator();
		}

		public ulong CreateUnsignedId()
		{
			return this.Generator();
		}
	}
}
