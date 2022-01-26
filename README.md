# Architect.Identities

Reliable unique ID generation for distributed applications.

This package provides highly tuned tools for ID generation and management.

- [TLDR](#tldr)
- [Introduction](#introduction)
- [Distributed IDs](#distributed-ids)
  * [Example Value](#example-value)
  * [Example Usage](#example-usage)
  * [Benefits](#benefits)
  * [Trade-offs](#trade-offs)
  * [Structure](#structure)
  * [Collision Resistance](#collision-resistance)
    + [The degenerate worst case](#the-degenerate-worst-case)
    + [Absolute certainty](#absolute-certainty)
  * [Guessability](#guessability)
  * [Attack Surface](#attack-surface)
  * [Entity Framework](#entity-framework)
- [Public Identities](#public-identities)
  * [Example Value](#example-value-1)
  * [Example Usage](#example-usage-1)
  * [Implementation](#implementation)
  * [Forgery Resistance](#forgery-resistance)
- [Shorter IDs](#shorter-ids)
  * [Example Value](#example-value-2)
  * [Example Usage](#example-usage-2)
  * [Benefits](#benefits-1)
  * [Trade-offs](#trade-offs-1)
  * [Structure](#structure-1)
  * [Clock Synchronization](#clock-synchronization)
  * [Collision Resistance](#collision-resistance-1)
  * [Guessability](#guessability-1)
  * [Application Instance ID Sources](#application-instance-id-sources)
  * [Attack Surface](#attack-surface-1)

## TLDR

The **[DistributedId](#distributed-ids)** is a single ID that combines the advantages of auto-increment IDs and UUIDs.

For sensitive scenarios where zero metadata must be leaked from an ID, **[PublicIdentities](#public-identities)** can transform any ID into a public representation that reveals nothing, without ever introducing an unrelated secondary ID.

When very short, 64-bit numeric IDs are required, the **[Fluid](#shorter-ids)** provides an orchestrated alternative to the DistributedId, with different trade-offs.

## Introduction

Should entity IDs use UUIDs or auto-increment?

Auto-increment IDs are ill-suited for exposing publically: they leak hints about the row count and are easy to guess. Moreover, they are generated very late, on insertion, posing challenges to the creation of aggregates.

UUIDs, on the other hand, tend to be random, causing poor performance as database keys.

Using both types of ID on an entity is cumbersome and may leak a technical workaround into the domain model.

Luckily, we can do better.

## Distributed IDs

The DistributedId is a UUID replacement that is generated on-the-fly (without orchestration), unique, hard to guess, easy to store and sort, and highly efficient as a database key.

A DistributedId is created as a 93-bit decimal value of 27-28 digits, but can also be represented as a (case-sensitive) 16-char alphanumeric value or as a `Guid`.

Distributed applications can create unique DistributedIds with no synchronization mechanism between them. This holds true under almost any load. Even under extreme conditions, collisions tend to be far under 1 collision per 350 billion IDs generated.

DistributedIds are designed to be unique within a logical context, such as a database table, a Bounded Context, or even a whole medium-sized company. These form the most common boundaries within which uniqueness is required. Any number of distributed applications may generate new IDs within such a context.

Note that a DistributedId **reveals its creation timestamp**, which may be considered sensitive data in certain contexts.

### Example Value

- `decimal` value: `448147911486426236008828585` (27-28 digits)
- Alphanumeric encoding: `1dw14L86uHcPoQJd` (16 alphanumeric characters)

### Example Usage

```cs
decimal id = DistributedId.CreateId(); // 448147911486426236008828585

// For a more compact representation, IDs can be encoded in alphanumeric
string compactId = id.ToAlphanumeric(); // "1dw14L86uHcPoQJd"
decimal originalId = IdEncoder.GetDecimalOrDefault(compactId)
	?? throw new ArgumentException("Not a valid encoded ID.");
```

For SQL databases, the recommended column type is `DECIMAL(28, 0)`. Alternatively, a DistributedId can be stored as 16 _case-sensitive_ ASCII characters, or even as a UUID. (The latter is discouraged, as storage engines differ in how they sort UUIDs.)

The ID generation can be controlled from the outside, such as in unit tests that require constant IDs:

```cs
[Fact]
public void ShowInversionOfControl()
{
	// A custom generator is included in the package
	const decimal fixedId = 1m;
	using (new DistributedIdGeneratorScope(new CustomDistributedIdGenerator(() => fixedId)))
	{
		var entity = new Entity(); // Constructor implementation uses DistributedId.CreateId()
		Assert.Equal(fixedId, entity.Id); // True
		
		// A simple incremental generator is included as well
		using (new DistributedIdGeneratorScope(new IncrementalDistributedIdGenerator(fixedId)))
		{
			Assert.Equal(1m, DistributedId.CreateId()); // True
			Assert.Equal(2m, DistributedId.CreateId()); // True
			Assert.Equal(3m, DistributedId.CreateId()); // True
		}
		
		Assert.Equal(fixedId, DistributedId.CreateId()); // True
	}
}
```

### Benefits

- Is incremental, making it _significantly_ more efficient as a primary key than a UUID.
- Is shorter than a UUID, making it more efficient as a primary key.
- Like a UUID, can be generated on-the-fly, with no registration or synchronization whatsoever.
- Like a UUID, makes collisions extremely unlikely.
- Like a UUID, is hard to guess due to a significant random component.
- Like a UUID, does not require database insertion for determining the ID, nor reading the ID back in after insertion (as with auto-increment).
- Consists of digits only.
- Can be encoded as 16 alphanumeric characters, for a shorter representation.
- Uses the common `decimal` type, which is intuitively represented, sorted, and manipulated in .NET and databases (which cannot be said for UUIDs).
- Supports comparison operators (unlike UUIDs, which make comparisons notoriously hard to write using the Entity Framework).
- Is suitable for use in URLs.
- Can by selected in UIs (such as for copying) by double-clicking, as it consists of only word characters in both its numeric and alphanumeric form.

### Trade-offs

- Reveals its creation timestamp in milliseconds.
- Is rate-limited to 128 generated IDs per millisecond (i.e. 128K IDs per second), on average, per application instance.
- Is designed to be unique within a chosen context rather than globally. (The context could still be the entire company, depending on the collision resistance required.)
- Is slightly less efficient than a 64-bit integer.
- Is unpleasant to use with SQLite, which truncates decimals to 8 bytes. (The alphanumeric representation can be used to remedy this.)

### Structure

- Is represented as a positive `decimal` of up to 28 digits, with 0 decimal places.
- Occcupies 16 bytes in memory.
- Is represented as `DECIMAL(28, 0)` in SQL databases.
- Requires 13 bytes of storage in many SQL databases, including SQL Server and MySQL. (This is more compact than a UUID, which takes up 16 bytes.)
- Can be represented in a natural, workable form by most SQL databases, being a simple `decimal`. (By contrast, not all databases have a UUID type, requiring the use of binary types, making manual queries cumbersome.)
- Is ordered intuitively and consistently in .NET and databases. (By contrast, SQL Server orders UUIDs by some of the _middle_ bytes, making it very hard to implement an ordered UUID type.)
- Can be represented numerically, as up to 28 digits.
- Can be represented alphanumerically, as exactly 16 alphanumeric characters.
- Contains 93 bits worth of data.
- Contains the number of milliseconds since the epoch in its first 45 bits.
- Contains a cryptographically-secure pseudorandom sequence in its last 48 bits, with at least 42 bits of entropy.
- Uses 42-bit cryptographically-secure pseudorandom increments to remain incremental even intra-millisecond.
- Can represent timestamps beyond the year 3000.

### Collision Resistance

DistributedIds have strong collision resistance. The probability of generating the same ID twice is neglible for almost all contexts.

Most notably, collisions across different timestamps are impossible, since the millisecond values differ.

Within a single application instance, collisions during a particular millisecond are avoided (while maintaining the incremental nature) by reusing the previous random value (48 bits) and incrementing it by a smaller random value (42 bits). This guarantees unique IDs within the application instance, as long as the system clock is not adjusted backwards. If it is, the scenario is comparable to having an extra application instance (addressed below) during the repeated time span.

The scenario where collisions can occur is when multiple application instances are generating IDs at the same millisecond. It is detailed below and should be negligible.

#### The degenerate worst case

These are the statistics under the worst possible circumstances:

- On average, with 2 application instances at maximum throughput, there is **1 collision per 3500 billion IDs**. (That is 3,500,000,000,000. As a frame of reference, it takes 2 billion IDs to exhaust an `int` primary key.)
- On average, with 10 application instances at maximum throughput, there is **1 collision 350 billion IDs**.
- On average, with 100 application instances at maximum throughput, there is **1 collision per 35 billion IDs**.

It is important to note that **the above is only in the degenerate scenario** where _all instances_ are generating IDs _at the maximum rate per millisecond_, and always _on the exact same millisecond_. In practice, far fewer IDs tend to be generated per millisecond, thus spreading IDs out over more timestamps. This significantly reduces the realistic probability of a collision, to 1 per many trillions, which is negligible.

#### Absolute Certainty

Luckily, we can protect ourselves even against the extremely unlikely event of a collision.

For contexts where even a single collision could be catastrophic, such as in certain financial domains, it is advisable to avoid "upserts", and always explicitly separate inserts from updates. This way, even if a collision did occur, it would merely cause one single transaction to fail (out of billions or trillions), rather than overwriting an existing record. This is good practice in general.

### Guessability

Presupposing knowledge of the millisecond timestamp on which an ID was generated, the probability of guessing that ID is between 1/2^42 and 1/2^48, thanks to the 48-bit cryptographically-secure pseudorandom sequence. In practice, the timestamp component tends to reduce the guessability, since for most milliseconds no IDs at will will have been generated.

The difference between the two probabilities (given knowledge of the timestamp) stems from the way the incremental property is achieved. If only one ID was generated on a timestamp, as tends to be common, the probability is 1/2^48. If the maximum number of IDs were generated on that timestamp, or if another ID from the same timestamp is known, an educated guess has a 1/2^42 probability of being correct.

To reduce the guessability to 1/2^128, see [PublicIdentities](#public-identities).

### Attack Surface

A DistributedId reveals its creation timestamp. Otherwise, it consists of cryptographically-secure pseudorandom data.

### Entity Framework

When DistributedIds are used in Entity Framework, the column type needs to be configured. Although this can be done manually, the package [Architect.Identities.EntityFramework](https://www.nuget.org/packages/Architect.Identities.EntityFramework) facilitates it through its extension methods.

The recommended approach is to first map all entities, and then invoke a single extension method to set the correct column type for _all_ mapped properties that are of type `decimal` (including nullable ones) _and_ whose name ends in `Id` or `ID` (e.g. `Id`, `OrderId`, `ParentID`, etc.):

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	modelBuilder.Entity<Order>(entity =>
	{
		entity.Property(o => o.Id)
			.ValueGeneratedNever();
		
		entity.HasKey(o => o.Id);
	});
	
	// Other entities ...

	// For all mapped decimal columns named *Id or *ID
	modelBuilder.StoreDecimalIdsWithCorrectPrecision(dbContext: this);
}
```

Alternatively, each property can be mapped individually:

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	modelBuilder.Entity<Order>(entity =>
	{
		entity.Property(o => o.Id)
			.ValueGeneratedNever()
			.StoreWithDecimalIdPrecision(dbContext: this);
	});
}
```

The demonstrated methods abstract away the knowledge of how to configure the properties. If the database used at runtime is SQLite (e.g. for integration tests), the methods will automatically customize the mapping differently, since SQLite needs a little extra work to deal with high-precision decimals.

It is also possible to manually configure the precision of `28, 0`.

## Public Identities

Sometimes, revealing even a creation timestamp is too much. For example, an ID might represent a bank account.

Still, it is desirable to have only a single ID, and one that is efficient as a primary key, at that. To achieve that, we can create a public representation of that ID, one that reveals nothing.

### Example Value

- The regular ID can be any `long`, `ulong`, or `decimal`, such as a DistributedId or an auto-increment ID: `29998545287255040`
- Public `Guid` value: `32f0edac-8063-2c68-5c43-c889b058556e` (16 bytes)
- Alphanumeric encoding: `EqUxdTU1Ih27v7dmQVilag` (22 alphanumeric characters)

### Example Usage

```cs
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
	const string key = "k7fBDJcQam02hsByaOWPeP2CqeDeGXvrPUkEAQBtAFc="; // Strong 256-bit key
	
	services.AddPublicIdentities(publicIdentities => publicIdentities.Key(key));
}
```

```cs
public void ExampleUse(IPublicIdentityConverter publicIdConverter)
{
	long id = IdGenerator.Current.CreateId(); // 29998545287255040
	
	// Convert to public ID
	Guid publicId = publicIdConverter.GetPublicRepresentation(id); // 32f0edac-8063-2c68-5c43-c889b058556e
	string publicIdString = publicId.ToAlphanumeric(); // "EqUxdTU1Ih27v7dmQVilag" (22 chars)
	
	// Convert back to internal ID
	long originalId = publicIdConverter.GetLongOrDefault(IdEncoder.GetGuidOrDefault(publicIdString) ?? Guid.Empty)
		?? throw new ArgumentException("Not a valid ID.");
}
```

```cs
public void ExampleHexadecimalEncoding(IPublicIdentityConverter publicIdConverter)
{
	long id = IdGenerator.Current.CreateId(); // 29998545287255040
	
	// We can use Guid's own methods to get a hexadecimal representation
	Guid publicId = publicIdConverter.GetPublicRepresentation(id); // 32f0edac-8063-2c68-5c43-c889b058556e
	string publicIdString = publicId.ToString("N").ToUpperInvariant(); // "32F0EDAC80632C685C43C889B058556E" (32 chars)
	
	// Convert back to internal ID
	long originalId = publicIdConverter.GetLongOrDefault(new Guid(publicIdString))
		?? throw new ArgumentException("Not a valid ID.");
}
```

### Implementation

Public identities are implemented by using AES encryption under a secret key. With the key, the original ID can be retrieved. _Without the key_, the data is indistinguishable from random noise. In fact, it is exactly the size of a UUID, and it can be formatted to look just like one, for that familiar feel.

Obviously, it is important that the secret key is kept safe. Moreover, the key must not be changed. Doing so would render any previously provided public identities invalid.

### Forgery Resistance

Without possession of the key, it is extremely hard to forge a valid public identity.

When a public identity is converted back into the original ID, its structure is validated. If it is invalid, `null` or `false` is returned, depending on the method used.

For `long` and `ulong` IDs, the chance to forge a valid ID is 1/2^64. For `decimal` IDs, the chance is 1/2^32. Even if a valid value were to be forged, the resulting internal ID would be a random one and would be extremely unlikely to match an existing one.

Generally, when an ID is taken as client input, something is loaded based on that ID. As such, it is often best to simply turn an invalid public identity into a nonexistent local ID, such as 0:

```cs
// Any invalid input will result in id=0
long id = publicIdConverter.GetLongOrDefault(IdEncoder.GetGuidOrDefault(publicIdString) ?? Guid.Empty)
	?? 0L;

var entity = this.Repository.GetEntityById(id);
if (entity is null) return this.NotFound();
```

## Shorter IDs

Sometimes a very short, 64-bit numeric ID is required. Due to its tighter constraints, such an ID requires orchestration and has additional caveats. As such, use of the **[DistributedId](#distributed-ids)** is recommended.

The **F**lexible, **L**ocally-**U**nique **ID** is a 64-bit ID value guaranteed to be unique within its configured context. It is extremely efficient as a primary key, and it avoids leaking the sensitive information that an auto-increment ID does.

An application using Fluids **should run on a clock-synchronized system** with clock adjustments of no more than 5 seconds. This is because if the clock is adjusted backwards right after an ID was generated, the system needs to wait for the clock to reach its original value again in order to guarantee ID uniqueness. In other words, once an application instance generates an ID on a timestamp, it will no longer generate IDs on _earlier_ timestamps. ID generation will wait for up to 5 seconds to let the clock catch up, or throw an exception if it would need to wait longer.

Note that a Fluid **reveals its creation timestamp**, which may be considered sensitive data in certain contexts.

### Example Value

- `long` value: `29998545287255040` (17-19 digits)
- Alphanumeric encoding: `02DOPDAYO1o` (11 alphanumeric characters)

### Example Usage

Once registered, the ID generator is accessible from anywhere:

```cs
public void ExampleUse()
{
	long id = IdGenerator.Current.CreateId(); // 29998545287255040
	
	// For a more compact representation, IDs can be encoded in alphanumeric
	string compactId = id1.ToAlphanumeric(); // "1dw14L86uHcPoQJd"
	decimal originalId = IdEncoder.GetLongOrDefault(compactId)
		?? throw new ArgumentException("Not a valid encoded ID.");
}

// If you prefer to use an explicitly injected generator
public void ExampleUseWithInjectedGenerator(IIdGenerator idGenerator)
{
	long id = idGenerator.CreateId(); // 29998545287255040
}
```

The generator is registered on startup. It needs a small, unique ID for the current application instance, in order to generate IDs that are strictly different from those generated by other applications or servers. This is achieved by storing a unique ID in a central storage location until application shutdown.

Support for various databases is built-in, as well as for Azure Blob Storage (by installing an [additional package](https://www.nuget.org/packages/Architect.Identities.Azure)).

```cs
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
	// Provide a source where our application can claim a unique ID for itself
	// Using SQL Server, MySQL, SQLite, Standard SQL, or even Azure Blob Storage
	services.AddApplicationInstanceIdSource(source =>
		source.UseSqlServer(() => new SqlConnection("ConnectionString")));

	// Register Fluid as the ID generator
	services.AddIdGenerator(generator => generator.UseFluid());
}

public void Configure(IApplicationBuilder applicationBuilder)
{
	// Rent a unique ID for the current application instance (and return it on shutdown)
	// This helps guarantee generation of different IDs from other applications/servers
	applicationBuilder.UseApplicationInstanceIdSource();
	
	// Optional: Make IdGenerator.Current available
	applicationBuilder.UseIdGenerator();
}
```

Alternatively, if we happen to be using Entity Framework, we can use the DbContext directly (by installing an [additional package](https://www.nuget.org/packages/Architect.Identities.EntityFramework)). Just make sure to call the appropriate method for your database: `UseSqliteDbContext`, `UseSqlServerDbContext`, etc.

```cs
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
	// Register the DbContext
	services.AddPooledDbContextFactory<ExampleDbContext>(context =>
		context.UseSqlite(new SqliteConnection("Filename=:memory:")));
	
	// Provide a source where our application instance can claim a unique ID for itself
	// Using the DbContext's database (with SQL Server, MySQL, SQLite, or Standard SQL)
	services.AddApplicationInstanceIdSource(source =>
		source.UseSqliteDbContext<ExampleDbContext>());
	
	// Register Fluid as the ID generator
	services.AddIdGenerator(generator => generator.UseFluid());
}

public void Configure(IApplicationBuilder applicationBuilder)
{
	// Rent a unique ID for the current application instance (and return it on shutdown)
	// This helps guarantee generation of different IDs from other applications/servers
	applicationBuilder.UseApplicationInstanceIdSource();
	
	// Optional: Make IdGenerator.Current available
	applicationBuilder.UseIdGenerator();
}
```

In unit tests, no registration is required:

```cs
[Fact]
public void ShowNoRegistrationRequiredInUnitTests()
{
	long id = IdGenerator.Current.CreateId(); // 29998545287255040
}
```

Even without an injected generator, the ID generation can be controlled from the outside, such as in unit tests that require constant IDs:

```cs
[Fact]
public void ShowInversionOfControl()
{
	// A custom generator is included
	const long fixedId = 1;
	using (new IdGeneratorScope(new CustomIdGenerator(() => fixedId)))
	{
		var entity = new Entity(); // Constructor uses IdGenerator.Current.CreateId()
		Assert.Equal(fixedId, entity.Id); // True
		
		// A simple incremental generator is included as well
		using (new IdGeneratorScope(new IncrementalIdGenerator()))
		{
			Assert.Equal(1L, IdGenerator.Current.CreateId()); // True
			Assert.Equal(2L, IdGenerator.Current.CreateId()); // True
			Assert.Equal(3L, IdGenerator.Current.CreateId()); // True
		}
		
		Assert.Equal(fixedId, IdGenerator.Current.CreateId()); // True
	}
}
```

### Benefits

- Is incremental, making it efficient as a primary key.
- Fits within a 64-bit integral type, making it extremely efficient as a primary key.
- Guarantees uniqueness (see Trade-offs for preconditions).
- Like a UUID, does not require database insertion to determine the ID, nor reading the ID back in after insertion (as with auto-increment).
- Consists of digits only.
- Can be encoded as 11 alphanumeric characters, for a shorter representation.
- Uses the common `long` (`Int64`) type (`BIGINTEGER` in SQL databases), which is intuitively and efficiently represented, sorted, and manipulated in .NET and databases.
- Supports comparison operators (unlike UUIDs, which make comparisons notoriously hard to write using the Entity Framework).
- Is suitable for use in URLs.
- Can by selected (such as for copying) by double-clicking, as it consists of only word characters in all supported representations.

### Trade-offs

- Requires dependency registration on startup (but not in unit tests).
- Requires a synchronization mechanism on startup to guarantee uniqueness (such as the database that is already used by the application, or Azure blob storage).
- Requires clock synchronization (such as through NTP).
- Reveals its creation timestamp in milliseconds (see Attack Surface).
- Reveals very minor information about which instance created the ID and how many IDs it has created this second (see Attack Surface).
- Is rate-limited to 1,024 generated IDs per millisecond (i.e. 1 million IDs per second) per application.
- Is intended to be unique within a chosen context rather than globally.

### Structure

- Is represented as a positive `long` or `ulong`.
- Occcupies 8 bytes in memory.
- Is represented as `BIGINTEGER` in SQL databases.
- Requires 8 bytes of storage in SQL databases, comparable to an auto-increment ID.
- Can be represented in a natural, workable form by SQL databases, being a simple integral type. (By contrast, not all databases have a UUID type, requiring the use of binary types, which make manual queries cumbersome.)
- Is ordered intuitively and consistently in .NET and databases. (By contrast, SQL Server orders UUIDs starting with some of the middle bytes, making it very hard to implement an ordered UUID type.)
- Can be represented numerically, i.e. as up to 19 digits.
- Can be represented alphanumerically, i.e. as exactly 11 alphanumeric characters.
- Contains the number of milliseconds since a custom epoch in its first 43 bits.
- Contains a unique application instance ID in its middle 11 bits (see [ApplicationInstanceIdSource](#application-instance-id-sources)), allowing applications to generate unique IDs simultaneously.
- Contains a counter value in its last 10 bits, allowing multiple IDs to be generated during a single millisecond.
- Can represent timestamps beyond the year 2150.
- The epoch can be changed, allowing future applications to start with the same number of years of capacity.
- The bit distribution can be tweaked, allowing a custom balance between years of capacity, number of applications, and IDs per millisecond.

### Clock Synchronization

Because a Fluid contains a time component, it relies on the host system's clock. This would introduce the risk of collisions if the clock were to be adjusted backwards. To counter this, the generator will allow the clock to catch up if necessary (i.e. if the last generated value had a timestamp _greater_ than the current timestamp), by up to several seconds. However, the host system is responsible for keeping potential clock adjustments under a few seconds. This is generally achieved by **having the system clock synchronized using the Network Time Protocol (NTP)**, which is a recommended practice in general. (Note that the timestamps are based on UTC, so daylight savings adjustments are a non-issue.)

### Collision Resistance

Fluids avoid collisions entirely, between all application instances that share a synchronization mechanism. Beyond those boundaries, they are not designed to be unique.

Fluids generated by a single application instance do not collide. The generator will never repeat a `{Timestamp, Counter}` pair. If necessary, the generator will wait for the clock to advance to guarantee this. If it is forced to wait unreasonably long, it will throw an exception. This only happens if the clock is turned back several seconds, a situation that is generally avoided.

Fluids generated by application instances that share a synchronization mechanism do not collide. Each instance has a distinct application instance ID, which makes up part of any ID it generates. This application instance ID is reserved on application startup and relinquised on shutdown. Particularly when the database is used as the synchronization mechanism, it leads to a natural boundary: Applications instances that write IDs to the same database also use the same database as their synchronization mechanism, thus preventing collisions between them.

Fluids generated by application instances that do _not_ share a synchronization mechanism are free to collide. For example, a set of microservices responsible for Account Management may share a database table for their synchronization. Another set of microservices, responsible for Sales, may share their own table for synchronization. Inside each context, generated IDs are unique. Between the contexts, IDs have nothing to do with each other. Just like the Sales context would not mix IDs generated by a third-party application with its own, it has no reason to mix Account Management IDs with its own: they identify different concepts.

### Guessability

Fluids are not designed to be hard to guess, although they are by no means easy to guess.

Guessing a Fluid requires guesssing the creation timestamp in milliseconds (presupposing knowledge of the correct epoch), the application instance ID, and the counter value. Assuming there is no High Availability and there is only one application, the application instance ID is likely to be 1. The counter keeps rotating even between timestamps and can have 1024 different values. That makes the chance to guess a Fluid 1 in `1 * 1024 * ChanceToGuessTimestampInMilliseconds`.

As an example, with an application consistently generating 1 ID every second, the chance is 1 in `1 * 1024 * 1000`, or 1 in 1 million.

To reduce guessability to 1/2^128, see [PublicIdentities](#public-identities).

### Application Instance ID Sources

_While created to support the Fluid ID generator, the application instance ID sources can also be used as their own feature._

Multiple applications may be writing IDs to the same table, e.g. a web application and a background job server. Also, each application may be hosted by multiple servers, for High Availability purposes. As such, it makes sense to speak of application instances.

The Fluid ID generator uses a unique identifier assigned to the current instance of the current application, or the current process, if you will. This ensures that no collisions occur between IDs generated by different application instances within the same context. The context is defined by which application instances share an application instance ID source.

The package offers various application instance ID sources. Most sources use some form of centralized external storage. Such sources claim an ID on startup and relinquish it again on shutdown. Of course, the occasional unclean shutdown may cause an application instance ID or two to linger. However, the default bit distribution allows for 2048 different application instance IDs, leaving plenty of room. Still, you can manually delete registrations from the external storage, should the need arise.

- `UseFixedSource`. This source allows you to manually provide the application instance ID. It is recommended only for integration tests.
- `UseSqlServer`. This source uses a SQL Server or Azure SQL database, creating the application instance ID tracking table if it does not yet exist.
- `UseMySql`. This source uses a MySQL database, creating the application instance ID tracking table if it does not yet exist.
- `UseSqlite`. This source uses a SQLite databases, creating the application instance ID tracking table if it does not yet exist.
- `UseStandardSql`. This source works with most SQL databases, allowing other databases to be used without the need for custom extensions. However, since table creation syntax rarely follows the standard, this method throws an exception if the required table does not exist. The exception provides an example `CREATE TABLE` query to guide you in the right direction.
- `UseSqlServerDbContext`, `UseMySqlDbContext`, `UseSqliteDbContext`, `UseStandardSqlDbContext`. Offered in a [separate package](https://www.nuget.org/packages/Architect.Identities.EntityFramework). These sources work like their counterparts above, except they make use of a given DbContext. The DbContext's configuration is honored (e.g. retryable execution strategies). Support is included for `AddDbContextFactory` and `AddPooledDbContextFactory` (available since EF Core 5.0.0), although lower versions of EF Core are supported as well.
- `UseAzureBlobStorageContainer`. Offered in a [separate package](https://www.nuget.org/packages/Architect.Identities.Azure). This source uses an Azure blob storage container to store application instance IDs that are in use.

Third-party libraries may provide additional sources through further extension methods.

### Attack Surface

We know that a Fluid does not leak volume information like an auto-increment ID does. Still, since it is hard to reveal as little as a fully random ID, we should consider what information we are revealing.

- Most obviously, the timestamp component reveals the creation datetime.
- Next, the application instance ID component reveals: "there are _likely_ to be at least this many application instances within the bounded context".
- Given sufficient determination and the ability to obtain new IDs at will, an attacker could determine which IDs are created by the same applications, and how many instances of such applications are active.
- Given sufficient determination and the ability to obtain new IDs at will, an attacker could determine at which rate activate instances are _currently_ producing IDs. Note that applications tend to create various things, and this does not reveal how the production rates are distributed between them.
