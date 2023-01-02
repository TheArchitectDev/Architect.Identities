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
		public void UnconfiguredProperty_WithSqlite_ReturnsIncorrectPrecision()
		{
			using var dbContext = TestDbContext.Create((modelBuilder, dbContext) =>
			{
				modelBuilder.Entity<TestEntity>().Ignore(x => x.Id);
				modelBuilder.Entity<TestEntity>().Property(x => x.Id);
			});

			var entity = new TestEntity(number: 1234567890123456789012345678m);
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.DoesNotHaveIdSuffix, loadedEntity.DoesNotHaveIdSuffix);
			Assert.Equal(65536, GetSignAndScale(loadedEntity.DoesNotHaveIdSuffix));
		}

		[Fact]
		public void StoreWithDecimalIdPrecision_WithInMemory_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create(useInMemoryInsteadOfSqlite: true);

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
				modelBuilder.Entity<TestEntity>(entity => entity.Property(e => e.Id).HasColumnType("DECIMAL(29,1)")));

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			//Assert.Equal(1, GetSignAndScale(loadedEntity.Id)); // Unfortunately, the in-memory provider does not honor this, but at least we know that the flow did not throw
			//Assert.Equal("DECIMAL(29,1)", dbContext.Model.FindEntityType(typeof(TestEntity)).FindProperty(nameof(TestEntity.Id)).GetColumnType()); // Does not work with in-memory provider
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndPrimitiveId_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create();

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			//Assert.Equal(0, GetSignAndScale(loadedEntity.Id)); // Does not retain scale with SQLite
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndCustomStructId_ReturnsExpectedPrecision()
		{
			using var dbContext = TestDbContext.Create();

			var entity = new StronglyTypedTestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.Id, loadedEntity.Id);
			//Assert.Equal(0, GetSignAndScale(loadedEntity.Id)); // Does not retain scale with SQLite
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndPrimitiveId_AffectsSecondaryIdProperties()
		{
			using var dbContext = TestDbContext.Create();

			var entity = new TestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.ForeignId, loadedEntity.ForeignId);
			Assert.Equal(entity.ForeignID, loadedEntity.ForeignID);

			//Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignId)); // Does not retain scale with SQLite
			//Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignID)); // Does not retain scale with SQLite
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqliteAndCustomStructId_AffectsSecondaryIdProperties()
		{
			using var dbContext = TestDbContext.Create();

			var entity = new StronglyTypedTestEntity();
			var loadedEntity = this.SaveAndReload(entity, dbContext);

			Assert.Equal(entity.ForeignId, loadedEntity.ForeignId);
			Assert.Equal(entity.ForeignID, loadedEntity.ForeignID);

			//Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignId)); // Does not retain scale with SQLite
			//Assert.Equal(0, GetSignAndScale(loadedEntity.ForeignID)); // Does not retain scale with SQLite
		}

		[Fact]
		public void StoreDecimalIdsWithCorrectPrecision_WithSqlite_DoesNotAffectNonIdDecimals()
		{
			using var dbContext = TestDbContext.Create();

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

			var loadedEntity = dbContext.Entities.Single();
			return loadedEntity;
		}

		private StronglyTypedTestEntity SaveAndReload(StronglyTypedTestEntity entity, TestDbContext dbContext)
		{
			dbContext.StronglyTypedEntities.Add(entity);
			dbContext.SaveChanges();

			dbContext.Entry(entity).State = EntityState.Detached;

			var loadedEntity = dbContext.StronglyTypedEntities.Single();
			return loadedEntity;
		}

		private static int GetSignAndScale(decimal dec) => Decimal.GetBits(dec)[3]; // Decimal.GetBits() contains the sign-and-scale portion of a decimal
	}
}
