using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.ApplicationInstanceIds
{
	public sealed class ApplicationInstanceIdSourceFactoryTests : IDisposable
	{
		private IApplicationInstanceIdSource MockSource { get; } = new MockApplicationInstanceIdSource();

		/// <summary>
		/// Not started, for performance.
		/// </summary>
		private IHost Host { get; }
		private IHostApplicationLifetime ApplicationLifetime { get; }

		public ApplicationInstanceIdSourceFactoryTests()
		{
			var hostBuilder = new HostBuilder();
			this.Host = hostBuilder.Build();
			this.ApplicationLifetime = this.Host.Services.GetRequiredService<IHostApplicationLifetime>();
		}

		public void Dispose()
		{
			this.Host.Dispose();
		}

		[Fact]
		public void Create_Regularly_ShouldResolveSource()
		{
			var didResolveSource = false;

			ApplicationInstanceIdSourceFactory.Create(this.ApplicationLifetime, () =>
			{
				didResolveSource = true;
				return this.MockSource;
			});

			Assert.True(didResolveSource);
		}

		[Fact]
		public void Create_WithUnstartedApplication_ShouldResolveLazy()
		{
			ApplicationInstanceIdSourceFactory.Create(this.ApplicationLifetime, () => this.MockSource);

			Assert.True(this.MockSource.ContextUniqueApplicationInstanceId.IsValueCreated);
		}

		[Fact]
		public void Create_WithStartedApplication_ShouldNotResolveLazy()
		{
			this.Host.Start();

			ApplicationInstanceIdSourceFactory.Create(this.ApplicationLifetime, () => this.MockSource);

			Assert.False(this.MockSource.ContextUniqueApplicationInstanceId.IsValueCreated);
		}

		private sealed class MockApplicationInstanceIdSource : IApplicationInstanceIdSource
		{
			public Lazy<ushort> ContextUniqueApplicationInstanceId { get; } = new Lazy<ushort>(() => 1);
		}
	}
}
