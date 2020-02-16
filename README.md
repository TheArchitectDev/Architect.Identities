# Architect.Identities

Provides various tools related to IDs.

This package features the **Fluid** ID generator, which generates **F**lexible, **L**ocally-**U**nique **ID**s. A Fluid is intended to replace the combination of auto-increment ID and UUID. It is 64-bit and incremental (i.e. efficient as a primary key) and does not leak the sensitive information that an auto-increment ID does.

The package also provides **PublicIdentities**, a set of tools for converting local IDs to public IDs. When a Fluid is still considered to leak too much information to be exposed publically, or when using auto-increment IDs, PublicIdentities can help out. This subsystem converts 64-bit (or smaller) IDs into deterministic, reversible public IDs that are indistinguishable from random noise without possession of the configured key. Using the key, public IDs can be converted back to the original IDs. These can replace UUIDs, without the additional bookkeeping.

Furthermore, this package features various **ApplicationInstanceIdSource** implementations. These implementations provide a unique ID to each distinct application (or instance thereof) within a chosen bounded context, by using a centralized storage component, such as a SQL database or an Azure Blob Storage Container. The Fluid ID generator relies on this feature to ensure that generated IDs are unique.

## Registering and Using an IIdGenerator

```C#
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		// The Fluid generator needs a source to provide its unique application instance ID
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

## Service-free Usage

The Fluid ID generator can be used without the need for injected services. For example, in Domain-Driven Design (DDD) an entity's ID property should be immutable. Ideally, it does not have a setter at all. We must either inject the ID into the constructor (complicating construction) or make the entity responsible for generating its own ID.

## Fluid Requirements

Because a Fluid contains a time component, it relies on the host system's clock. This would introduce the risk of collisions if the clock were to be adjusted backwards. To counter this, the generator will allow the clock to catch up if necessary (i.e. if the last generated value has a timestamp _greater_ than the current timestamp), by up to one second. However, the system is responsible for keeping potential clock adjustments under a second. This is generally achieved by **having the system clock synchronized using the Network Time Protocol (NTP)**, which your systems should be doing anyway. (Note that the timestamps are based on UTC, so daylight savings adjustments do not apply.)

## Attack Surface

We know that a Fluid does not leak volume information like an auto-increment ID does. Still, since is hard to reveal less than a fully random ID, we should consider what information we are revealing.

Most obviously, the timestamp component reveals the entity's creation datetime. Next, the application instance ID component reveals: "there are _likely_ at least this many application instances within the bounded context". Given the ability to receive new IDs at will and sufficient determination, an attacker could also determine which entities are created by the same applications and how many instances of such applications are reachable. Finally, the counter component lets a determined attacker discover the latter as well, and at what rate these instances are _currently_ producing any entities. Note that applications usually create various entities, and this does not reveal how the production rates are distributed across them.
