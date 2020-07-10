using System;

namespace Architect.Identities.Example
{
	/// <summary>
	/// <para>
	/// This implementation of a user entity has its ID value injected through the constructor.
	/// </para>
	/// <para>
	/// The upside is that this approach keeps things very straightforward.
	/// </para>
	/// <para>
	/// The downside is that, to create the ID value, some service is usually required, to access the registered <see cref="IIdGenerator"/>.
	/// This makes the entity hard-to-create: we rely on some sort of factory.
	/// Also, the knowledge of what type of ID generation to use (e.g. "the application's regular ID generator") is duplicated among callers, instead of being part of the entity.
	/// </para>
	/// </summary>
	public sealed class UserEntityWithInjection
	{
		public override string ToString() => $"{{User created with DI: UserName={{{this.UserName}}} FullName={{{this.FullName}}} Id={this.Id}}}";

		public long Id { get; }

		public string UserName { get; }
		public string FullName { get; private set; }

		/// <summary>
		/// Constructs a new instance representing the given data.
		/// </summary>
		public UserEntityWithInjection(long id, string userName, string fullName)
		{
			this.Id = id;
			this.UserName = userName ?? throw new ArgumentNullException(nameof(userName));
			this.FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
		}

		/// <summary>
		/// The domain operation of changing the user's full name.
		/// </summary>
		public void ChangeFullName(string fullName)
		{
			this.FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
		}
	}
}
