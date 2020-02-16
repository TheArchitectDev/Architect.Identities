using System;
using System.Diagnostics;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators.Fluids
{
	public sealed class FluidIdGeneratorTests
	{
		/// <summary>
		/// A sample clock value, for use in most unit tests.
		/// </summary>
		private static readonly DateTime ClockValue = new DateTime(2020, 01, 02, 0, 0, 0, millisecond: 1, DateTimeKind.Utc);
		/// <summary>
		/// A sample clock based on ClockValue, for use in most unit tests.
		/// </summary>
		private static Func<DateTime> Clock => () => ClockValue;
		/// <summary>
		/// A sample epoch value, for use in most unit tests.
		/// </summary>
		private static readonly DateTime Epoch = new DateTime(2020, 01, 02, 0, 0, 0, DateTimeKind.Utc);
		/// <summary>
		/// A sample ApplicationInstanceId value, for use in most unit tests.
		/// </summary>
		private const ushort ApplicationInstanceId = 123;
		/// <summary>
		/// A sample bit distribution, for use in most unit tests.
		/// </summary>
		private static readonly FluidBitDistribution BitDistribution = new FluidBitDistribution(timestampBitCount: 43, applicationInstanceIdBitCount: 11, counterBitCount: 10);

		private static ulong GetMilliseconds(DateTime dateTime, DateTime epoch) => (ulong)(dateTime - epoch).TotalMilliseconds;
		private static ulong GetMilliseconds(Fluid fluid) => (ulong)fluid >> BitDistribution.ApplicationInstanceIdBitCount >> BitDistribution.CounterBitCount;
		private static ushort GetApplicationInstanceId(Fluid fluid) => (ushort)((ulong)fluid << BitDistribution.TimestampAndUnusedBitCount >> BitDistribution.TimestampAndUnusedBitCount
																																		>> BitDistribution.CounterBitCount);
		private static ulong GetCounterValue(Fluid fluid) => (ulong)fluid << BitDistribution.TimestampAndUnusedBitCount << BitDistribution.ApplicationInstanceIdBitCount
															>> BitDistribution.TimestampAndUnusedBitCount >> BitDistribution.ApplicationInstanceIdBitCount;

		[Fact]
		public void GetDefaultValue_InUnitTest_ShouldBePopulated()
		{
			Assert.NotNull(FluidIdGenerator.Default);
		}

		[Fact]
		public void GetDefault_InUnitTest_ShouldSucceed()
		{
			_ = FluidIdGenerator.Default;
		}

		[Fact]
		public void GetUtcNow_WhenInvoked_ShouldReturnUtcNow()
		{
			var getUtcNowResult = FluidIdGenerator.GetUtcNow();
			var utcNow = DateTime.UtcNow;
			Assert.True((utcNow - getUtcNowResult) < TimeSpan.FromMilliseconds(1000));
		}

		[Fact]
		public void GetDefaultEpoch_Regularly_ShouldReturnExpectedDefaultEpoch()
		{
			var defaultEpoch = FluidIdGenerator.DefaultEpoch;
			var expectedDefaultEpoch = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
			Assert.Equal(expectedDefaultEpoch, defaultEpoch);
			Assert.Equal(expectedDefaultEpoch.Kind, defaultEpoch.Kind);
		}

		[Fact]
		public void GetDefaultEpoch_Regularly_ShouldReturnUtcDateTime()
		{
			var defaultEpoch = FluidIdGenerator.DefaultEpoch;
			Assert.Equal(DateTimeKind.Utc, defaultEpoch.Kind);
		}

		[Fact]
		public void Construct_WithNullClock_ShouldThrow()
		{
			Assert.Throws<ArgumentNullException>(() => new FluidIdGenerator(isProduction: false, utcClock: null, ApplicationInstanceId));
		}

		[Fact]
		public void Construct_WithNonUtcClock_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false, utcClock: () => DateTime.SpecifyKind(ClockValue, DateTimeKind.Local), ApplicationInstanceId));
		}

		[Fact]
		public void Construct_WithNonUtcEpoch_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, ApplicationInstanceId, DateTime.SpecifyKind(Epoch, DateTimeKind.Local)));
		}

		[Fact]
		public void Construct_WithEpochWithTimeComponent_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, ApplicationInstanceId, Epoch.AddHours(1)));
		}

		[Fact]
		public void Construct_WithEpochInTheFuture_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false,
				utcClock: () => new DateTime(2010, 01, 01, 0, 0, 0, DateTimeKind.Utc),
				ApplicationInstanceId,
				epoch: new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public void Construct_WithEpochTooLongAgoToFitInTimestampBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false,
				utcClock: () => new DateTime(9999, 01, 01, 0, 0, 0, DateTimeKind.Utc),
				ApplicationInstanceId,
				epoch: new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public void Construct_WithZeroApplicationInstanceIdOutsideProduction_ShouldSucceed()
		{
			_ = new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, applicationInstanceId: 0);
		}

		[Fact]
		public void Construct_WithZeroApplicationInstanceIdInProduction_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: true, FluidIdGenerator.GetUtcNow, applicationInstanceId: 0));
		}

		[Fact]
		public void Construct_WithApplicationInstanceIdTooGreatToFitInBits_ShouldThrow()
		{
			Debug.Assert(FluidBitDistribution.Default.MaxApplicationInstanceId < UInt16.MaxValue);

			Assert.Throws<ArgumentException>(() => new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, applicationInstanceId: UInt16.MaxValue));
		}

		[Fact]
		public void Construct_WithDefaultBitDistribution_ShouldDistributeBitsAsExpected()
		{
			var factory = new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, ApplicationInstanceId);
			var maxTimestamp = (ulong)(FluidIdGenerator.DefaultEpoch.AddMilliseconds((1UL << FluidBitDistribution.Default.TimestampBitCount) - 1) - FluidIdGenerator.DefaultEpoch).TotalMilliseconds;

			var timestampPartial = factory.GetTimestampPartialFluid(maxTimestamp);
			var applicationInstanceIdPartial = factory.GetApplicationInstanceIdPartialFluid();
			var counterValuePartial = factory.GetCounterValuePartialFluid(2UL);

			var timestampMask = FluidBitDistribution.Default.MaxTimestamp << FluidBitDistribution.Default.ApplicationInstanceIdBitCount << FluidBitDistribution.Default.CounterBitCount;
			Assert.Equal(0UL, timestampPartial & ~timestampMask);
			Assert.Equal(maxTimestamp, timestampPartial >> FluidBitDistribution.Default.ApplicationInstanceIdBitCount >> FluidBitDistribution.Default.CounterBitCount);

			var applicationInstanceIdMask = FluidBitDistribution.Default.MaxApplicationInstanceId << FluidBitDistribution.Default.CounterBitCount;
			Assert.Equal(0UL, applicationInstanceIdPartial & ~applicationInstanceIdMask);
			Assert.Equal(ApplicationInstanceId, applicationInstanceIdPartial >> FluidBitDistribution.Default.CounterBitCount);

			var counterMask = FluidBitDistribution.Default.MaxCounterValue;
			Assert.Equal(0UL, counterValuePartial & ~counterMask);
			Assert.Equal(2UL, counterValuePartial);
		}

		[Fact]
		public void Construct_WithBitDistribution_ShouldDistributeBitsAsExpected()
		{
			var factory = new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, ApplicationInstanceId, bitDistribution: BitDistribution);
			var maxTimestamp = (ulong)(FluidIdGenerator.DefaultEpoch.AddMilliseconds((1UL << FluidBitDistribution.Default.TimestampBitCount) - 1) - FluidIdGenerator.DefaultEpoch).TotalMilliseconds;

			var timestampPartial = factory.GetTimestampPartialFluid(maxTimestamp);
			var applicationInstanceIdPartial = factory.GetApplicationInstanceIdPartialFluid();
			var counterValuePartial = factory.GetCounterValuePartialFluid(2UL);

			var timestampMask = BitDistribution.MaxTimestamp << BitDistribution.ApplicationInstanceIdBitCount << BitDistribution.CounterBitCount;
			Assert.Equal(0UL, timestampPartial & ~timestampMask);
			Assert.Equal(maxTimestamp, timestampPartial >> BitDistribution.ApplicationInstanceIdBitCount >> BitDistribution.CounterBitCount);

			var applicationInstanceIdMask = BitDistribution.MaxApplicationInstanceId << BitDistribution.CounterBitCount;
			Assert.Equal(0UL, applicationInstanceIdPartial & ~applicationInstanceIdMask);
			Assert.Equal(ApplicationInstanceId, applicationInstanceIdPartial >> BitDistribution.CounterBitCount);

			var counterMask = BitDistribution.MaxCounterValue;
			Assert.Equal(0UL, counterValuePartial & ~counterMask);
			Assert.Equal(2UL, counterValuePartial);
		}

		[Fact]
		public void Construct_WithApplicationInstanceId_ShouldUseThatValue()
		{
			var factory = new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, applicationInstanceId: ApplicationInstanceId, bitDistribution: BitDistribution);
			var fluid = factory.CreateFluid();

			Assert.Equal(ApplicationInstanceId, GetApplicationInstanceId(fluid));
		}

		[Fact]
		public void Construct_WithClock_ShouldUseThat()
		{
			var clockValue = FluidIdGenerator.DefaultEpoch;
			DateTime FixedClock() => clockValue;
			var factory = new FluidIdGenerator(isProduction: false, FixedClock, applicationInstanceId: ApplicationInstanceId, bitDistribution: BitDistribution);
			var fluid = factory.CreateFluid();

			Assert.Equal(GetMilliseconds(clockValue, factory.Epoch), GetMilliseconds(fluid));
		}

		[Fact]
		public void Construct_WithClockAndEpoch_ShouldUseThose()
		{
			var factory = new FluidIdGenerator(isProduction: false, Clock, ApplicationInstanceId, epoch: Epoch, BitDistribution);
			var fluid = factory.CreateFluid();

			Assert.Equal(GetMilliseconds(ClockValue, factory.Epoch), GetMilliseconds(fluid));
		}

		[Fact]
		public void Construct_WithOverflowingTimestampBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() =>
				new FluidIdGenerator(isProduction: false, utcClock: () => new DateTime(9999, 01, 01, 0, 0, 0, DateTimeKind.Utc), ApplicationInstanceId, Epoch, BitDistribution));
		}

		[Fact]
		public void GetMillisecondsSinceEpoch_Regularly_ShouldReturnExpectedValue()
		{
			var factory = new FluidIdGenerator(isProduction: false, 
				utcClock: () => new DateTime(2020, 01, 01, 0, 0, 0, millisecond: 1, DateTimeKind.Utc),
				ApplicationInstanceId,
				epoch: new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var milliseconds = factory.GetMillisecondsSinceEpoch();

			Assert.Equal(1UL, milliseconds);
		}

		[Fact]
		public void CreateFluid_WithNoApplicationInstanceIdAndNoCounter_ShouldEqualTimestamp()
		{
			var factory = new FluidIdGenerator(isProduction: false, Clock, applicationInstanceId: 0, Epoch,
				new FluidBitDistribution(timestampBitCount: 63, applicationInstanceIdBitCount: 0, counterBitCount: 0));

			var fluid = factory.CreateFluid();

			Assert.Equal(GetMilliseconds(ClockValue, factory.Epoch), (ulong)fluid);
		}

		[Fact]
		public void CreateFluid_TheFirstTime_ShouldUseCounterValue0()
		{
			var factory = new FluidIdGenerator(isProduction: false, Clock, ApplicationInstanceId, epoch: Epoch, BitDistribution);

			var fluid = factory.CreateFluid();

			Assert.Equal(0UL, GetCounterValue(fluid));
		}

		[Fact]
		public void CreateFluid_TheSecondTime_ShouldUseCounterValue1()
		{
			var factory = new FluidIdGenerator(isProduction: false, Clock, ApplicationInstanceId, epoch: Epoch, BitDistribution);

			factory.CreateFluid();
			var fluid = factory.CreateFluid();

			Assert.Equal(1UL, GetCounterValue(fluid));
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1024Calls_ShouldNotSleep()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			var sleepCount = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch.AddMilliseconds(sleepCount == 0 ? 1 : 2), ApplicationInstanceId, epoch: Epoch, BitDistribution,
				sleepAction: ms => sleepCount++);

			for (var i = 0; i < 1024; i++) factory.CreateFluid();

			Assert.Equal(0, sleepCount);
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1025Calls_ShouldSleepOnce()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			var sleepCount = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch.AddMilliseconds(sleepCount == 0 ? 1 : 2), ApplicationInstanceId, epoch: Epoch, BitDistribution,
				sleepAction: ms => sleepCount++);

			for (var i = 0; i < 1025; i++) factory.CreateFluid();

			Assert.Equal(1, sleepCount);
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1025Calls_ShouldSleepNoMoreThan1Second()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			var sleepMs = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch.AddMilliseconds(sleepMs == 0 ? 1 : 2), ApplicationInstanceId, epoch: Epoch, BitDistribution,
				sleepAction: ms => sleepMs += ms);

			for (var i = 0; i < 1025; i++) factory.CreateFluid();

			Assert.True(sleepMs <= 1000);
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1025CallsAndStuckClock_ShouldSleep1Second()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			var sleepMs = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch, ApplicationInstanceId, epoch: Epoch, BitDistribution,
				sleepAction: ms => sleepMs += ms);

			for (var i = 0; i < 1025; i++) factory.CreateFluid();

			Assert.True(sleepMs == 1000);
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1024Calls_ShouldCountUpTo1023()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			// Timestamp advances after 1024 invocations
			var counter = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch.AddMilliseconds(counter++ >= 1024 ? 2 : 1), ApplicationInstanceId, epoch: Epoch, BitDistribution);
			counter = 0; // Reset from initial internal call

			for (var i = 0UL; i < 1024; i++) Assert.Equal(i, GetCounterValue(factory.CreateFluid()));
		}

		[Fact]
		public void CreateFluid_With10CounterBitsAnd1026Calls_ShouldCount0And1OnTheLastTwoCalls()
		{
			Debug.Assert(BitDistribution.CounterBitCount == 10);

			// Timestamp advances after 1024 invocations
			var counter = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: () => Epoch.AddMilliseconds(counter++ >= 1024 ? 2 : 1), ApplicationInstanceId, epoch: Epoch, BitDistribution);
			counter = 0;

			for (var i = 0UL; i < 1024; i++) factory.CreateFluid();
			var fluid1024 = factory.CreateFluid();
			var fluid1025 = factory.CreateFluid();

			Assert.Equal(0UL, GetCounterValue(fluid1024));
			Assert.Equal(1UL, GetCounterValue(fluid1025));
		}

		[Fact]
		public void CreateFluid_TwiceWithAdvancingTimestamps_ShouldCount0Twice()
		{
			var invocationCount = 0;
			DateTime AutoAdvancingClock() => Epoch.AddMilliseconds(invocationCount++);
			
			var factory = new FluidIdGenerator(isProduction: false, utcClock: AutoAdvancingClock, ApplicationInstanceId, epoch: Epoch, BitDistribution);

			var fluid1 = factory.CreateFluid();
			var fluid2 = factory.CreateFluid();

			Assert.Equal(0UL, GetCounterValue(fluid1));
			Assert.Equal(0UL, GetCounterValue(fluid2));
		}

		[Fact]
		public void CreateFluid_TwiceWithRewindingClock_ShouldSleep1Second()
		{
			var clockDirection = 0;
			DateTime AutorewindingClock() => Epoch.AddDays(1).AddMinutes(clockDirection++ < 0 ? -1 : +1); // Go back once, on the second call, then proceed

			var sleepMs = 0;
			var factory = new FluidIdGenerator(isProduction: false, utcClock: AutorewindingClock, ApplicationInstanceId, epoch: Epoch, BitDistribution,
				sleepAction: ms => sleepMs += ms);

			factory.CreateFluid();
			clockDirection = -1; // Make the next one clock invocation return a rewinded datetime
			factory.CreateFluid();

			Assert.True(sleepMs == 1000); // We would need a minute to catch up, but will never sleep for more than a second
		}

		[Fact]
		public void CreateFluid_Regularly_ShouldCreateExpectedValue()
		{
			var factory = new FluidIdGenerator(isProduction: false, Clock, ApplicationInstanceId, Epoch, BitDistribution);

			var fluid = factory.CreateFluid();

			Debug.Assert((ulong)(ClockValue - Epoch).TotalMilliseconds == 1);
			var milliseconds = 1UL << BitDistribution.ApplicationInstanceIdBitCount << BitDistribution.CounterBitCount;
			var applicationInstanceId = (ulong)ApplicationInstanceId << BitDistribution.CounterBitCount;
			const ulong counterValue = 0;
			var expectedValue = milliseconds | applicationInstanceId | counterValue;
			Assert.Equal(expectedValue, (ulong)fluid);
		}


		[Fact]
		public void CreateFluid_WithClockThrowingOnSecondInvocationForOneFluid_ShouldNotChangeCounter()
		{
			var invocationCount = 0;
			DateTime ClockThatThrowsOnThirdInvocation()
			{
				if (++invocationCount == 3) throw new Exception("Deliberately throws on third invocation.");
				return ClockValue;
			}

			var factory = new FluidIdGenerator(isProduction: false, ClockThatThrowsOnThirdInvocation, ApplicationInstanceId, Epoch, BitDistribution);

			factory.CreateFluid(); // Ensure that a "previous timestamp" was registered

			Assert.Throws<Exception>(() => factory.CreateFluid()); // This one throws

			var fluid = factory.CreateFluid(); // This one's counter value should not have been affected by the throwing invocation

			Assert.Equal(1UL, GetCounterValue(fluid));
		}
	}
}
