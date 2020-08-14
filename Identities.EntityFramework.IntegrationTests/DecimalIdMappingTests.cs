using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Architect.Identities;
using Architect.Identities.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Architect.Identities.EntityFramework.IntegrationTests
{
	public class DecimalIdMappingTests
	{
		/// <summary>
		/// Not a requirement, but a baseline for our other tests.
		/// Can be changed or deleted if the situation changes.
		/// </summary>
		[Fact]
		public void NoMapping_WithSqlite_ReturnsIncorrectPrecision()
		{
			using var dbContext = TestDbContext.Create();

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(65536, GetSignAndScale(loadedEntity.Id));
		}

		[Fact]
		public void StoreWithDecimalIdPrecision_WithSqlite_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Id).StoreWithDecimalIdPrecision(dbContext)));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
		}

		/// <summary>
		/// This should work too, since the precision is being explicitly set. The per-property method should not have any naming requirements.
		/// </summary>
		[Fact]
		public void StoreWithDecimalIdPrecision_WithNonIdProperty_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.DoesNotHaveIdSuffix).StoreWithDecimalIdPrecision(dbContext)));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.DoesNotHaveIdSuffix, loadedEntity.DoesNotHaveIdSuffix);
			Assert.Equal(0, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffix));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqlite_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqlite_AffectsOtherIdProperties()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.ForeignId, loadedEntity.ForeignId);
			Assert.Equal(entity.ForeignID, loadedEntity.ForeignID);

			Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignId));
			Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignID));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqlite_DoesNotAffectNonIdDecimals()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Number, loadedEntity.Number);
			Assert.NotEqual(0, GetSignAndScale(loadedEntity.Number));
		}

		private TestEntity SaveAndReload(TestEntity entity, TestDbContext dbContext)
		{
			dbContext.Entities.Add(entity);
			dbContext.SaveChanges();

			dbContext.Entry(entity).State = EntityState.Detached;

			var loadedEntity = dbContext.Entities.ToList()[0];
			return loadedEntity;
		}

		private static int GetSignAndScale(decimal dec) => Decimal.GetBits(dec)[3]; // Decimal.GetBits() contains the sign-and-scale portion of a decimal

		public sealed class TestEntity
		{
			public decimal Id { get; }
			public string Name { get; }
			public decimal Number { get; }
			public decimal ForeignId { get; }
			public decimal ForeignID { get; } // Deliberate spelling
			public decimal DoesNotHaveIdSuffix { get; }

			public TestEntity(string name = "TestName", decimal number = 0.1234567890123456789012345678m)
			{
				this.Id = DistributedId.CreateId();

				this.Name = name;
				this.Number = number;
				this.ForeignId = 1234567890123456789012345678m;
				this.ForeignID = 1234567890123456789012345678m;
				this.DoesNotHaveIdSuffix = 1234567890123456789012345678m;
			}
		}

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
}
