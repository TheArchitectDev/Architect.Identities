using System;
using Architect.Identities.ApplicationInstanceIds;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.Identities
{
	public static class AzureBlobApplicationInstanceIdRenterExtensions
	{
		/// <summary>
		/// <para>
		/// Registers an implementation based on a dedicated Azure Blob Storage Container. Use the options to specify the container.
		/// </para>
		/// <para>
		/// The storage container must be used for this purpose only, and no other content must ever exist in it.
		/// </para>
		/// <para>
		/// The implementation will throw if the Azure Storage Account is unreachable when the application instance ID is registered.
		/// The container, however, is created automatically if it does not exist.
		/// </para>
		/// </summary>
		public static ApplicationInstanceIdSourceExtensions.Options UseAzureBlobStorageContainer(this ApplicationInstanceIdSourceExtensions.Options options,
			BlobContainerClient blobContainerClient)
		{
			if (blobContainerClient is null) throw new ArgumentNullException(nameof(blobContainerClient));

			return UseAzureBlobStorageContainer(options, serviceProvider => blobContainerClient);
		}

		/// <summary>
		/// <para>
		/// Registers an implementation based on a dedicated Azure Blob Storage Container. Use the options to specify the container.
		/// </para>
		/// <para>
		/// The storage container must be used for this purpose only, and no other content must ever exist in it.
		/// </para>
		/// <para>
		/// The implementation will throw if the Azure Storage Account is unreachable when the application instance ID is registered.
		/// The container, however, is created automatically if it does not exist.
		/// </para>
		/// </summary>
		public static ApplicationInstanceIdSourceExtensions.Options UseAzureBlobStorageContainer(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<IServiceProvider, BlobContainerClient> blobContainerClientFactory)
		{
			if (blobContainerClientFactory is null) throw new ArgumentNullException(nameof(blobContainerClientFactory));

			options.Services.AddTransient(CreateInstance);

			return options;

			// Local function used to create an instance
			IApplicationInstanceIdRenter CreateInstance(IServiceProvider serviceProvider)
			{
				var blobContainerClient = blobContainerClientFactory(serviceProvider) ?? throw new Exception($"The factory produced a null {nameof(BlobContainerClient)}.");
				var blobContainerRepo = new AzureBlobApplicationInstanceIdRenter.BlobContainerRepo(blobContainerClient);

				var instance = new AzureBlobApplicationInstanceIdRenter(serviceProvider, blobContainerRepo);
				return instance;
			}
		}
	}
}
