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
	/// Although this type is thread-safe, single-threaded use also allows the generated IDs to be predicted.
	/// </para>
	/// </summary>
	public sealed class IncrementalDistributedIdGenerator : IDistributedIdGenerator
	{
		private long _lastGeneratedId = 0;

		public decimal CreateId()
		{
			var id = (decimal)this.GenerateId();
			return id;
		}

		private long GenerateId() => Interlocked.Increment(ref this._lastGeneratedId);
	}
}
