using System;

namespace Architect.Identities.Example
{
	/// <summary>
	/// <para>
	/// This implementation of a user entity determines its own ID, by depending on an ID generator "plucked out of thin air".
	/// </para>
	/// <para>
	/// The upside is that the entity remains easy-to-create, as other types without access to services/dependencies remain able to create it.
	/// Also, the entity remains responsible for choosing its ID generation strategy (e.g. "the application's regular ID generator").
	/// </para>
	/// <para>
	/// The downside is that the dependency has become an implementation detail, no longer visible from its API nor checked at compile-time.
	/// </para>
	/// <para>
	/// As it happens, the downside is mitigated in a scenario like this: a cross-cutting concern with a trivial, ubiquitous implementation.
	/// All the entities in the application follow the ID generation strategy configured on startup, allowing it to be an implicit understanding.
	/// In unit tests, there is a simple default implementation that is available without any manual registration.
	/// For the occasional unit test that needs it, Inversion of Control (IoC) is maintained thanks to the Ambient Context pattern.
	/// </para>
	/// </summary>
	public sealed class UserEntityWithAmbientContext
	{
		public override string ToString() => $"{{User easily created with 'new': UserName={{{this.UserName}}} FullName={{{this.FullName}}} Id={this.Id}}}";

		/// <summary>
		/// Guarantueed to be unique within the Bounded Context, regardless which instance of which application on which server created it.
		/// </summary>
		public long Id { get; }

		public string UserName { get; }
		public string FullName { get; private set; }

		/// <summary>
		/// Constructs a new instance representing the given data.
		/// </summary>
		public UserEntityWithAmbientContext(string userName, string fullName)
		{
			// Use the currently registered IIdGenerator to obtain a new ID
			this.Id = IdGenerator.Current.CreateId();

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
