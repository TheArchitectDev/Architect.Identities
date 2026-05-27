using System;
using System.Threading;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A simple incremental <see cref="IDistributedId128Generator"/>, intended for testing purposes.
	/// </para>
	/// <para>
	/// Generates ID values equivalent to 1, 2, 3, and so on.
	/// </para>
	/// <para>
	/// Although this type is thread-safe, single-threaded use additionally allows the generated IDs to be predicted.
	/// </para>
	/// </summary>
	public sealed class IncrementalDistributedId128Generator : IDistributedId128Generator
	{
		private readonly UInt128 _firstId;
		private long _previousIncrement = -1;

		public IncrementalDistributedId128Generator()
		{
			this._firstId = 1;
		}

		public IncrementalDistributedId128Generator(Guid firstId)
			: this(firstId.ToUInt128())
		{
		}

		public IncrementalDistributedId128Generator(UInt128 firstId)
		{
			this._firstId = firstId;
		}

		public UInt128 CreateId()
		{
			return this._firstId + (ulong)Interlocked.Increment(ref this._previousIncrement);
		}

		public Guid CreateGuid()
		{
			var result = this.CreateId().ToGuid();
			return result;
		}
	}
}
