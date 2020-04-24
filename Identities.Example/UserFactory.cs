using System;

namespace Architect.Identities.Example
{
	/// <summary>
	/// Used to create user entities.
	/// Required because the entity needs an ID, which is based on the <see cref="IIdGenerator"/> dependency.
	/// </summary>
	internal sealed class UserFactory
	{
		private IIdGenerator IdGenerator { get; }

		public UserFactory(IIdGenerator idGenerator)
		{
			this.IdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		}

		public UserEntityWithInjection CreateUser(string userName, string fullName)
		{
			var id = this.IdGenerator.CreateId();
			var user = new UserEntityWithInjection(id, userName, fullName);
			return user;
		}
	}
}
