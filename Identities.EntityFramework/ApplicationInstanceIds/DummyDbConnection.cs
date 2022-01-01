using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// A dummy <see cref="DbConnection"/> type that may throw on use.
	/// </summary>
	public sealed class DummyDbConnection : DbConnection
	{
		public override string ConnectionString { get; [param: AllowNull] set; } = null!;
		public override string Database { get; } = null!;
		public override string DataSource { get; } = null!;
		public override string ServerVersion { get; } = null!;
		public override ConnectionState State { get; }

		public override void ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			throw new NotImplementedException();
		}

		public override void Open()
		{
			throw new NotImplementedException();
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			throw new NotImplementedException();
		}

		protected override DbCommand CreateDbCommand()
		{
			throw new NotImplementedException();
		}
	}
}
