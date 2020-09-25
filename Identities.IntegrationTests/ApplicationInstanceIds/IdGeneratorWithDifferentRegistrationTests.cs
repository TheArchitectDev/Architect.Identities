using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.IntegrationTests.ApplicationInstanceIds
{
	public sealed class IdGeneratorWithDifferentRegistrationTests : IDisposable
	{
		private List<Action<IServiceCollection>> RegistrationActions { get; } = new List<Action<IServiceCollection>>();

		private IHostBuilder HostBuilder { get; }
		private IHost Host
		{
			get
			{
				if (this._host is null)
				{
					foreach (var action in this.RegistrationActions)
						this.HostBuilder.ConfigureServices(action);

					this._host = this.HostBuilder.Build();
				}
				return this._host;
			}
		}
		private IHost _host;

		public IdGeneratorWithDifferentRegistrationTests()
		{
			this.HostBuilder = new HostBuilder();

			this.HostBuilder.ConfigureServices(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlite(ThrowDatabaseNotAvailable));
				services.AddIdGenerator(generator => generator.UseFluid());
			});
		}

		public void Dispose()
		{
			this.Host.Dispose();
		}

		private static DbConnection ThrowDatabaseNotAvailable()
		{
			throw new NotSupportedException("The database is not available in a test run.");
		}

		/// <summary>
		/// With ambient context:
		/// Because we never invoked <see cref="IdGeneratorExtensions.UseIdGenerator(IServiceProvider)"/> in a test run, we will get the default test scope.
		/// </summary>
		[Fact]
		public void GetIdGeneratorCurrent_WithRegisteredDatabaseThatIsUnavailable_ShouldSucceed()
		{
			_ = IdGenerator.Current;
		}

		/// <summary>
		/// With dependency injection:
		/// Because we explicitly resolve the <see cref="IIdGenerator"/>, the ID generator that our application registers will try to access the database.
		/// </summary>
		[Fact]
		public void ResolveIdGenerator_WithRegisteredDatabaseThatIsUnavailable_ShouldThrow()
		{
			Assert.Throws<Exception>(() => this.Host.Services.GetRequiredService<IIdGenerator>());
		}

		/// <summary>
		/// With dependency injection:
		/// Because we explicitly resolve the <see cref="IIdGenerator"/>, the ID generator that our application registers will try to access the database.
		/// However, as with any such DI, we can avoid the problem by registering a different implementation.
		/// </summary>
		[Fact]
		public void ResolveIdGenerator_WithRegisteredDatabaseThatIsAvailable_ShouldSucceed()
		{
			this.RegistrationActions.Add(this.RegisterFixedApplicationInstanceIdSource);

			_ = this.Host.Services.GetRequiredService<IIdGenerator>();
		}

		[Fact]
		public void ResolveIdGenerator_WithRegisteredScopedFactory_ShouldSucceed()
		{
			this.RegistrationActions.Add(services => services.AddScoped<FixedIdSource>());
			this.RegistrationActions.Add(services => services.AddApplicationInstanceIdSource(source =>
				source.UseFixedSource(serviceProvider => serviceProvider.GetRequiredService<FixedIdSource>().GetId)));

			_ = this.Host.Services.GetRequiredService<IIdGenerator>();
		}

		private void RegisterFixedApplicationInstanceIdSource(IServiceCollection services)
		{
			services.AddApplicationInstanceIdSource(source => source.UseFixedSource(applicationInstanceId: 1));
		}

		private sealed class FixedIdSource
		{
			public ushort GetId => 1;
		}
	}
}
