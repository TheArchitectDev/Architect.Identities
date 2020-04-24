using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using static Architect.Identities.AzureBlobApplicationInstanceIdSource;

namespace Architect.Identities.Tests.ApplicationInstanceIds.Implementations
{
	public sealed class AzureBlobApplicationInstanceIdSourceTests : IDisposable
	{
		/// <summary>
		/// Not started by default, for performance.
		/// </summary>
		private IHost Host { get; }
		private IHostApplicationLifetime HostApplicationLifetime { get; }
		private MockBlobContainerRepo Repo { get; }
		private AzureBlobApplicationInstanceIdSource Source { get; }
		private MockExceptionHandler ExceptionHandler { get; }

		public AzureBlobApplicationInstanceIdSourceTests()
		{
			var hostBuilder = new HostBuilder();
			this.Host = hostBuilder.Build();
			this.HostApplicationLifetime = this.Host.Services.GetRequiredService<IHostApplicationLifetime>();
			this.Repo = new MockBlobContainerRepo();
			this.ExceptionHandler = new MockExceptionHandler();
			this.Source = new AzureBlobApplicationInstanceIdSource(this.Repo, this.HostApplicationLifetime,
				exceptionHandler: this.ExceptionHandler.HandleException);
		}

		public void Dispose()
		{
			this.Host.Dispose();
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_Regularly_ShouldAddBlob()
		{
			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Single(this.Repo.Blobs);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithNoPriorIds_ShouldAddId1()
		{
			System.Diagnostics.Debug.Assert(this.Repo.Blobs.Count == 0);

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Single(this.Repo.Blobs);
			Assert.Equal("1", this.Repo.Blobs.Keys.Single());
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnlyPriorId1_ShouldAddId2()
		{
			this.Repo.Blobs.TryAdd("1", new byte[0]);

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Contains("2", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnlyPriorId100_ShouldAddId1()
		{
			this.Repo.Blobs.TryAdd("100", new byte[0]);

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Contains("1", this.Repo.Blobs.Keys);
		}

		[Fact]
		public async Task StopHost_Regularly_ShouldDeleteId()
		{
			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Contains("1", this.Repo.Blobs.Keys);

			await this.Host.StopAsync();

			Assert.DoesNotContain("1", this.Repo.Blobs.Keys);
		}

		[Fact]
		public async Task StopHost_WithMultipleIdsPresent_ShouldOnlyTouchOwnId()
		{
			this.Host.Start();

			this.Repo.Blobs.TryAdd("1", new byte[0]);

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Contains("2", this.Repo.Blobs.Keys);

			this.Repo.Blobs.TryAdd("3", new byte[0]);

			await this.Host.StopAsync();

			Assert.Contains("1", this.Repo.Blobs.Keys);
			Assert.DoesNotContain("2", this.Repo.Blobs.Keys);
			Assert.Contains("3", this.Repo.Blobs.Keys);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_Regularly_ShouldSucceed()
		{
			var _ = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Empty(this.ExceptionHandler.Invocations);
		}

		[Fact]
		public void CreateThroughFactory_WithException_ShouldThrow()
		{
			this.Repo.ShouldThrow = true;

			Assert.ThrowsAny<Exception>(() => this.Source.ContextUniqueApplicationInstanceId.Value);

			Assert.Equal(1, this.ExceptionHandler.Invocations.Count);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithException_ShouldThrow()
		{
			this.Repo.ShouldThrow = true;

			Assert.ThrowsAny<Exception>(() => _ = this.Source.ContextUniqueApplicationInstanceId.Value);

			Assert.Equal(1, this.ExceptionHandler.Invocations.Count);
		}

		[Fact]
		public async Task StopHost_WithException_ShouldCallExceptionHandler()
		{
			this.Host.Start();

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			this.Repo.ShouldThrow = true;

			Assert.Empty(this.ExceptionHandler.Invocations);

			await this.Host.StopAsync();

			Assert.Equal(1, this.ExceptionHandler.Invocations.Count);
		}

		private sealed class MockExceptionHandler
		{
			public IReadOnlyCollection<Exception> Invocations => this._invocations;
			private readonly List<Exception> _invocations = new List<Exception>();

			public void HandleException(Exception e) => this._invocations.Add(e);
		}

		private sealed class MockBlobContainerRepo : IBlobContainerRepo
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

			public bool UploadBlob(string blobName, Stream contentStream, bool overwrite)
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
