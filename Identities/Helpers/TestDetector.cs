using System;
using System.Collections.Generic;
using System.Linq;

namespace Architect.Identities.Helpers
{
	/// <summary>
	/// Provides information about test environments.
	/// </summary>
	internal static class TestDetector
	{
		private static readonly HashSet<string> TestFrameworkNames = new HashSet<string>() { "Xunit.", "Nunit.", "VisualStudio.TestTools.", "VisualStudio.TestPlatform." };

		/// <summary>
		/// Are we currently running as part of a unit test or the like?
		/// </summary>
		public static bool IsTestRun { get; } = IsTestFrameworkLoaded();

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
