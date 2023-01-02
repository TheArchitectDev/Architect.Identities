using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace Architect.Identities.EntityFramework.IntegrationTests.TestHelpers
{
	public class TestDbContext : DbContext // Must be public, unsealed for Create method's reflection
	{
		private static ModuleBuilder ModuleBuilder { get; } = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UniqueTestDbContextAssembly"), AssemblyBuilderAccess.Run)
			.DefineDynamicModule("UniqueTestDbContextModule");

		public DbSet<TestEntity> Entities { get; protected set; }
		public DbSet<StronglyTypedTestEntity> StronglyTypedEntities { get; protected set; }
		public Action<ModelBuilder, DbContext> OnModelCreatingAction { get; }

		public static TestDbContext Create(Action<ModelBuilder, DbContext> onModelCreating = null, bool useInMemoryInsteadOfSqlite = false)
		{
			// Must construct a runtime subtype, because otherwise EF caches the result of OnModelCreating

			var typeBuilder = ModuleBuilder.DefineType($"{nameof(TestDbContext)}_{Guid.NewGuid()}",
				TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.Public, typeof(TestDbContext));

			var baseConstructor = typeof(TestDbContext).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
				new[] { typeof(Action<ModelBuilder, DbContext>), typeof(bool) }, modifiers: null);
			var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Action<ModelBuilder, DbContext>), typeof(bool) });
			var ilGenerator = ctorBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			ilGenerator.Emit(OpCodes.Ldarg_2);
			ilGenerator.Emit(OpCodes.Call, baseConstructor);
			ilGenerator.Emit(OpCodes.Ret);

			var type = typeBuilder.CreateType();

			try
			{
				var instance = (TestDbContext)Activator.CreateInstance(type, new object[] { onModelCreating, useInMemoryInsteadOfSqlite, });
				return instance;
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
		}

		protected TestDbContext(Action<ModelBuilder, DbContext> onModelCreating = null, bool useInMemoryInsteadOfSqlite = false)
			: base(useInMemoryInsteadOfSqlite
				  ? new DbContextOptionsBuilder().UseInMemoryDatabase("db").Options
				  : new DbContextOptionsBuilder().UseSqlite("Filename=:memory:").Options)
		{
			this.OnModelCreatingAction = onModelCreating ?? ((modelBuilder, dbContext) => { });

			if (!useInMemoryInsteadOfSqlite)
				this.Database.OpenConnection();

			this.Database.EnsureCreated();
		}

		protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
		{
			base.ConfigureConventions(configurationBuilder);

			configurationBuilder.ConfigureDecimalIdTypes(modelAssemblies: this.GetType().Assembly);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<TestEntity>(entity =>
			{
				entity.Property(e => e.Id);

				entity.Property(e => e.Name);

				entity.Property(e => e.Number);

				entity.Property(e => e.ForeignId)
					.HasColumnName("ForeignId1");

				entity.Property(e => e.ForeignID)
					.HasColumnName("ForeignId2");

				entity.Property(e => e.DoesNotHaveIdSuffix);

				entity.Property(e => e.DoesNotHaveIdSuffixEither)
					.HasConversion(codeValue => (decimal)codeValue, dbValue => (TestEntityId)dbValue);

				entity.HasKey(e => e.Id);
			});

			modelBuilder.Entity<StronglyTypedTestEntity>(entity =>
			{
				entity.Property(e => e.Id);

				entity.Property(e => e.Name);

				entity.Property(e => e.Number);

				entity.Property(e => e.ForeignId)
					.HasColumnName("ForeignId1");

				entity.Property(e => e.ForeignID)
					.HasColumnName("ForeignId2");

				entity.Property(e => e.DoesNotHaveIdSuffix);

				entity.HasKey(e => e.Id);
			});

			this.OnModelCreatingAction(modelBuilder, this);
		}
	}
}
