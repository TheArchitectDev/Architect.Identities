# Architect.Identities

Provides various tools related to IDs.

This package features the **Fluid** ID generator, which generates **F**lexible, **L**ocally-**U**nique **ID**s. A Fluid is intended to replace a pair of auto-increment ID + UUID. It is 64-bit and incremental (i.e. suitable as a primary key) and does not leak the sensitive information that an auto-increment ID does.

The package also features the **PublicIdentities** system, a set of tools for converting local IDs to public IDs. If the local IDs still leak too much information to be shared publically, PublicIdentities can be used. It converts 64-bit (or smaller) IDs into deterministic, reversible public IDs that are indistinguishable from random noise without possession of the configured key. These can replace UUIDs, without the additional bookkeeping.

Furthermore, this package features various **ApplicationInstanceIdSource** implementations. These implementations provide a unique ID to each distinct application (or instance thereof) in a bounded context, by using a centralized storage component, such as a SQL database or an Azure Blob Storage Container. The Fluid system relies on this.

## Registering and Using an IIdGenerator

```C#
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		// The Fluid generator needs a source of unique application instance IDs
		services.AddApplicationInstanceIdSource(source => source.UseFixedSource(valueFromConfig));

		// Register the Fluid ID generator
		services.AddIdGenerator(generator => generator.UseFluid());
	}
}

public class ExampleFactory
{
	public IIdGenerator IdGenerator { get; }

	public ExampleFactory(IIdGenerator idGenerator)
  {
  	this.IdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
  }
  
  public Example CreateExample()
  {
    return new Example(this.IdGenerator.CreateId());
  }
}
```

## Application Instance ID Sources

The Fluid ID generator uses a unique identifier of this instance of this particular application. This ensures that no collisions occur between IDs generator by different applications within the same context, or by multiple instances of the same application.

The package offers various application instance ID sources. Most sources use some form centralized external storage. Such sources claim an ID on startup and release it again on shutdown. The occasional unclean shutdown may cause IDs to linger. However, the default settings allows for 2048 different application instance IDs, leaving plenty of room. Still, you can opt to manually delete registrations from the external storage.

- `UseFixedSource`. This source allows you to manually provide the application instance ID.
- `UseAzureBlobStorageContainer`. This source uses an Azure blob storage container to store IDs that are in use.
- `UseSqlServer`. This source uses a SQL Server or Azure SQL database, creating the ID tracking table if it does not yet exist.
- `UseMySql`. This source uses a MySQL database, creating the ID tracking table if it does not yet exist.
- `UseStandardSql`. This source works with most SQL databases, allowing other databases to be used without the need for custom extensions. However, since table creation syntax rarely follows the standard, this throws if the required table does not exist.

Third party libraries may provide additional sources through further extension methods.
