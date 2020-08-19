namespace Architect.Identities.EntityFramework.IntegrationTests.TestHelpers
{
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
}
