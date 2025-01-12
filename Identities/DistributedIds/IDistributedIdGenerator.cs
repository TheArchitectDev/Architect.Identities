#pragma warning disable IDE0130 // Namespace does not match folder structure
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
