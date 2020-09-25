using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Architect.Identities.ApplicationInstanceIds;
using Azure;
using Azure.Storage.Blobs;

namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Implementation for application instance ID management through a dedicated Azure Blob Storage Container.
	/// </para>
	/// <para>
	/// This implementation rents the smallest available ID by inserting it into a dedicated table.
	/// On returning, it attempts to remove that ID, freeing it up again.
	/// </para>
	/// </summary>
	internal sealed class AzureBlobApplicationInstanceIdRenter : BaseApplicationInstanceIdRenter
	{
		public IBlobContainerRepo Repo { get; }

		public AzureBlobApplicationInstanceIdRenter(IServiceProvider serviceProvider, IBlobContainerRepo repo)
			: base(serviceProvider)
		{
			this.Repo = repo ?? throw new ArgumentNullException(nameof(repo));
		}

		protected override ushort GetContextUniqueApplicationInstanceIdCore()
		{
			var metadata = $"ApplicationName={this.GetApplicationName()}\nServerName={this.GetServerName()}\nCreationDateTime={DateTime.UtcNow:O}";
			var metadataBytes = Encoding.UTF8.GetBytes(metadata);
			using var contentStream = new MemoryStream(metadataBytes);

			this.Repo.CreateContainerIfNotExists();

			while (true) // Loop to handle race conditions
			{
				ushort lastBlobId = 0;
				var blobNames = this.Repo.EnumerateBlobNames();
				foreach (var blobName in blobNames)
				{
					if (!UInt16.TryParse(blobName, out var blobId))
						throw new Exception($"{this.GetType().Name} encountered unrelated blobs in the Azure blob storage container.");

					// Break if we have found a gap
					if (blobId > lastBlobId + 1) break;

					lastBlobId = blobId;
				}

				if (lastBlobId == UInt16.MaxValue)
					throw new Exception($"{this.GetType().Name} created an application instance ID overflowing UInt16.");

				var applicationInstanceId = (ushort)(lastBlobId + 1);

				var didCreateBlob = this.Repo.UploadBlob(applicationInstanceId.ToString(), contentStream);

				if (!didCreateBlob) continue; // Race condition - loop to retry

				return applicationInstanceId;
			}
		}

		protected override void ReturnContextUniqueApplicationInstanceIdCore(ushort id)
		{
			this.Repo.DeleteBlob(id.ToString(), includeSnapshots: true);
		}

		/// <summary>
		/// Represents a client to a particular BlobContainer.
		/// Must be thread-safe.
		/// </summary>
		public interface IBlobContainerRepo
		{
			void CreateContainerIfNotExists();
			IEnumerable<string> EnumerateBlobNames();
			/// <summary>
			/// If the blob name already exists, this method returns false.
			/// </summary>
			bool UploadBlob(string blobName, Stream contentStream);
			void DeleteBlob(string blobName, bool includeSnapshots);
		}

		internal sealed class BlobContainerRepo : IBlobContainerRepo
		{
			/// <summary>
			/// Thread-safe according to Microsoft: poorly documented, but mentioned on Github and in Microsoft's latest design guidelines.
			/// </summary>
			private BlobContainerClient Client { get; }

			public BlobContainerRepo(BlobContainerClient client)
			{
				this.Client = client ?? throw new ArgumentNullException(nameof(client));
			}

			public void CreateContainerIfNotExists()
			{
				var creationResponse = this.Client.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);
				var creationResponsStatusCode = creationResponse?.GetRawResponse().Status;
				if (creationResponsStatusCode != null && creationResponsStatusCode != (int)HttpStatusCode.OK && creationResponsStatusCode != (int)HttpStatusCode.Created)
					throw new Exception($"{this.GetType().Name} received an unexpected status code trying to ensure that the container exists: {creationResponsStatusCode}.");
			}

			public IEnumerable<string> EnumerateBlobNames()
			{
				var blobs = this.Client.GetBlobs();
				foreach (var blob in blobs) yield return blob.Name;
			}

			public bool UploadBlob(string blobName, Stream contentStream)
			{
				var blobClient = this.Client.GetBlobClient(blobName);
				try
				{
					var uploadResponse = blobClient.Upload(contentStream, overwrite: false);
					var rawUploadResponse = uploadResponse.GetRawResponse();
					var uploadResponseStatusCode = rawUploadResponse.Status;
					if (uploadResponseStatusCode != (int)HttpStatusCode.OK && uploadResponseStatusCode != (int)HttpStatusCode.Created)
						throw new Exception($"{this.GetType().Name} received unexpected status code {uploadResponseStatusCode} trying to upload a blob: {blobName}. Reason: {rawUploadResponse.ReasonPhrase}");
					return true;
				}
				catch (RequestFailedException e) when (e.ErrorCode == "BlobAlreadyExists")
				{
					// Race condition
					return false;
				}
			}

			public void DeleteBlob(string blobName, bool includeSnapshots)
			{
				var deleteResponse = this.Client.DeleteBlob(blobName, includeSnapshots
					? Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots
					: Azure.Storage.Blobs.Models.DeleteSnapshotsOption.None);
				var deleteResponseStatusCode = deleteResponse.Status;
				if (deleteResponseStatusCode != (int)HttpStatusCode.OK && deleteResponseStatusCode != (int)HttpStatusCode.Accepted)
					throw new Exception($"{this.GetType().Name} received unexpected status code {deleteResponseStatusCode} trying to delete a blob: {blobName}. Reason: {deleteResponse.ReasonPhrase}");
			}
		}
	}
}
