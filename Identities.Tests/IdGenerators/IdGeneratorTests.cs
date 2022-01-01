using Architect.Identities.Helpers;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators
{
	public sealed class IdGeneratorTests
	{
		[Fact]
		public void Current_WithNothingRegisteredOutsideUnitTest_ShouldMatchResultOfIdGeneratorScopeCurrentGenerator()
		{
			static IIdGenerator Action(Func<IIdGenerator> getGenerator)
			{
				using (new TestDetector(isTestRun: false))
				{
					System.Diagnostics.Debug.Assert(!TestDetector.IsTestRun);
					return getGenerator();
				}
			}

			var expectedResult = this.GetResultOrExceptionMessage(() => Action(() => IdGeneratorScope.CurrentGenerator));
			var result = this.GetResultOrExceptionMessage(() => Action(() => IdGenerator.Current));

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Current_WithCustomScope_ShouldMatchResultOfIdGeneratorScopeCurrentGenerator()
		{
			using (new IdGeneratorScope(new CustomIdGenerator(1UL)))
			{
				var expectedResult = this.GetResultOrExceptionMessage(() => IdGeneratorScope.CurrentGenerator);
				var result = this.GetResultOrExceptionMessage(() => IdGenerator.Current);

				Assert.Equal(expectedResult, result);
			}
		}

		[Fact]
		public void Current_WithNestedCustomScopes_ShouldMatchResultOfIdGeneratorScopeCurrentGenerator()
		{
			using (new IdGeneratorScope(new CustomIdGenerator(1UL)))
			using (new IdGeneratorScope(new CustomIdGenerator(2UL)))
			{
				var expectedResult = this.GetResultOrExceptionMessage(() => IdGeneratorScope.CurrentGenerator);
				var result = this.GetResultOrExceptionMessage(() => IdGenerator.Current);

				Assert.Equal(expectedResult, result);
			}
		}

		private object GetResultOrExceptionMessage<T>(Func<T> action)
		{
			try
			{
				return action();
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}
	}
}
