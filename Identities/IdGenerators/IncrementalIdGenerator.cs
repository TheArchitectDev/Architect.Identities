using System.Threading;

namespace Architect.Identities.IdGenerators
{
	/// <summary>
	/// <para>
	/// A simple incremental <see cref="IIdGenerator"/>, intended for testing purposes.
	/// </para>
	/// <para>
	/// Generates ID values 1, 2, 3, and so on.
	/// </para>
	/// <para>
	/// Although this type is thread-safe, single-threaded use also allows the generated IDs to be predicted.
	/// </para>
	/// </summary>
	public sealed class IncrementalIdGenerator : IIdGenerator
	{
		private long _lastGeneratedId = 0;

		public long CreateId()
		{
			return this.GenerateId();
		}

		public ulong CreateUnsignedId()
		{
			return (ulong)this.GenerateId();
		}

		private long GenerateId() => Interlocked.Increment(ref this._lastGeneratedId);
	}
}
