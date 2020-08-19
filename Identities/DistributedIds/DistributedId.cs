using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A <see cref="Guid"/> replacement that provides values that are unique with extremely high probability, in a distributed fashion.
	/// </para>
	/// <para>
	/// Like <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
	/// </para>
	/// <para>
	/// The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist, as DECIMAL(28, 0).
	/// </para>
	/// <para>
	/// Note that the values expose their creation timestamps. This may be sensitive data in some contexts.
	/// </para>
	/// <para>
	/// The incremental property makes values much more efficient for use as primary keys in databases than random <see cref="Guid"/> values.
	/// </para>
	/// <para>
	/// The values are decimals of up to 28 digits, with 0 decimal places. In SQL databases, the corresponding type is DECIMAL(28, 0).
	/// In [Azure] SQL Server and MySQL, this takes 13 bytes of storage, making it about 20% more compact than a <see cref="Guid"/>.
	/// </para>
	/// <para>
	/// The <see cref="decimal"/> type has broad support and predictable implementations, unlike the <see cref="Guid"/> type.
	/// For example, SQL Server sorts <see cref="Guid"/> values in an unfavorable way, treating some of the middle bytes as the most significant.
	/// MySQL has no <see cref="Guid"/> type, making manual queries cumbersome.
	/// Decimals avoid such issues.
	/// </para>
	/// <para>
	/// There is a rate limit of 128 values generated per millisecond (i.e. 128K per second) on average, with threads sleeping if necessary.
	/// </para>
	/// <para>
	/// Collisions between generated values are extremely unlikely. Visit the GitHub page for more information.
	/// </para>
	/// </summary>
	public static class DistributedId
	{
		/// <summary>
		/// <para>
		/// Returns a new ID value of up to 28 decimal digits, with no decimal places.
		/// </para>
		/// <para>
		/// Like <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
		/// </para>
		/// <para>
		/// The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist, as DECIMAL(28, 0).
		/// </para>
		/// <para>
		/// Note that the values expose their creation timestamps. This may be sensitive data in some contexts.
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
