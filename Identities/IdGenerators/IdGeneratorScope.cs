using System;
using Architect.AmbientContexts;
using Architect.Identities.Helpers;

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
	/// A default scope can be registered on startup through <see cref="IdGeneratorExtensions.UseIdGenerator(IServiceProvider)"/>.
	/// In test runs, a default scope is available without the need for any registrations.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="IdGeneratorScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public sealed class IdGeneratorScope : AmbientScope<IdGeneratorScope>
	{
		/// <summary>
		/// Returns the currently accessible <see cref="IdGeneratorScope"/>.
		/// The scope is configured from the outside, such as from startup.
		/// </summary>
		internal static IdGeneratorScope Current => GetAmbientScope() ??
			TestInstance ??
			throw new InvalidOperationException($"{nameof(IdGeneratorScope)} was not configured. Call {nameof(IdGeneratorExtensions.UseIdGenerator)}() on startup.");

		/// <summary>
		/// <para>
		/// Returns the current ambient <see cref="IIdGenerator"/>, or throws if none is registered.
		/// </para>
		/// <para>
		/// The ID generator can be controlled by constructing a new <see cref="IdGeneratorScope"/> in a using statement.
		/// A default can be registered using <see cref="IdGeneratorExtensions.UseIdGenerator(IServiceProvider)"/>.
		/// </para>
		/// </summary>
		internal static IIdGenerator CurrentGenerator => Current.IdGenerator;

		/// <summary>
		/// Returns an instance if the code is executing in a test run.
		/// </summary>
		private static IdGeneratorScope? TestInstance => TestInstanceValue ??= TestDetector.IsTestRun
			? new IdGeneratorScope(new FluidIdGenerator(isProduction: false, FluidIdGenerator.GetUtcNow, applicationInstanceId: 1), AmbientScopeOption.NoNesting)
			: null;
		private static IdGeneratorScope? TestInstanceValue;

		/// <summary>
		/// The registered ID generator.
		/// </summary>
		internal IIdGenerator IdGenerator { get; }

		/// <summary>
		/// Establishes the given ID generator as the ambient one until the scope is disposed.
		/// </summary>
		public IdGeneratorScope(IIdGenerator idGenerator)
			: this(idGenerator, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		/// <summary>
		/// Private constructor.
		/// Does not activate this instance.
		/// </summary>
		private IdGeneratorScope(IIdGenerator idGenerator, AmbientScopeOption ambientScopeOption)
			: base(ambientScopeOption)
		{
			this.IdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		}

		protected override void DisposeImplementation()
		{
			// Nothing to dispose
		}

		/// <summary>
		/// Sets the ubiquitous default scope, overwriting and disposing the previous one if necessary.
		/// </summary>
		/// <param name="newDefaultIdGenerator">The ID generator to register. May be null to unregister.</param>
		internal static void SetDefaultValue(IIdGenerator newDefaultIdGenerator)
		{
			var newDefaultScope = newDefaultIdGenerator is null
				? null
				: new IdGeneratorScope(newDefaultIdGenerator, AmbientScopeOption.NoNesting);
			SetDefaultScope(newDefaultScope);
		}
	}
}
