using Architect.Identities.EntityFramework.IntegrationTests.TestHelpers;
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
		public void StoreWithDecimalIdPrecision_WithUnsuitableProperty_Throws()
		{
			Assert.Throws<ArgumentException>(() => TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Name).StoreWithDecimalIdPrecision(dbContext))));
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

		[Fact]
		public void StoreWithDecimalIdPrecision_WithInMemory_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create(useInMemoryInsteadOfSqlite: true, onModelCreating: (modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Id).StoreWithDecimalIdPrecision(dbContext)));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
			//Assert.Equal("DECIMAL(28,0)", dbContext.Model.FindEntityType(typeof(TestEntity)).FindProperty(nameof(TestEntity.Id)).GetColumnType()); // Does not work with in-memory provider
		}

		[Fact]
		public void StoreWithDecimalIdPrecision_WithInMemoryAndExplicitColumnType_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create(useInMemoryInsteadOfSqlite: true, onModelCreating: (modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Id).StoreWithDecimalIdPrecision(dbContext, columnType: "DECIMAL(28,0)")));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
			//Assert.Equal("DECIMAL(28,0)", dbContext.Model.FindEntityType(typeof(TestEntity)).FindProperty(nameof(TestEntity.Id)).GetColumnType()); // Does not work with in-memory provider
		}

		[Fact]
		public void StoreWithDecimalIdPrecision_WithInMemoryAndDifferentColumnType_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create(useInMemoryInsteadOfSqlite: true, onModelCreating: (modelBuilder, dbContext) =>
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Id).StoreWithDecimalIdPrecision(dbContext, columnType: "DECIMAL(29,1)")));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			//Assert.Equal(1, GetSignAndScale(loadedEntity.Id)); // Unfortunately, the in-memory provider does not honor this, but at least we know that the flow did not throw
			//Assert.Equal("DECIMAL(29,1)", dbContext.Model.FindEntityType(typeof(TestEntity)).FindProperty(nameof(TestEntity.Id)).GetColumnType()); // Does not work with in-memory provider
		}

		/// <summary>
		/// This should work too, since the precision is being explicitly set. The per-property method should not have any naming requirements.
		/// </summary>
		[Fact]
		public void StoreWithDecimalIdPrecision_WithNonIdProperty_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) => modelBuilder
				.Entity<TestEntity>(entity => entity.Property(e => e.DoesNotHaveIdSuffix).StoreWithDecimalIdPrecision(dbContext))
				.Entity<TestEntity>(entity => entity.Property(e => e.DoesNotHaveIdSuffixEither).StoreWithDecimalIdPrecision(dbContext)));

			var entity = new TestEntity(number: 1234567890123456789012345678m);
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.DoesNotHaveIdSuffix, loadedEntity.DoesNotHaveIdSuffix);
			Assert.Equal(entity.DoesNotHaveIdSuffixEither, loadedEntity.DoesNotHaveIdSuffixEither);
			Assert.Equal(0, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffix));
			Assert.Equal(0, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffixEither));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndPrimitiveId_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndCustomStructId_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new StronglyTypedTestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			Assert.Equal(0, GetSignAndScale(loadedEntity.Id));
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndPrimitiveId_AffectsSecondaryIdProperties()
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
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndCustomStructId_AffectsSecondaryIdProperties()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
				modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext));

			var entity = new StronglyTypedTestEntity();
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
			Assert.Equal(entity.DoesNotHaveIdSuffix, loadedEntity.DoesNotHaveIdSuffix);
			Assert.Equal(entity.DoesNotHaveIdSuffixEither, loadedEntity.DoesNotHaveIdSuffixEither);
			Assert.NotEqual(0, GetSignAndScale(loadedEntity.Number));
			Assert.NotEqual(0, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffix));
			Assert.NotEqual(0, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffixEither));
		}

		private TestEntity SaveAndReload(TestEntity entity, TestDbContext dbContext)
		{
			dbContext.Entities.Add(entity);
			dbContext.SaveChanges();

			dbContext.Entry(entity).State = EntityState.Detached;

			var loadedEntity = dbContext.Entities.ToList()[0];
			return loadedEntity;
		}

		private StronglyTypedTestEntity SaveAndReload(StronglyTypedTestEntity entity, TestDbContext dbContext)
		{
			dbContext.StronglyTypedEntities.Add(entity);
			dbContext.SaveChanges();

			dbContext.Entry(entity).State = EntityState.Detached;

			var loadedEntity = dbContext.StronglyTypedEntities.ToList()[0];
			return loadedEntity;
		}

		private static int GetSignAndScale(decimal dec) => Decimal.GetBits(dec)[3]; // Decimal.GetBits() contains the sign-and-scale portion of a decimal
	}
}
