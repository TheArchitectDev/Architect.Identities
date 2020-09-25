using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed class FluidBitDistribution
	{
		public override string ToString() => $"{this.GetType().Name} {nameof(this.TimestampBitCount)}={this.TimestampBitCount} {nameof(this.ApplicationInstanceIdBitCount)}={this.ApplicationInstanceIdBitCount} {nameof(this.CounterBitCount)}={this.CounterBitCount}";
		public override bool Equals(object? obj) => obj is FluidBitDistribution other && other.TimestampBitCount == this.TimestampBitCount && other.ApplicationInstanceIdBitCount == this.ApplicationInstanceIdBitCount && other.CounterBitCount == this.CounterBitCount;
		public override int GetHashCode() => HashCode.Combine(this.TimestampBitCount, this.ApplicationInstanceIdBitCount, this.CounterBitCount);

		public static readonly FluidBitDistribution Default = new FluidBitDistribution(timestampBitCount: 43, applicationInstanceIdBitCount: 11, counterBitCount: 10);

		public byte TimestampBitCount { get; }
		public byte ApplicationInstanceIdBitCount { get; }
		public byte CounterBitCount { get; }

		public byte TotalBitCount => (byte)(this.TimestampBitCount + this.ApplicationInstanceIdBitCount + this.CounterBitCount);

		public ulong MaxTimestamp => (1UL << this.TimestampBitCount) - 1;
		public ulong MaxApplicationInstanceId => (1UL << this.ApplicationInstanceIdBitCount) - 1;
		public ulong MaxCounterValue => (1UL << this.CounterBitCount) - 1;

		public FluidBitDistribution(byte timestampBitCount, byte applicationInstanceIdBitCount, byte counterBitCount)
		{
			if (timestampBitCount + applicationInstanceIdBitCount + counterBitCount != 64)
			{
				throw new ArgumentException("The bit counts must add up to 64.");
			}
			if (timestampBitCount > 63)
			{
				throw new ArgumentException("There must be no more than 63 timestamp bits."); // This limitation allows certain overflow checks without overflowing DURING the check
			}
			if (applicationInstanceIdBitCount > 16)
			{
				throw new ArgumentException("There must be no more than 16 application instance ID bits."); // This limitation allows the use of ushort
			}

			this.TimestampBitCount = timestampBitCount;
			this.ApplicationInstanceIdBitCount = applicationInstanceIdBitCount;
			this.CounterBitCount = counterBitCount;
		}
	}
}
