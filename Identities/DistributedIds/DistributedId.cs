using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A <see cref="Guid"/> replacement that provides values that are unique with extremely high probability, in a distributed fashion.
	/// </para>
	/// <para>
	/// <strong>Similarities:</strong> Like .NET's built-in version-4 <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
	/// </para>
	/// <para>
	/// <strong>Benefits:</strong> The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist.
	/// The incremental property generally makes values much more efficient as database/storage keys than random <see cref="Guid"/> values.
	/// </para>
	/// <para>
	/// <strong>Structure:</strong> The values are decimals of exactly 28 digits, with 0 decimal places. In SQL databases, the corresponding type is DECIMAL(28, 0).
	/// In [Azure] SQL Server and MySQL, this takes 13 bytes of storage, making it about 20% more compact than a <see cref="Guid"/>.
	/// </para>
	/// <para>
	/// <strong>Exposure:</strong> Note that the values expose their creation timestamps to some degree. This may be sensitive data in certain contexts.
	/// </para>
	/// <para>
	/// <strong>Rate limit:</strong> The rate limit per process is 128 values generated per millisecond (i.e. 128K per second) on average, with threads sleeping if necessary.
	/// However, about 128K values can be burst generated instantaneously, with the burst capacity recovering quickly during non-exhaustive use.
	/// </para>
	/// <para>
	/// <strong>Collisions:</strong> Collisions between generated values are extremely unlikely, although worldwide uniqueness is not a guarantee. View the README for more information.
	/// </para>
	/// <para>
	/// <strong>Sorting:</strong> The <see cref="Decimal"/> type has broad support and predictable implementations, unlike the <see cref="Guid"/> type.
	/// For example, SQL Server sorts <see cref="Guid"/> values in an unfavorable way, treating some of the middle bytes as the most significant.
	/// MySQL has no <see cref="Guid"/> type, making manual queries cumbersome.
	/// Decimals avoid such issues.
	/// </para>
	/// </summary>
	public static class DistributedId
	{
		/// <summary>
		/// <para>
		/// Returns a new ID value of exactly 28 decimal digits, with no decimal places.
		/// </para>
		/// <para>
		/// View the class summary or the README for an extensive description of the ID's properties.
		/// </para>
		/// <para>
		/// The ID generator can be controlled by constructing a new <see cref="DistributedIdGeneratorScope"/> in a using statement.
		/// </para>
		/// </summary>
		public static decimal CreateId()
		{
			var id = DistributedIdGeneratorScope.CurrentGenerator.CreateId();
			return id;
		}
	}
}
