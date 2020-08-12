using System;
using System.Reflection;
using Architect.AmbientContexts;

namespace Architect.Identities.Helpers
{
	/// <summary>
	/// Provides information about being in a test host.
	/// </summary>
	internal sealed class TestDetector : AmbientScope<TestDetector>
	{
		static TestDetector()
		{
			SetDefaultScope(new TestDetector(IsRunningInTestHost(), AmbientScopeOption.NoNesting));
		}

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
		/// Returns true if the current process appears to be a known test host.
		/// </summary>
		private static bool IsRunningInTestHost()
		{
			const string testHostName = "testhost";

			try
			{
				var process = System.Diagnostics.Process.GetCurrentProcess();
				var isTestHost = process.ProcessName.Contains(testHostName, StringComparison.OrdinalIgnoreCase);
				return isTestHost;
			}
			catch
			{
				try
				{
					var entryAssembly = Assembly.GetEntryAssembly();
					var isTestHost = entryAssembly?.ManifestModule.Name.Contains(testHostName, StringComparison.OrdinalIgnoreCase) == true;
					return isTestHost;
				}
				catch
				{
					return false;
				}
			}
		}
	}
}
