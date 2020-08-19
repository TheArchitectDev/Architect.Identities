using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Provides access to an ID generator through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// This type provides a lightweight Inversion of Control (IoC) mechanism.
	/// The mechanism optimizes accessiblity (through a static property) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.
	/// </para>
	/// <para>
	/// A default scope can be registered on startup through <see cref="IdGeneratorExtensions.UseIdGeneratorScope(IServiceProvider)"/>.
	/// In test runs, a default scope is available without the need for any registrations.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="IdGeneratorScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public static class IdGenerator
	{
		/// <summary>
		/// <para>
		/// Returns the current ambient <see cref="IIdGenerator"/>, or throws if none is registered.
		/// </para>
		/// <para>
		/// The ID generator can be controlled by constructing a new <see cref="IdGeneratorScope"/> in a using statement.
		/// A default can be registered using <see cref="IdGeneratorExtensions.UseIdGeneratorScope(IServiceProvider)"/>.
		/// </para>
		/// </summary>
		public static IIdGenerator Current => IdGeneratorScope.CurrentGenerator;
	}
}
