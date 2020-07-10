﻿using Xunit;

namespace Architect.Identities.Tests.CompanyUniqueIds
{
	/// <summary>
	/// The static subject under test merely acts as a wrapper, rather than implementing the functionality by itself.
	/// This test class merely confirms that its methods succeed, covering them. All other assertions are done in tests on the implementing types.
	/// </summary>
	public sealed class CompanyUniqueIdTests
	{
		[Fact]
		public void CreateId_Regularly_ShouldSucceed()
		{
			_ = CompanyUniqueId.CreateId();
		}
	}
}