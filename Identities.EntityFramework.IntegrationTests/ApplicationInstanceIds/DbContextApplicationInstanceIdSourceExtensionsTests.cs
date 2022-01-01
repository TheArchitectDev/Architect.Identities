using System.Data.Common;
using System.Reflection;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.EntityFramework.IntegrationTests.ApplicationInstanceIds
{
	public sealed class DbContextApplicationInstanceIdSourceExtensionsTests : IDisposable
	{
		private List<Action<IServiceCollection>> RegistrationActions { get; } = new List<Action<IServiceCollection>>();

		private IHostBuilder HostBuilder { get; } = new	HostBuilder();
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

		public void Dispose()
		{
			this._host?.Dispose();
		}

		[Fact]
		public void UseSqlServerDbContext_Regularly_ShouldUseExpectedTableName()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlServerDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();

			var tableName = renter.GetType().GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(renter);

			Assert.Equal(DbContextApplicationInstanceIdSourceExtensions.DefaultTableName, tableName);
		}

		[Fact]
		public void UseMySqlDbContext_Regularly_ShouldUseExpectedTableName()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseMySqlDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();

			var tableName = renter.GetType().GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(renter);

			Assert.Equal(DbContextApplicationInstanceIdSourceExtensions.DefaultTableName, tableName);
		}

		[Fact]
		public void UseSqliteDbContext_Regularly_ShouldUseExpectedTableName()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();

			var tableName = renter.GetType().GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(renter);

			Assert.Equal(DbContextApplicationInstanceIdSourceExtensions.DefaultTableName, tableName);
		}

		[Fact]
		public void UseStandardSqlDbContext_Regularly_ShouldUseExpectedTableName()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseStandardSqlDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();

			var tableName = renter.GetType().GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(renter);

			Assert.Equal(DbContextApplicationInstanceIdSourceExtensions.DefaultTableName, tableName);
		}

		[Fact]
		public void UseSqliteDbContext_WithTransientDbContext_ShouldUseTransientIApplicationInstanceIdSource()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			IApplicationInstanceIdRenter renter1;
			IApplicationInstanceIdRenter renter2;

			using var scope = this.Host.Services.CreateScope();

			renter1 = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();
			renter2 = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();

			Assert.False(ReferenceEquals(renter1, renter2));
		}

		[Fact]
		public void UseSqliteDbContext_WithScopedDbContext_ShouldUseTransientIApplicationInstanceIdSource()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Scoped);
			});

			IApplicationInstanceIdRenter renter1;
			IApplicationInstanceIdRenter renter2;

			using var scope = this.Host.Services.CreateScope();

			renter1 = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();
			renter2 = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();

			Assert.False(ReferenceEquals(renter1, renter2));
		}

		[Fact]
		public void UseSqliteDbContext_WithAnyDbContextWithUseApplicationInstanceIdSource_ShouldCreateValue()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddPooledDbContextFactory<SqliteDbContext>(factory => factory.UseSqlite(connection));
			});

			var source = this.Host.Services.GetRequiredService<IApplicationInstanceIdSource>();

			Assert.ThrowsAny<Exception>(() => source.ApplicationInstanceId); // Not yet acquired

			this.Host.UseApplicationInstanceIdSource();

			var id = source.ApplicationInstanceId;
			var count = this.ExecuteScalar(connection, $"SELECT COUNT(*) FROM {DbContextApplicationInstanceIdSourceExtensions.DefaultTableName};");

			Assert.Equal(1, id);
			Assert.Equal(1L, count);
		}

		[Fact]
		public void UseSqliteDbContext_WithPooledDbContextFactory_ShouldWorkAsExpected()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddPooledDbContextFactory<SqliteDbContext>(factory => factory.UseSqlite(connection));
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();
			var value = renter.RentId();

			Assert.Equal(1, value);
		}

		[Fact]
		public void UseSqliteDbContext_WithDbContextFactory_ShouldWorkAsExpected()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContextFactory<SqliteDbContext>(factory => factory.UseSqlite(connection));
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();
			var value = renter.RentId();

			Assert.Equal(1, value);
		}

		[Fact]
		public void UseSqliteDbContext_WithScopedDbContext_ShouldWorkAsExpected()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Scoped);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();
			var value = renter.RentId();

			Assert.Equal(1, value);
		}

		[Fact]
		public void UseSqliteDbContext_WithTransientDbContext_ShouldWorkAsExpected()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Scoped);
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();
			var value = renter.RentId();

			Assert.Equal(1, value);
		}

		[Fact]
		public async Task UseSqliteDbContext_WithPooledDbContextFactoryWithUseApplicationInstanceIdSource_ShouldCleanUpOnShutdown()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddPooledDbContextFactory<SqliteDbContext>(factory => factory.UseSqlite(connection));
			});

			this.Host.UseApplicationInstanceIdSource();

			this.Host.Start();
			await this.Host.StopAsync();

			var count = this.ExecuteScalar(connection, $"SELECT COUNT(*) FROM {DbContextApplicationInstanceIdSourceExtensions.DefaultTableName};");

			Assert.Equal(0L, count);
		}

		[Fact]
		public async Task UseSqliteDbContext_WithDbContextFactoryWithUseApplicationInstanceIdSource_ShouldCleanUpOnShutdown()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContextFactory<SqliteDbContext>(factory => factory.UseSqlite(connection));
			});

			this.Host.UseApplicationInstanceIdSource();

			this.Host.Start();
			await this.Host.StopAsync();

			var count = this.ExecuteScalar(connection, $"SELECT COUNT(*) FROM {DbContextApplicationInstanceIdSourceExtensions.DefaultTableName};");

			Assert.Equal(0L, count);
		}

		[Fact]
		public async Task UseSqliteDbContext_WithScopedDbContextWithUseApplicationInstanceIdSource_ShouldCleanUpOnShutdown()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Scoped);
			});

			this.Host.UseApplicationInstanceIdSource();

			this.Host.Start();
			await this.Host.StopAsync();

			var count = this.ExecuteScalar(connection, $"SELECT COUNT(*) FROM {DbContextApplicationInstanceIdSourceExtensions.DefaultTableName};");

			Assert.Equal(0L, count);
		}

		[Fact]
		public async Task UseSqliteDbContext_WithTransientDbContextWithUseApplicationInstanceIdSource_ShouldCleanUpOnShutdown()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Transient);
			});

			this.Host.UseApplicationInstanceIdSource();

			this.Host.Start();
			await this.Host.StopAsync();

			var count = this.ExecuteScalar(connection, $"SELECT COUNT(*) FROM {DbContextApplicationInstanceIdSourceExtensions.DefaultTableName};");

			Assert.Equal(0L, count);
		}

		[Fact]
		public void UseSqliteDbContext_WithScopedDbContext_ShouldUseScopedDbContext()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(factory => factory.UseSqlite(connection), ServiceLifetime.Scoped);
			});

			// By resolving a scoped service outside of any scope, we get a singleton instance that is separate from any scoped instances
			var singletonInstance = this.Host.Services.GetRequiredService<SqliteDbContext>();

			// By disposing it, if the IApplicationInstanceIdRenter were to try to use the singleton instance, it would cause an exception
			singletonInstance.Dispose();

			this.Host.UseApplicationInstanceIdSource();

			var source = this.Host.Services.GetRequiredService<IApplicationInstanceIdSource>();
			var value = source.ApplicationInstanceId;

			Assert.Equal(1, value);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqliteDbContext_WhenResolvedWithNonFixedConnectionAndTransientDbContext_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(context => context.UseSqlite("FileName=:memory:"), ServiceLifetime.Transient); // Creates different connections
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqliteDbContext_WhenResolvedWithNonFixedConnectionAndScopedDbContext_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContext<SqliteDbContext>(context => context.UseSqlite("FileName=:memory:"), ServiceLifetime.Scoped); // Creates different connections
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqliteDbContext_WhenResolvedWithNonFixedConnectionAndDbContextFactory_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddDbContextFactory<SqliteDbContext>(context => context.UseSqlite("DataSource=:memory:")); // Creates different connections
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqliteDbContext_WhenResolvedWithNonFixedConnectionAndPooledDbContextFactory_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqliteDbContext<SqliteDbContext>());
				services.AddPooledDbContextFactory<SqliteDbContext>(context => context.UseSqlite("Data Source=:memory:")); // Creates different connections
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
		}

		private object ExecuteScalar(DbConnection connection, string queryString)
		{
			using var command = connection.CreateCommand();
			command.CommandText = queryString;
			connection.Open();
			return command.ExecuteScalar();
		}
	}
}
