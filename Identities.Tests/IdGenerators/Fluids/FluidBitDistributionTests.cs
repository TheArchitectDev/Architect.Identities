using Xunit;

namespace Architect.Identities.Tests.IdGenerators.Fluids
{
	public sealed class FluidBitDistributionTests
	{
		[Fact]
		public void Construct_With64TotalBits_ShouldSucceed()
		{
			_ = new FluidBitDistribution(43, 11, 10);
		}
		
		[Fact]
		public void Construct_With63TotalBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidBitDistribution(42, 11, 10));
		}
		
		[Fact]
		public void Construct_With62TotalBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidBitDistribution(41, 11, 10));
		}
		
		[Fact]
		public void Construct_With65TotalBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidBitDistribution(44, 11, 10));
		}
		
		[Fact]
		public void Construct_With64TimestampBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidBitDistribution(64, 0, 0));
		}
		
		[Fact]
		public void Construct_Regularly_ShouldContainGivenValues()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal(43, distribution.TimestampBitCount);
			Assert.Equal(11, distribution.ApplicationInstanceIdBitCount);
			Assert.Equal(10, distribution.CounterBitCount);
		}

		[Fact]
		public void Construct_With16ApplicationInstanceIdBits_ShouldSucceed()
		{
			_ = new FluidBitDistribution(43, 16, 5);
		}

		[Fact]
		public void Construct_WithMoreThan16ApplicationInstanceIdBits_ShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => new FluidBitDistribution(43, 17, 4));
		}

		[Fact]
		public void GetTotalBitCount_Regularly_ShouldReturnExpectedValue()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal(64, distribution.TotalBitCount);
		}
		
		[Fact]
		public void GetTimestampBitCount_Regularly_ShouldReturnExpectedValue()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal(43, distribution.TimestampBitCount);
		}
		
		[Fact]
		public void GetMaxTimestamp_Regularly_ShouldReturnExpectedValue()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal(8796093022207UL, distribution.MaxTimestamp);
		}
		
		[Fact]
		public void GetMaxApplicationInstanceId_Regularly_ShouldReturnExpectedValue()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal((ushort)2047, distribution.MaxApplicationInstanceId);
		}
		
		[Fact]
		public void GetMaxCounterValue_Regularly_ShouldReturnExpectedValue()
		{
			var distribution = new FluidBitDistribution(43, 11, 10);

			Assert.Equal(1023UL, distribution.MaxCounterValue);
		}
	}
}
