using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	// This implementation still needs to be compared to simply borrowing from and returning to a ConcurrentQueue
	/// <summary>
	/// Manages a fixed-size, lock-free pool of instances.
	/// It helps pool expensive resources that are not thread-safe.
	/// </summary>
	[Obsolete("Reserved for future use.", error: true)]
	internal sealed class FixedInstancePool<T>
	{
		private ImmutableArray<T> Instances { get; }
		private int _freeInstanceBits;

		/// <summary>
		/// Constructs a fixed-size, lock-free pool pool of instances constructed using the given factory.
		/// It helps pool expensive resources that are not thread-safe.
		/// </summary>
		/// <param name="factory">The factory method invoked on construction to create the instances.</param>
		/// <param name="instanceCount">The number of instances. By default, Environment.ProcessorCount is used. The value is always capped at 31.</param>
		public FixedInstancePool(Func<T> factory, int? instanceCount)
		{
			// Acquire [CPU count] instances (max 31)
			var count = Math.Min(31, instanceCount ?? Environment.ProcessorCount);
			var instances = new T[count];
			for (var i = 0; i < instances.Length; i++) instances[i] = factory();

			this.Instances = instances.ToImmutableArray();
			this._freeInstanceBits = Int32.MaxValue;
		}

		public T Borrow()
		{
			var claimedIndex = this.TryFastClaimLowestFreeIndex(); // Fast path
			if (claimedIndex < 0) claimedIndex = this.ClaimLowestFreeIndex(); // SpinWait path

			return this.Instances[claimedIndex];
		}

		public void Return(T instance)
		{
			var index = this.Instances.IndexOf(instance);
			if (!this.TryFastReleaseIndex(index)) this.ReleaseIndex(index);
		}

		/// <summary>
		/// Fast-path implementation. Returns -1 if immediate success is not achieved.
		/// </summary>
		private int TryFastClaimLowestFreeIndex()
		{
			var value = this._freeInstanceBits;

			// The lowest free index is the lowest bit that is SET
			var singleAvailableBit = value & ~(value - 1); // Get the lowest bit that is SET
			var bitIndex = 31 - BitOperations.LeadingZeroCount((uint)singleAvailableBit); // Get the index of that bit (0=LSB)
			var valueWithTheNewBitUnset = value & ~(1 << bitIndex); // Clear (unset) that bit

			// If there are no bits available (i.e. unset)
			// Or if we fail to change the value because someone else changed it since we observed it
			// Return failure
			if (value == 0 || Interlocked.CompareExchange(ref this._freeInstanceBits, valueWithTheNewBitUnset, value) != value) return -1;

			return bitIndex;
		}

		private int ClaimLowestFreeIndex()
		{
			var spinWait = new SpinWait();

			while (true) // Until we have something to return
			{
				spinWait.SpinOnce();

				var index = this.TryFastClaimLowestFreeIndex();
				if (index >= 0) return index;
			}
		}

		private bool TryFastReleaseIndex(int index)
		{
			var singleBit = 1 << index;
			var value = this._freeInstanceBits;
			System.Diagnostics.Debug.Assert((value & singleBit) == 0);
			var valueWithTheNewBitSet = value | singleBit;
			return Interlocked.CompareExchange(ref this._freeInstanceBits, valueWithTheNewBitSet, value) == value;
		}

		private void ReleaseIndex(int index)
		{
			var spinWait = new SpinWait();

			while (true) // Until we succeed
			{
				spinWait.SpinOnce();

				if (this.TryFastReleaseIndex(index)) return;
			}
		}
	}
}
