using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Generates 128-bit ID values in a distributed way, with no synchronization between generators.
	/// </summary>
	public interface IDistributedId128Generator
	{
		// #TODO: Return UInt128 from CreateId() and Guid from CreateGuid()?

		/// <summary>
		/// Returns a new ID value.
		/// </summary>
		Guid CreateId();
	}
}
