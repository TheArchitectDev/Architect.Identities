using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.IntegrationTests.ApplicationInstanceIds.Implementations
{
	public sealed class SqlServerApplicationInstanceIdRenterExtensionsTests
	{
		private List<Action<IServiceCollection>> RegistrationActions { get; } = new List<Action<IServiceCollection>>();

		private IHostBuilder HostBuilder { get; } = new HostBuilder();
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
		
		private DbConnection Connection { get; } = new SQLiteConnection("Filename=:memory:");

		[Fact]
		public void UseSqlServer_Regularly_ShouldUseExpectedTableName()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlServer(() => this.Connection));
			});

			var renter = this.Host.Services.GetRequiredService<IApplicationInstanceIdRenter>();

			var tableName = renter.GetType().GetProperty("TableName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(renter);

			Assert.Equal(SqlServerApplicationInstanceIdRenter.DefaultTableName, tableName);
		}
	}
}
