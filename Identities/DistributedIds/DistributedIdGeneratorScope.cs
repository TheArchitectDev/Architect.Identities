using System;
using Architect.AmbientContexts;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Provides access to an <see cref="IDistributedIdGenerator"/> through the Ambient Context pattern.
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
	/// Outer code may construct a custom <see cref="DistributedIdGeneratorScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public sealed class DistributedIdGeneratorScope : AmbientScope<DistributedIdGeneratorScope>
	{
		static DistributedIdGeneratorScope()
		{
			var defaultGenerator = new DistributedIdGenerator();
			var defaultScope = new DistributedIdGeneratorScope(defaultGenerator, AmbientScopeOption.NoNesting);
			SetDefaultScope(defaultScope);
		}

		internal static DistributedIdGeneratorScope Current => GetAmbientScope()!;
		public static IDistributedIdGenerator CurrentGenerator => Current.IdGenerator;

		private IDistributedIdGenerator IdGenerator { get; }

		/// <summary>
		/// Establishes the given <see cref="IDistributedIdGenerator"/> as the ambient one until the scope is disposed.
		/// </summary>
		public DistributedIdGeneratorScope(IDistributedIdGenerator idGenerator)
			: this(idGenerator, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		private DistributedIdGeneratorScope(IDistributedIdGenerator idGenerator, AmbientScopeOption scopeOption)
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
