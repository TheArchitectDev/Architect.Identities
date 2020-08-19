using System;
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
		public Action<ModelBuilder, DbContext> OnModelCreatingAction { get; }

		public static TestDbContext Create(Action<ModelBuilder, DbContext> onModelCreating = null)
		{
			// Must construct a runtime subtype, because otherwise EF caches the result of OnModelCreating

			var typeBuilder = ModuleBuilder.DefineType($"{nameof(TestDbContext)}_{Guid.NewGuid()}",
				TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.Public, typeof(TestDbContext));

			var baseConstructor = typeof(TestDbContext).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, new[] { typeof(Action<ModelBuilder, DbContext>) }, modifiers: null);
			var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Action<ModelBuilder, DbContext>) });
			var ilGenerator = ctorBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			ilGenerator.Emit(OpCodes.Call, baseConstructor);
			ilGenerator.Emit(OpCodes.Ret);

			var type = typeBuilder.CreateType();

			var instance = (TestDbContext)Activator.CreateInstance(type, new[] { onModelCreating });
			return instance;
		}

		protected TestDbContext(Action<ModelBuilder, DbContext> onModelCreating = null)
			: base(new DbContextOptionsBuilder().UseSqlite("Filename=:memory:").Options)
		{
			this.OnModelCreatingAction = onModelCreating ?? ((modelBuilder, dbContext) => { });

			this.Database.OpenConnection();
			this.Database.EnsureCreated();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<TestEntity>(entity =>
			{
				entity.Property(e => e.Id)
					.ValueGeneratedNever();

				entity.Property(e => e.Name);

				entity.Property(e => e.Number);

				entity.HasKey(e => e.Id);
			});

			this.OnModelCreatingAction(modelBuilder, this);
		}
	}
}
