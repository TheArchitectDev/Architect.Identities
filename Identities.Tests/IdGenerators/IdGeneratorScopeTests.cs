using System;
using Architect.Identities.Helpers;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators
{
	public sealed class IdGeneratorScopeTests
	{
		/// <summary>
		/// This method combines a few tests.
		/// Because of the static nature of Ambient Context defaults, we test them in a single test.
		/// </summary>
		[Fact]
		public void Current_WithOnlyDefault_ShouldSucceedForUnitTestButRequireRegistrationOtherwise()
		{
			using (new TestDetector(isTestRun: false))
			{
				if (TestDetector.IsTestRun) throw new Exception("Test detector wrongfully detected test run.");

				Assert.Throws<InvalidOperationException>(() => IdGeneratorScope.Current);

				var hostBuilder = new HostBuilder();
				hostBuilder.ConfigureServices(services =>
				{
					services
						.AddApplicationInstanceIdSource(source => source.UseFixedSource(1))
						.AddIdGenerator(generator => generator.UseFluid());
				});
				using (var host = hostBuilder.Build())
				{
					Assert.Throws<InvalidOperationException>(() => IdGeneratorScope.Current);
					host.UseIdGeneratorScope();
					Assert.NotNull(IdGeneratorScope.Current);
				}
				IdGeneratorScope.SetDefaultValue(null);
				Assert.Throws<InvalidOperationException>(() => IdGeneratorScope.Current);
			}

			if (!TestDetector.IsTestRun) throw new Exception("Test detector failed to detect test run.");

			Assert.NotNull(IdGeneratorScope.Current);
		}

		[Fact]
		public void Current_WithLocalScope_ShouldUseScopedGenerator()
		{
			var invocationDetector = new InvocationDetector();
			using var scope = new IdGeneratorScope(CreateIdGenerator(invocationDetector));

			Assert.True(invocationDetector.WasInvoked);
		}

		[Fact]
		public void Current_WithLocalScopeWhenUsed_ShouldUseScopedGenerator()
		{
			var invocationDetector = new InvocationDetector();
			using var scope = new IdGeneratorScope(CreateIdGenerator(invocationDetector));
			invocationDetector.WasInvoked = false;

			IdGeneratorScope.Current.Generator.CreateId();

			Assert.True(invocationDetector.WasInvoked);
		}

		[Fact]
		public void Current_WithNestedScopesWhenUsed_ShouldUseNearestScopedGenerator()
		{
			var outerInvocationDetector = new InvocationDetector();
			using var outerScope = new IdGeneratorScope(CreateIdGenerator(outerInvocationDetector));
			outerInvocationDetector.WasInvoked = false;

			var innerInvocationDetector = new InvocationDetector();
			using var innerScope = new IdGeneratorScope(CreateIdGenerator(innerInvocationDetector));
			outerInvocationDetector.WasInvoked = false;

			Assert.True(innerInvocationDetector.WasInvoked);
			Assert.False(outerInvocationDetector.WasInvoked);
		}

		private static IIdGenerator CreateIdGenerator(InvocationDetector invocationDetector)
		{
			return new FluidIdGenerator(isProduction: false, utcClock: GetUtcNow, applicationInstanceId: 1);

			// Local function that acts as the UTC clock and also toggles a ref bool
			DateTime GetUtcNow()
			{
				invocationDetector.WasInvoked = true;
				return FluidIdGenerator.GetUtcNow();
			}
		}

		/// <summary>
		/// Constructed by tests and passed to code that will change it if invoked.
		/// Helps detect whether a method was invoked on a particular instance.
		/// </summary>
		private sealed class InvocationDetector
		{
			public bool WasInvoked { get; set; }
		}
	}
}
