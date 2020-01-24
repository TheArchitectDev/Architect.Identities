using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed class FluidIdGenerator : IIdGenerator
	{
		public static DateTime GetUtcNow() => DateTime.UtcNow;
		internal static readonly DateTime DefaultEpoch = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);

		internal static FluidIdGenerator Default => DefaultValue ?? throw new InvalidOperationException($"{nameof(Fluid)} was not configured. Call {nameof(FluidExtensions)}.{nameof(FluidExtensions.UseFluid)} on startup.");
		internal static FluidIdGenerator? DefaultValue = TestDetector.IsTestRun
			? new FluidIdGenerator(isProduction: false, GetUtcNow, applicationInstanceId: 1)
			: null;

		private Func<DateTime> Clock { get; }
		private Action<int> SleepAction { get; }

		internal ushort ApplicationInstanceId { get; }
		internal DateTime Epoch { get; }

		private byte TimestampBitCount { get; }
		private byte ApplicationInstanceIdBitCount { get; }
		private byte CounterBitCount { get; }

		private ulong MaxTimestamp => (1UL << this.TimestampBitCount) - 1;
		private ulong MaxApplicationInstanceId => (1UL << this.ApplicationInstanceIdBitCount) - 1;
		private ulong MaxCounterValue => (1UL << this.CounterBitCount) - 1;

		private readonly object _lockObject = new object();
		private ulong _counter;
		private ulong _previousTimestamp;

		/// <summary>
		/// </summary>
		/// <param name="utcClock">You can pass this class' own public static GetUtcNow method.</param>
		public FluidIdGenerator(bool isProduction,
			Func<DateTime> utcClock,
			ushort applicationInstanceId, DateTime? epoch = null,
			FluidBitDistribution? bitDistribution = null,
			Action<int>? sleepAction = null)
		{
			this.Clock = utcClock ?? throw new ArgumentNullException(nameof(utcClock));
			this.SleepAction = sleepAction ?? Thread.Sleep;

			var now = this.Clock();

			if (now.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException($"The given {nameof(utcClock)} produced a non-UTC datetime.");
			}

			if (epoch != null && epoch.Value.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException($"The {nameof(this.Epoch)} must be in UTC.");
			}
			if (epoch != null && epoch.Value.TimeOfDay != TimeSpan.Zero)
			{
				throw new ArgumentException($"The {nameof(this.Epoch)} must not include a time component.");
			}

			if (applicationInstanceId == 0 && isProduction)
			{
				throw new ArgumentException($"{nameof(this.ApplicationInstanceId)} value {applicationInstanceId} is not allowed in production.");
			}

			this.ApplicationInstanceId = applicationInstanceId;
			this.Epoch = epoch ?? DefaultEpoch;

			bitDistribution ??= FluidBitDistribution.Default;
			this.TimestampBitCount = bitDistribution.TimestampBitCount;
			this.ApplicationInstanceIdBitCount = bitDistribution.ApplicationInstanceIdBitCount;
			this.CounterBitCount = bitDistribution.CounterBitCount;
			System.Diagnostics.Debug.Assert(this.TimestampBitCount + this.ApplicationInstanceIdBitCount + this.CounterBitCount == bitDistribution.TotalBitCount);

			if (this.Epoch > now)
			{
				throw new ArgumentException($"The epoch, {this.Epoch:yyyy-MM-dd}, is in the future compared to the clock, {now:yyyy-MM-dd}.");
			}
			if (Math.Log2((now - this.Epoch).TotalMilliseconds) > this.TimestampBitCount)
			{
				throw new ArgumentException($"The {this.TimestampBitCount} timestamp bits are too few to range from the epoch, {this.Epoch:yyyy-MM-dd}, to now, {now:yyyy-MM-dd}.");
			}
			if (this.ApplicationInstanceId > this.MaxApplicationInstanceId)
			{
				throw new ArgumentException($"The {nameof(this.ApplicationInstanceId)} {this.ApplicationInstanceId} does not fit into {this.CounterBitCount} bits.");
			}

			// Avoid overflow when creating maximum datetimes
			var maxTimestamp = (ulong)(DateTime.MaxValue - now).TotalMilliseconds;
			maxTimestamp = Math.Min(this.MaxTimestamp, maxTimestamp);

			// Only inform about ApplicationInstanceId usage above a certain percentage, because we fill in gaps and may guess too low
			// We only know that AT LEAST this many are in use
			// So provide a rough minimum
			var appInstanceIdPercentageUsed = this.MaxApplicationInstanceId == 0UL ? 0UL : this.ApplicationInstanceId * 10UL / this.MaxApplicationInstanceId;
			if (appInstanceIdPercentageUsed >= 5)
				Console.WriteLine($"{nameof(Fluid)} has used over {10 * appInstanceIdPercentageUsed}% of its available {this.MaxApplicationInstanceId} application instance identifiers.");

			var maxDateTime = now.AddMilliseconds(maxTimestamp);
			var signedMaxDateTime = now.AddMilliseconds(maxTimestamp >> 1);
			var yearsRemaining = maxDateTime.Year - now.Year;
			var signedYearsRemaining = signedMaxDateTime.Year - now.Year;
			Console.WriteLine($"{nameof(Fluid)} has {yearsRemaining} ({signedYearsRemaining}) years of capacity remaining, until {maxDateTime:yyyy-MM-dd} ({signedMaxDateTime:yyyy-MM-dd}) for unsigned (signed) ID storage.");
		}

		public ulong CreateId() => this.CreateFluid();

		public long CreateSignedId() => this.CreateFluid();

		public Fluid CreateFluid()
		{
			return this.CreateLocallyUniqueValue();
		}

		private ulong CreateLocallyUniqueValue()
		{
			ulong timestamp;
			ulong counterValue;

			lock (this._lockObject)
			{
				timestamp = this.GetMillisecondsSinceEpoch();

				if (timestamp > this._previousTimestamp) counterValue = 0; // If the time has advanced, count from 0
				else counterValue = this._counter + 1; // Increment the counter otherwise (i.e. the clock tells the same time as before - or earlier, which we will handle)

				// We must never go back in time (the clock can be adjusted backwards)
				var requiredTimestamp = this._previousTimestamp;

				// If we have used up all counter values, then we need at least the next millisecond, and we can count from zero
				if (counterValue > this.MaxCounterValue)
				{
					requiredTimestamp++;
					counterValue = 0;
				}

				System.Diagnostics.Debug.Assert(requiredTimestamp >= this._previousTimestamp, "The required timestamp can never be BEFORE the previous timestamp.");

				// Ensure that we have reached the required moment in time
				// This protects us from clock rewinds and from reusing counter values
				var maxSleepMilliseconds = 1000UL;
				while (timestamp < requiredTimestamp && maxSleepMilliseconds > 0UL)
				{
					var millisecondsRequired = requiredTimestamp - timestamp;

					// If the clock was turned back a lot, we may need to wait for a long time
					// This is where NTP is essential, as it can prevent this
					// If the wait is more than a full second, then we surrunder to the risk of collisions rather than stalling that long
					if (millisecondsRequired > maxSleepMilliseconds) millisecondsRequired = maxSleepMilliseconds;
					
					maxSleepMilliseconds -= millisecondsRequired;
					this.SleepAction((int)millisecondsRequired);
					timestamp = this.GetMillisecondsSinceEpoch();
				}

				// Always update the timestamp forward before updating the counter
				// This keeps (theoretical) exceptions from introducing collisions
				System.Diagnostics.Debug.Assert(timestamp >= this._previousTimestamp || maxSleepMilliseconds == 0, "The timestamp was rewinded even though we waited.");
				System.Diagnostics.Debug.Assert(counterValue > this._counter || timestamp > this._previousTimestamp || maxSleepMilliseconds == 0, "Either the counter or the time must advance (or both).");
				this._previousTimestamp = timestamp;
				this._counter = counterValue;
			}

			if (timestamp > this.MaxTimestamp) throw new OverflowException($"{nameof(Fluid)}'s timestamp component overflowed its {this.TimestampBitCount} bits with value {timestamp}.");

			// Timestamp
			// Placed to the left of everything else
			var result = this.GetTimestampPartialFluid(timestamp);
			System.Diagnostics.Debug.Assert(result >> this.ApplicationInstanceIdBitCount >> this.CounterBitCount == timestamp || this.TimestampBitCount == 0);
			System.Diagnostics.Debug.Assert(result >> (this.ApplicationInstanceIdBitCount + this.CounterBitCount) << (this.ApplicationInstanceIdBitCount + this.CounterBitCount) == result);

			// Application instance id
			// Placed to the left of the counter
			System.Diagnostics.Debug.Assert((result | ((ulong)this.ApplicationInstanceId << this.CounterBitCount)) - ((ulong)this.ApplicationInstanceId << this.CounterBitCount) == result);
			result |= this.GetApplicationInstanceIdPartialFluid();
			System.Diagnostics.Debug.Assert((result & (~0UL >> this.TimestampBitCount)) >> this.CounterBitCount == this.ApplicationInstanceId || this.ApplicationInstanceIdBitCount == 0);

			// Counter
			// Placed to the right of everything else
			System.Diagnostics.Debug.Assert((result | counterValue) - counterValue == result);
			result |= this.GetCounterValuePartialFluid(counterValue);
			System.Diagnostics.Debug.Assert((result & (~0UL >> this.TimestampBitCount >> this.ApplicationInstanceIdBitCount)) == counterValue || this.CounterBitCount == 0);

			return result;
		}

		internal ulong GetMillisecondsSinceEpoch()
		{
			return (ulong)this.Clock().Subtract(this.Epoch).TotalMilliseconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong GetTimestampPartialFluid(ulong timestamp) => timestamp << this.ApplicationInstanceIdBitCount << this.CounterBitCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong GetApplicationInstanceIdPartialFluid() => (ulong)this.ApplicationInstanceId << this.CounterBitCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong GetCounterValuePartialFluid(ulong counterValue) => counterValue;
	}
}
