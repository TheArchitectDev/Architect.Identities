using System;
using System.Threading;

// ReSharper disable once CheckNamespace
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
#if NET7_0_OR_GREATER
		private readonly UInt128 _firstId;
#endif
		private long _previousIncrement = -1;

		public IncrementalDistributedId128Generator()
		{
#if NET7_0_OR_GREATER
			this._firstId = 1;
#else
			this._previousIncrement = 0;
#endif
		}

#if NET7_0_OR_GREATER

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

#endif

		public Guid CreateGuid()
		{
#if NET7_0_OR_GREATER
			var result = this.CreateId().ToGuid();
			return result;
#else
			var increment = (ulong)Interlocked.Increment(ref this._previousIncrement);

			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(increment, bytes[8..]);
			BinaryIdEncoder.TryDecodeGuid(bytes, out var result);
			return result;
#endif
		}
	}
}
