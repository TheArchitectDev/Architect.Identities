using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class AzureBlobApplicationInstanceIdSourceExtensions
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

			options.Services.AddSingleton(CreateInstance);
			return options;

			// Local function used to create an instance
			IApplicationInstanceIdSource CreateInstance(IServiceProvider serviceProvider)
			{
				var blobContainerClient = blobContainerClientFactory(serviceProvider);
				var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
				var exceptionHandler = options.ExceptionHandlerFactory?.Invoke(serviceProvider);

				var instance = ApplicationInstanceIdSourceFactory.Create(applicationLifetime, () => new AzureBlobApplicationInstanceIdSource(
					new AzureBlobApplicationInstanceIdSource.BlobContainerRepo(blobContainerClient),
					applicationLifetime,
					exceptionHandler));
				return instance;
			}
		}
	}
}
