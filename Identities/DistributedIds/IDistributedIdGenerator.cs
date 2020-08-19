// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Generates decimal ID values in a distributed way, with no synchronization between generators.
	/// </summary>
	public interface IDistributedIdGenerator
	{
		/// <summary>
		/// Returns a new ID value.
		/// </summary>
		decimal CreateId();
	}
}
