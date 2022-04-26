namespace Architect.Identities.Example
{
	/// <summary>
	/// <para>
	/// This implementation of a user entity determines its own ID, by depending on an ID generator "plucked out of thin air".
	/// </para>
	/// <para>
	/// The entity remains easy-to-create, as other types without access to services/dependencies remain able to create it.
	/// Also, the entity remains responsible for choosing its ID generation strategy.
	/// </para>
	/// <para>
	/// Inversion of Control (IoC) remains possible thanks to the Ambient Context pattern.
	/// A test method that wants to see ID "0000000000000000000000000001" can simply run the code within a block of "using (new DistributedIdGeneratorScope(new IncrementalDistributedIdGenerator()))".
	/// </para>
	/// </summary>
	public sealed class UserEntityWithAmbientContext
	{
		public override string ToString() => $"{{User easily created with 'new': UserName={{{this.UserName}}} FullName={{{this.FullName}}} Id={this.Id}}}";

		/// <summary>
		/// Guarantueed to be unique within the Bounded Context, regardless which replica of which application on which server created it.
		/// </summary>
		public decimal Id { get; }

		public string UserName { get; }
		public string FullName { get; private set; }

		/// <summary>
		/// Constructs a new instance representing the given data.
		/// </summary>
		public UserEntityWithAmbientContext(string userName, string fullName)
		{
			// DistributedId.CreateId() uses the ambient DistributedIdGeneratorScope.CurrentGenerator to generate a new ID
			this.Id = DistributedId.CreateId();

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
