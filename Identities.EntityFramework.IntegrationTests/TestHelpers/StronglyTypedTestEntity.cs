namespace Architect.Identities.EntityFramework.IntegrationTests.TestHelpers
{
	public sealed class StronglyTypedTestEntity
	{
		public TestEntityId Id { get; }
		public string Name { get; }
		public decimal Number { get; }
		public decimal ForeignId { get; }
		public TestEntityId ForeignID { get; } // Deliberate spelling
		public decimal DoesNotHaveIdSuffix { get; }

		public StronglyTypedTestEntity(string name = "TestName", decimal number = 0.1234567890123456789012345678m)
		{
			this.Id = DistributedId.CreateId();

			this.Name = name;
			this.Number = number;
			this.ForeignId = 1234567890123456789012345678m;
			this.ForeignID = 1234567890123456789012345678m;
			this.DoesNotHaveIdSuffix = number;
		}
	}

	public readonly struct TestEntityId : IEquatable<TestEntityId>, IComparable<TestEntityId>
	{
		public decimal Value { get; }

		public TestEntityId(decimal value)
		{
			this.Value = value;
		}
		public override string ToString()
		{
			return this.Value.ToString();
		}

		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		public override bool Equals(object other)
		{
			return other is TestEntityId otherId && this.Equals(otherId);
		}

		public bool Equals(TestEntityId other)
		{
			return this.Value == other.Value;
		}

		public int CompareTo(TestEntityId other)
		{
			return this.Value.CompareTo(other.Value);
		}

		public static bool operator ==(TestEntityId left, TestEntityId right) => left.Equals(right);
		public static bool operator !=(TestEntityId left, TestEntityId right) => !(left == right);
		public static bool operator >(TestEntityId left, TestEntityId right) => left.CompareTo(right) > 0;
		public static bool operator <(TestEntityId left, TestEntityId right) => left.CompareTo(right) < 0;
		public static bool operator >=(TestEntityId left, TestEntityId right) => left.CompareTo(right) >= 0;
		public static bool operator <=(TestEntityId left, TestEntityId right) => left.CompareTo(right) <= 0;

		public static implicit operator TestEntityId(decimal value) => new TestEntityId(value);
		public static implicit operator decimal(TestEntityId id) => id.Value;

		public static implicit operator TestEntityId?(decimal? value) => value is null ? null : new TestEntityId(value.Value);
		public static implicit operator decimal?(TestEntityId? id) => id?.Value;
	}
}
