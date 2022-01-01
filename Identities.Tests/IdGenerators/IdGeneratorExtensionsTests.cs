using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators
{
	public sealed class IdGeneratorExtensionsTests
	{
		[Fact]
		public void AddIdGenerator_ResolvedWithoutApplicationInstanceIdSource_ShouldThrow()
		{
			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services =>
			{
				services
					.AddIdGenerator(generator => generator.UseFluid());
			});
			using var host = hostBuilder.Build();
			Assert.ThrowsAny<InvalidOperationException>(() => host.Services.GetRequiredService<IIdGenerator>());
		}
		
		[Fact]
		public void AddIdGenerator_ResolvedWithRequiredDependenciesRegistered_ShouldSucceed()
		{
			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services =>
			{
				services
					.AddApplicationInstanceIdSource(source => source.UseFixedSource(1))
					.AddIdGenerator(generator => generator.UseFluid());
			});
			using var host = hostBuilder.Build();
			host.Services.GetRequiredService<IIdGenerator>();
		}
	}
}
