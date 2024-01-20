using System.Threading;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A simple incremental <see cref="IDistributedIdGenerator"/>, intended for testing purposes.
	/// </para>
	/// <para>
	/// Generates ID values 1, 2, 3, and so on.
	/// </para>
	/// <para>
	/// Although this type is thread-safe, single-threaded use additionally allows the generated IDs to be predicted.
	/// </para>
	/// </summary>
	public sealed class IncrementalDistributedIdGenerator : IDistributedIdGenerator
	{
		private readonly ulong _firstId;
		private long _previousIncrement = -1;

		public IncrementalDistributedIdGenerator()
			: this(firstId: 1)
		{
		}

		public IncrementalDistributedIdGenerator(ulong firstId = 1)
		{
			this._firstId = firstId;
		}

		public decimal CreateId()
		{
			var id = this.GenerateId();
			return id;
		}

		private decimal GenerateId()
		{
			return this._firstId + (decimal)Interlocked.Increment(ref this._previousIncrement);
		}
	}
}
