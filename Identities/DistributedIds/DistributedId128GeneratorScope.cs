using System;
using Architect.AmbientContexts;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Provides access to an <see cref="IDistributedId128Generator"/> through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// This type provides a lightweight Inversion of Control (IoC) mechanism.
	/// The mechanism optimizes accessiblity (through a static property) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.
	/// </para>
	/// <para>
	/// A default scope is available by default.
	/// Changing the scope is intended for testing purposes, to control the IDs generated.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="DistributedId128GeneratorScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public sealed class DistributedId128GeneratorScope : AmbientScope<DistributedId128GeneratorScope>
	{
		static DistributedId128GeneratorScope()
		{
			var defaultGenerator = new DistributedId128Generator();
			var defaultScope = new DistributedId128GeneratorScope(defaultGenerator, AmbientScopeOption.NoNesting);
			SetDefaultScope(defaultScope);
		}

		internal static DistributedId128GeneratorScope Current => GetAmbientScope()!;
		public static IDistributedId128Generator CurrentGenerator => Current.IdGenerator;

		private IDistributedId128Generator IdGenerator { get; }

		/// <summary>
		/// Establishes the given <see cref="IDistributedId128Generator"/> as the ambient one until the scope is disposed.
		/// </summary>
		public DistributedId128GeneratorScope(IDistributedId128Generator idGenerator)
			: this(idGenerator, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		private DistributedId128GeneratorScope(IDistributedId128Generator idGenerator, AmbientScopeOption scopeOption)
			: base(scopeOption)
		{
			this.IdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		}

		protected override void DisposeImplementation()
		{
			if (this.IdGenerator is IDisposable disposableGenerator)
				disposableGenerator.Dispose();
		}
	}
}
