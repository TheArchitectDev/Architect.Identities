using System.Data;
using System.Data.Common;

namespace Architect.Identities.IntegrationTests.TestHelpers
{
	/// <summary>
	/// Helps pretend we have pooling, accessing a temporary, in-memory database while pretending to dispose connections in between operations.
	/// </summary>
	internal sealed class UndisposableDbConnection : DbConnection
	{
		private DbConnection Connection { get; }

		public override string ConnectionString
		{
			get => this.Connection.ConnectionString;
			set => this.Connection.ConnectionString = value;
		}
		public override string Database => this.Connection.Database;
		public override string DataSource => this.Connection.DataSource;
		public override string ServerVersion => this.Connection.ServerVersion;
		public override ConnectionState State => this.Connection.State;

		private bool MayDispose { get; set; }

		public UndisposableDbConnection(DbConnection connection)
		{
			this.Connection = connection;
		}

		protected override void Dispose(bool disposing)
		{
			if (!this.MayDispose) return;
			base.Dispose(disposing);
			this.Connection.Dispose();
		}

		/// <summary>
		/// Actually disposes this object and the underlying DbConnection object.
		/// </summary>
		public void TrulyDispose()
		{
			this.MayDispose = true;
			this.Dispose();
		}

		public override void ChangeDatabase(string databaseName) => this.Connection.ChangeDatabase(databaseName);
		public override void Close()
		{
			if (!this.MayDispose) return;
			this.Connection.Close();
		}
		public override void Open()
		{
			if (this.Connection.State == ConnectionState.Open) return;
			this.Connection.Open();
		}
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => this.Connection.BeginTransaction(isolationLevel);
		protected override DbCommand CreateDbCommand() => this.Connection.CreateCommand();
	}
}
