using System;
using System.Collections.Generic;
using System.Linq;
using Architect.AmbientContexts;

namespace Architect.Identities.Helpers
{
	/// <summary>
	/// Provides information about test environments.
	/// </summary>
	internal sealed class TestDetector : AmbientScope<TestDetector>
	{
		static TestDetector()
		{
			SetDefaultScope(new TestDetector(IsTestFrameworkLoaded(), AmbientScopeOption.NoNesting));
		}

		private static readonly HashSet<string> TestFrameworkNames = new HashSet<string>() { "Xunit.", "Nunit.", "VisualStudio.TestTools.", "VisualStudio.TestPlatform." };

		private static TestDetector Current => GetAmbientScope()!;

		/// <summary>
		/// Are we currently running as part of a unit test or the like?
		/// </summary>
		public static bool IsTestRun => Current._isTestRun;

		private readonly bool _isTestRun;

		public TestDetector(bool isTestRun)
			: this(isTestRun, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		/// <summary>
		/// Private constructor.
		/// Does not activate.
		/// </summary>
		private TestDetector(bool isTestRun, AmbientScopeOption ambientScopeOption)
			: base(ambientScopeOption)
		{
			this._isTestRun = isTestRun;
		}

		protected override void DisposeImplementation()
		{
			// Nothing to dispose
		}

		/// <summary>
		/// Returns true if any one of a specific set of test frameworks is currently loaded in the current AppDomain.
		/// </summary>
		private static bool IsTestFrameworkLoaded()
		{
			// Only look at loaded assemblies, without loading anything new
			return AppDomain.CurrentDomain.GetAssemblies()
				.Any(assembly => TestFrameworkNames.Any(name => assembly.FullName != null && assembly.FullName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
		}
	}
}
