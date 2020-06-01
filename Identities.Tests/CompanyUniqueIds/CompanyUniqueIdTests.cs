using System.Text;
using Xunit;

namespace Architect.Identities.Tests.CompanyUniqueIds
{
	/// <summary>
	/// The static subject under test merely acts as a wrapper, rather than implementing the functionality by itself.
	/// This test class merely confirms that its methods succeed, covering them. All other assertions are done in tests on the implementing types.
	/// </summary>
	public sealed partial class CompanyUniqueIdTests
	{
		private static readonly decimal SampleId = 447835050025542181830910637m;
		private static readonly string SampleShortString = "1drbWFYI4a3pLliX";
		private static readonly byte[] SampleShortStringBytes = Encoding.ASCII.GetBytes(SampleShortString);
		private static readonly string SampleDecimalString = 99999_99999_99999_99999_99999_999m.ToString();
		private static readonly byte[] SampleDecimalStringBytes = Encoding.ASCII.GetBytes(SampleDecimalString);

		[Fact]
		public void CreateId_Regularly_ShouldSucceed()
		{
			_ = CompanyUniqueId.CreateId();
		}

		[Fact]
		public void ToShortString_WithByteOutput_ShouldSucceed()
		{
			CompanyUniqueId.ToShortString(SampleId, stackalloc byte[16]);
		}

		[Fact]
		public void ToShortString_WithStringReturnValue_ShouldSucceed()
		{
			_ = CompanyUniqueId.ToShortString(SampleId);
		}

		[Fact]
		public void TryFromShortString_WithBytes_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromShortString(SampleShortStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromShortString_WithChars_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromShortString(SampleShortString, out _);
			Assert.True(success);
		}

		[Fact]
		public void FromShortStringOrDefault_WithBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromShortStringOrDefault(SampleShortStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromShortStringOrDefault_WithChars_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromShortStringOrDefault(SampleShortString);
			Assert.True(result > 0m);
		}

		[Fact]
		public void TryFromString_WithShortBytes_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromString(SampleShortStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithDecimalStringBytes_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromString(SampleDecimalStringBytes, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithShortString_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromString(SampleShortString, out _);
			Assert.True(success);
		}

		[Fact]
		public void TryFromString_WithDecimalString_ShouldSucceed()
		{
			var success = CompanyUniqueId.TryFromString(SampleDecimalString, out _);
			Assert.True(success);
		}

		[Fact]
		public void FromStringOrDefault_WithShortBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromStringOrDefault(SampleShortStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithDecimalStringBytes_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromStringOrDefault(SampleDecimalStringBytes);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithShortString_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromStringOrDefault(SampleShortString);
			Assert.True(result > 0m);
		}

		[Fact]
		public void FromStringOrDefault_WithDecimalString_ShouldReturnExpectedValue()
		{
			var result = CompanyUniqueId.FromStringOrDefault(SampleDecimalString);
			Assert.True(result > 0m);
		}
	}
}
