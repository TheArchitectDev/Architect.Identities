using System;
using Architect.AmbientContexts;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Provides access to a public identity converter through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// This type provides a lightweight Inversion of Control (IoC) mechanism.
	/// The mechanism optimizes accessiblity (through a static property) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.
	/// </para>
	/// <para>
	/// A default scope can be registered on startup through <see cref="PublicIdentityExtensions.UsePublicIdentityScope(IServiceProvider)"/>.
	/// In test runs, a default scope is available without the need for any registrations.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="PublicIdentityScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public sealed class PublicIdentityScope : AmbientScope<PublicIdentityScope>
	{
		/// <summary>
		/// Returns the currently accessible <see cref="PublicIdentityScope"/>.
		/// The scope is configured from the outside, such as from startup.
		/// </summary>
		public static PublicIdentityScope Current => GetAmbientScope() ??
			TestInstance ??
			throw new InvalidOperationException($"{nameof(PublicIdentityScope)} was not configured. Call {nameof(PublicIdentityExtensions)}.{nameof(PublicIdentityExtensions.UsePublicIdentityScope)} on startup.");

		/// <summary>
		/// Returns an instance if the code is executing in a test run.
		/// </summary>
		private static PublicIdentityScope? TestInstance => TestInstanceValue ?? (TestInstanceValue = TestDetector.IsTestRun
			? new PublicIdentityScope(new AesPublicIdentityConverter(new byte[32]), AmbientScopeOption.NoNesting)
			: null);
		private static PublicIdentityScope? TestInstanceValue;

		/// <summary>
		/// The registered public identity converter.
		/// </summary>
		public IPublicIdentityConverter Converter { get; }

		/// <summary>
		/// Establishes the given public identity converter as the ambient one until the scope is disposed.
		/// </summary>
		public PublicIdentityScope(IPublicIdentityConverter converter)
			: this(converter, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		/// <summary>
		/// Private constructor.
		/// Does not activate this instance.
		/// </summary>
		private PublicIdentityScope(IPublicIdentityConverter converter, AmbientScopeOption ambientScopeOption)
			: base(ambientScopeOption)
		{
			this.Converter = converter ?? throw new ArgumentNullException(nameof(converter));
		}

		protected override void DisposeImplementation()
		{
			// Nothing to dispose
		}

		/// <summary>
		/// Sets the ubiquitous default scope, overwriting and disposing the previous one if necessary.
		/// </summary>
		/// <param name="newConverter">The public identity converter to register. May be null to unregister.</param>
		internal static void SetDefaultValue(IPublicIdentityConverter newConverter)
		{
			var newDefaultScope = newConverter is null
				? null
				: new PublicIdentityScope(newConverter, AmbientScopeOption.NoNesting);
			SetDefaultScope(newDefaultScope);
		}
	}
}
