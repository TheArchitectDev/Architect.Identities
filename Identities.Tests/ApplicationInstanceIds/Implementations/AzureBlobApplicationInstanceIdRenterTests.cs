using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.ApplicationInstanceIds.Implementations
{
	public sealed class AzureBlobApplicationInstanceIdRenterTests : IDisposable
	{
		/// <summary>
		/// Not started by default, for performance.
		/// </summary>
		private IHost Host { get; }
		private MockBlobContainerRepo Repo { get; }
		private AzureBlobApplicationInstanceIdRenter Renter { get; }
		private MockExceptionHandler ExceptionHandler { get; }

		public AzureBlobApplicationInstanceIdRenterTests()
		{
			this.ExceptionHandler = new MockExceptionHandler();

			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services => services.AddSingleton<IExceptionHandler>(this.ExceptionHandler));
			this.Host = hostBuilder.Build();
			this.Repo = new MockBlobContainerRepo();
			this.Renter = new AzureBlobApplicationInstanceIdRenter(this.Host.Services, this.Repo);
		}

		public void Dispose()
		{
			this.Host.Dispose();
		}

		[Fact]
		public void RentId_Regularly_ShouldAddBlob()
		{
			_ = this.Renter.RentId();

			Assert.Single(this.Repo.Blobs);
		}

		[Fact]
		public void RentId_WithNoPriorIds_ShouldAddId1()
		{
			System.Diagnostics.Debug.Assert(this.Repo.Blobs.Count == 0);

			_ = this.Renter.RentId();

			Assert.Single(this.Repo.Blobs);
			Assert.Equal("1", this.Repo.Blobs.Keys.Single());
		}

		[Fact]
		public void RentId_WithOnlyPriorId1_ShouldAddId2()
		{
			this.Repo.Blobs.TryAdd("1", new byte[0]);

			_ = this.Renter.RentId();

			Assert.Contains("2", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void RentId_WithOnlyPriorId100_ShouldAddId1()
		{
			this.Repo.Blobs.TryAdd("100", new byte[0]);

			_ = this.Renter.RentId();

			Assert.Contains("1", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void ReturnId_Regularly_ShouldDeleteId()
		{
			var id = this.Renter.RentId();

			Assert.Contains("1", this.Repo.Blobs.Keys);

			this.Renter.ReturnId(id);

			Assert.DoesNotContain("1", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void ReturnId_WithMultipleIdsPresent_ShouldOnlyTouchOwnId()
		{
			this.Repo.Blobs.TryAdd("1", new byte[0]);

			var id = this.Renter.RentId();

			Assert.Contains("2", this.Repo.Blobs.Keys);

			this.Repo.Blobs.TryAdd("3", new byte[0]);

			this.Renter.ReturnId(id);

			Assert.Contains("1", this.Repo.Blobs.Keys);
			Assert.DoesNotContain("2", this.Repo.Blobs.Keys);
			Assert.Contains("3", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void RentId_Regularly_ShouldNotThrow()
		{
			var _ = this.Renter.RentId();

			Assert.Empty(this.ExceptionHandler.Invocations);
		}

		[Fact]
		public void RentId_WithException_ShouldThrow()
		{
			this.Repo.ShouldThrow = true;

			Assert.ThrowsAny<Exception>(() => this.Renter.RentId());

			Assert.Equal(1, this.ExceptionHandler.Invocations.Count);
		}

		[Fact]
		public void ReturnId_WithException_ShouldCallExceptionHandler()
		{
			this.Host.Start();

			var id = this.Renter.RentId();

			this.Repo.ShouldThrow = true;

			Assert.Empty(this.ExceptionHandler.Invocations);

			this.Renter.ReturnId(id);

			Assert.Equal(1, this.ExceptionHandler.Invocations.Count);
		}

		private sealed class MockExceptionHandler : IExceptionHandler
		{
			public IReadOnlyCollection<Exception> Invocations => this._invocations;
			private readonly List<Exception> _invocations = new List<Exception>();

			public void HandleException(Exception e) => this._invocations.Add(e);
		}

		private sealed class MockBlobContainerRepo : AzureBlobApplicationInstanceIdRenter.IBlobContainerRepo
		{
			public bool ContainerExists { get; set; }

			// Concurrent should not be necessary, but we will stick to the interface's thread-safety instructions somewhat
			public ConcurrentDictionary<string, byte[]> Blobs { get; } = new ConcurrentDictionary<string, byte[]>();

			/// <summary>
			/// If set to true, the next invocation throws.
			/// </summary>
			public bool ShouldThrow { get; set; }

			public void CreateContainerIfNotExists()
			{
				if (this.ShouldThrow) throw new Exception("Instructed to throw by unit test.");

				if (!this.ContainerExists) this.ContainerExists = true;
			}

			public void DeleteBlob(string blobName, bool includeSnapshots)
			{
				if (this.ShouldThrow) throw new Exception("Instructed to throw by unit test.");

				if (!this.Blobs.TryRemove(blobName, out _))
					throw new Exception("Blob does not exist.");
			}

			public IEnumerable<string> EnumerateBlobNames()
			{
				if (this.ShouldThrow) throw new Exception("Instructed to throw by unit test.");

				return this.Blobs.Keys;
			}

			public bool UploadBlob(string blobName, Stream contentStream)
			{
				if (this.ShouldThrow) throw new Exception("Instructed to throw by unit test.");

				var didAdd = false;
				this.Blobs.GetOrAdd(blobName, key =>
				{
					didAdd = true;

					var memoryStream = new MemoryStream();
					if (contentStream is MemoryStream inputMemoryStream) memoryStream = inputMemoryStream;
					else contentStream.CopyTo(memoryStream);

					return memoryStream.ToArray();
				});
				return didAdd;
			}
		}
	}
}
