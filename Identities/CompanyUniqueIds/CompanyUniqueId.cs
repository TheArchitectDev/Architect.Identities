﻿using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A <see cref="Guid"/> replacement that provides values that are unique company-wide.
	/// </para>
	/// <para>
	/// Like <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
	/// </para>
	/// <para>
	/// The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist, as DECIMAL(28, 0).
	/// </para>
	/// <para>
	/// Note that the values expose their creation timestamp. This may be sensitive data in some contexts.
	/// </para>
	/// <para>
	/// The incremental property makes values much more efficient for use as primary keys in databases than random <see cref="Guid"/> values.
	/// (There are acceptable minor fluctuations of the incremental property, caused by distributed systems, clock rewinds, and (with extremely low probability) bulk operations.)
	/// </para>
	/// <para>
	/// The values are decimals of up to 28 digits, with 0 decimal places. In many SQL databases, the corresponding type is DECIMAL(28, 0).
	/// In [Azure] SQL Server and MySQL, this takes 13 bytes of storage, making it about 20% more compact than a <see cref="Guid"/>.
	/// </para>
	/// <para>
	/// The decimal type has broad support and predictable implementations, unlike the <see cref="Guid"/> type.
	/// For example, SQL Server sorts <see cref="Guid"/> values in an unfavorable way, treating some of the middle bytes as the most significant.
	/// MySQL has no <see cref="Guid"/> type, making manual queries cumbersome.
	/// Decimals avoid such issues.
	/// </para>
	/// <para>
	/// There is a rate limit of 1000 values generated per millisecond (i.e. 1M per second), with threads sleeping to maintain the limit.
	/// </para>
	/// <para>
	/// Collisions between generated values are extremely unlikely. They may happen worldwide, but they are not expected within a single organization.
	/// If 24K values were generated in a single millisecond, the probability of a collision would be less than 1/1M, or 1 collision per 24 billion values generated.
	/// Realistically, the values will be spread out across many more milliseconds, making the probability drastically lower.
	/// </para>
	/// </summary>
	public static class CompanyUniqueId
	{
		private static CompanyUniqueIdGenerator Generator { get; } = new CompanyUniqueIdGenerator();

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
		/// Note that the values expose their creation timestamp. This may be sensitive data in some contexts.
		/// </para>
		/// </summary>
		public static decimal CreateId()
		{
			return Generator.CreateId();
		}
	}
}
