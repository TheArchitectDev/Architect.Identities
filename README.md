# Architect.Identities

This package provides tools for ID management.

## TLDR

Auto-increment IDs reveal sensitive information. UUIDs (also known as GUIDs) are inefficient as primary keys in a database. Having two different IDs is cumbersome and counterintuitive.

- For a 96-bit UUID replacement that is *efficient as a primary key* and has *virtually no caveats*, use **[CompanyUniqueId](#company-unique-ids)**.
- For a 64-bit UUID replacement that is *extremely efficient as a primary key* but *requires dependency registration and a synchronization mechanism*, use **[Fluid](#fluid)**.
- To expose IDs externally in a sensitive environment *where zero metadata can be leaked*, transform them with **[PublicIdentities](#public-identities)**.
- To assign a unique ID to each distinct application or instance thereof, use **[ApplicationInstanceIdSource](#application-instance-id-sources)**.

## Introduction

Many applications assign IDs to their entities. Unfortunately, most ID schemes leave something to be desired. Auto-increment IDs are highly efficient as primary keys in a database, but reveal sensitive information (row count approximations). UUIDs generally reveal nothing, but are large and usually random, leading to extremely poor performance when used as primary keys.

The **[CompanyUniqueId](#company-unique-ids)** aims to maintain the advantages of a UUID while adding the incremental property that makes it efficient as a primary key.

The **[Fluid](#fluid)**, or **F**lexible, **L**ocally-**U**nique **ID**, aims to replace the auto-increment ID, being extremely efficient as a primary key, while adding the property of not revealing sensitive information.

Additionally, for sensitive scenarios where zero metadata may be leaked, **[PublicIdentities](#public-identities)** can transform an ID into a public representation that reveals nothing, without ever introducing an unrelated secondary ID.

Finally, there are cases where it is useful to assign unique IDs to applications (or instances thereof, in High Availability scenarios). An **[ApplicationInstanceIdSource](#application-instance-id-sources)** accomplishes this. It is used by the Fluid ID generator, to guarantee uniqueness.

## Which type of ID should I use?

Prefer the [Fluid](#fluid), as it is the most compact (i.e. human-friendly) and the most efficient (i.e. machine-friendly).

When to use the [CompanyUniqueId](#company-unique-ids) instead:

- If you are unsure whether your servers are running a clock synchronization mechanism.
- If you need IDs to be practically impossible to guess. (Beware of security through obscurity.)
- If you expect to use more than 1,000 application instances that must generate mutually unique IDs. (The hard limit is 2,048.)
- If the application lacks access to either a SQL database or Azure. More effort would be required to provide the Fluid's synchronization mechanism.
- If the application lacks Dependency Injection.

## Company-Unique IDs

A CompanyUniqueId is a UUID replacement represented as a `decimal`. It can be generated on-the-fly without any prerequisites, and is much more efficient than a random UUID as a primary key in a database.

Note that a CompanyUniqueId reveals its creation timestamp, which may be considered sensitive data in certain contexts.

#### Example

- `decimal` value: `448147911486426236008828585` (27-28 digits)
- Alphanumeric encoding: `1dw14L86uHcPoQJd` (16 alphanumeric characters)

#### Example Usage

```cs
decimal id = CompanyUniqueId.CreateId(); // 448147911486426236008828585

// For a more compact representation, IDs can be encoded to alphanumeric
string compactId = id.ToAlphanumeric(); // "1dw14L86uHcPoQJd"
decimal originalId = IdEncoder.GetDecimalOrDefault(compactId)
	?? throw new ArgumentException("Not a valid encoded ID.");
```

Use `DECIMAL(28, 0)` to store a CompanyUniqueId in a SQL database.

#### Benefits

- Is incremental, making it significantly more efficient as a primary key.
- Is shorter than a UUID, making it more efficient as a primary key.
- Like UUIDs, can be generated on-the-fly with no registrations whatsoever.
- Like UUIDs, makes collisions extremely unlikely.
- Consists of digits only.
- Can be encoded as 16 alphanumeric characters, for a shorter representation.
- Uses the common `decimal` type, which is intuitively represented, sorted, and manipulated in .NET and most databases (which cannot be said for UUIDs).
- Is known during entity construction, unlike database-generated IDs.
- Is suitable for use in URLs.
- Can by selected (such as for copying) by double-clicking, as it consists of only word characters in both its numeric and alphanumeric form.

#### Trade-offs

- Reveals its creation timestamp in milliseconds.
- Is rate-limited to 1,000 generated IDs per millisecond (i.e. 1 million IDs per second) per application instance.
- Is company-unique rather than globally unique.
- Still exceeds 64 bits, the common CPU register size. (For an extremely efficient option that fits in 64 bits, see **[Fluid](#fluid)**.)

#### Structure

- Is represented as a positive `decimal` of up to 28 digits, with 0 decimal places.
- Occcupies 16 bytes in memory.
- Is represented as `DECIMAL(28, 0)` in SQL databases.
- Requires 13 bytes of storage in many SQL databases, including SQL Server and MySQL. (This is more compact than a UUID, which requires 16 bytes.)
- Can be represented in a natural, workable form by most SQL databases, being a simple `decimal` type. (By contrast, not all databases have a UUID type, requiring the use of binary types, which make manual queries cumbersome.)
- Is ordered intuitively and consistently in .NET and most databases. (By contrast, SQL Server orders UUIDs by some of the _middle_ bytes, making it very hard to implement an ordered UUID type.)
- Can be represented numerically, as up to 28 digits.
- Can be represented alphanumerically, as exactly 16 alphanumeric characters.
- Contains the number of milliseconds since the epoch in its first 45 bits.
- Contains a cryptographically-secure pseudorandom sequence in its last 48 bits.
- Uses 32-bit pseudorandom increments to remain incremental intra-millisecond.
- Can represent timestamps beyond the year 3000.

#### Collision Resistance

CompanyUniqueIds have strong collision resistance.

Collisions between different timestamps are impossible, since the millisecond values differ.

Within a single application, collisions during a particular millisecond are avoided (while maintaining the incremental nature) by reusing the previous random value (48 bits) and incrementing it by a smaller random value (32 bits). Combined with the rate limiting, this guarantees unique IDs within the application instance, as long as the clock is not rewinded.

If the system clock is rewinded, until it has caught up, collisions are _technically_ possible on timestamps for which IDs have been previously generated. The probabilities are extremely low, the same as for separate applications generating values simultaneously, as described next.

Between separate applications, collisions during a particular millisecond are technically possible, with extremely low probability. First, two applications need to be generating IDs with the same millisecond timestamp. Granted that this occurs, if **24,000 servers** were to each generate one ID with the same timestamp, the odds of a collision are about 1/1M (following the [birthday paradox](https://en.wikipedia.org/wiki/Birthday_problem#Probability_table)). This means **1 collision per 24 billion IDs on average**, in this degenerate scenario.

Realistically, the probability is even lower. Most companies do not have 24,000 servers, let alone all generating an ID during the same millisecond. If the same 24,000 IDs with the same millisecond timestamp were generated by 24 servers instead (each generating 1,000), then IDs generated by the same server could not collide with one another, lowering the odds. If the same 24,000 IDs were spread out over several milliseconds, the IDs from different timestamps could not collide with one another, further lowering the odds.

CompanyUniqueId's collision resistance makes it practically unique company-wide for almost all companies. On a global scale, with extensive use, some collisions can be expected, which is not something the design aims to avoid.

Note that the property of company-wide uniquess exceeds what is generally needed in practice. Just like you would not assume that another company's IDs never collide with your own, a company's applications tend to be grouped into separate bounded contexts that treat each other as external. Usually, the requirement is uniqueness within the context. Since CompanyUniqueIds overdeliver in this regard, they provide an extremely large safety margin, rendering the chance of collisions negligible.

#### Guessability

Presuming knowledge of a timestamp on which an ID was generated, the probability of guessing an ID is 1/2^48, thanks to the 48-bit cryptographically-secure pseudorandom sequence. In practice, the millisecond timestamp component tends to reduce the guessability, since there might not be any IDs generated on a particular timestamp.

Given a particular ID, _if_ subsequent IDs were generated by the same application during the same millisecond, the guessability is greater. For example, this might happen during batch processing for multiple tenants. A tenant could take one of their ID values and assume that data for another tenant was created directly after. The probability of guessing the next ID for that timestamp, if one exists, is 1/2^32. The odds of 1 in 4 billion are considered hard enough, as security through obscurity must not be the deciding factor.

To reduce guessability to 1/2^128, see **[PublicIdentities](#public-identities)**.

#### Attack Surface

A CompanyUniqueId reveals its creation timestamp. Otherwise, it consists of cryptographically-secure pseudorandom data, i.e. nothing sensitive.

## Fluid

The **F**lexible, **L**ocally-**U**nique **ID** is a 64-bit ID value guaranteed to be unique within its configured context. It is extremely efficient as a primary key, and it avoids leaking the sensitive information that an auto-increment ID does.

An application using Fluids **must run on a clock-synchronized system** with clock adjustments of no more than 5 seconds. This is because if the clock is adjusted backwards right after an ID was generated, the system needs to wait for the clock to reach its original value again in order to guarantee ID uniqueness. In other words, once we generate an ID on a timestamp, we will no longer generate IDs on _earlier_ timestamps. ID generation will wait for up to 5 seconds to let the clock catch up, or throw an exception.

## Example

- `long` value: `29998545287255040` (17-19 digits)
- Alphanumeric encoding: `02DOPDAYO1o` (11 alphanumeric characters)

## Example Usage

```cs
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
	// Avoid collisions between servers or application instances (using SQL Server, MySQL, or even Azure Blob Storage)
	services.AddApplicationInstanceIdSource(source =>
		source.UseSqlServer(() => new SqlConnection("ConnectionString")));

	// Register Fluid as the ID generation mechanism
	services.AddIdGenerator(generator => generator.UseFluid());
}

public void Configure(IApplicationBuilder applicationBuilder)
{
	// Make IdGeneratorScope.Current available
	applicationBuilder.UseIdGeneratorScope();
}
```

```cs
public void ExampleUse()
{
	long id = IdGeneratorScope.Current.Generator.CreateId(); // 29998545287255040
	
	// For a more compact representation, IDs can be encoded to alphanumeric
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

In unit tests, no registration is required:

```cs
[Fact]
public void ShowNoRegistrationRequiredInUnitTests()
{
	long id = IdGeneratorScope.Current.Generator.CreateId(); // 29998545287255040
}
```

Even without an injected generator, the ID generation can be controlled from the outside, such as in unit tests that require constant IDs:

```cs
[Fact]
public void ShowInversionOfControl()
{
	const long fixedId = 1;
	using (new IdGeneratorScope(new CustomIdGenerator(fixedId)))
	{
		var entity = new Entity(); // Implementation uses IdGeneratorScope.Current
		
		Assert.Equal(fixedId, entity.Id); // True
	}
}
```

#### Benefits

- Is incremental, making it efficient as a primary key.
- Fits within a 64-bit integral type, making it extremely efficient as a primary key.
- Guarantees uniqueness (see Trade-offs for preconditions).
- Consists of digits only.
- Can be encoded as 11 alphanumeric characters, for a shorter representation.
- Uses the common `long` (`Int64`) type (`BIGINTEGER` in SQL databases), which is intuitively and efficiently represented, sorted, and manipulated in .NET and databases.
- Is known during entity construction, unlike database-generated IDs.
- Is suitable for use in URLs.
- Can by selected (such as for copying) by double-clicking, as it consists of only word characters in all supported representations.

#### Trade-offs

- Requires dependency registration on startup (but not in unit tests).
- Requires a synchronization mechanism (such as the database that is already used by the application, or Azure blob storage).
- Requires clock synchronization (such as through NTP).
- Reveals its creation timestamp in milliseconds (see Attack Surface).
- Reveals very minor information about which instance created the ID and how many IDs it has created this second (see Attack Surface).
- Is rate-limited to 1,024 generated IDs per millisecond (i.e. 1 million IDs per second) per application.
- Is context-unique rather than globally unique.

#### Structure

- Is represented as a positive `decimal` of up to 28 digits, with 0 decimal places.
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
- The epoch can be changed, allowing future applications to have the same number of years of capacity.
- The bit distribution can be tweaked, allowing a custom balance between years of capacity, number of applications, and IDs per millisecond.

#### Collision Resistance

Fluids generated by an application instance do not collide. The generator will never repeat a `{Timestamp, Counter}` pair. If necessary, the generator will wait for the clock to advance to guarantee this. If it is forced too wait unreasonably long, it will throw an exception. This only happens if the clock is rewound several seconds, which is generally avoided anyway.

Fluids generated by application instances that share a synchronization mechanism do not collide. Each instance has a distinct application instance ID, which makes up part of any ID it generates. Particularly when the database is used as the synchronization mechanism, this leads to a natural boundary: Applications instances that write IDs to the same database also use the same database as their synchronization mechanism, thus preventing collisions between them.

Fluids generated by application instances that do _not_ share a synchronization mechanism are free to collide.

#### Guessability

Fluids are not designed to be hard to guess, although they are by no means easy to guess.

Guessing a Fluid requires guesssing the timestamp in milliseconds (assuming knowledge of the correct epoch), the application instance ID, and the counter value. Assuming there is no high availability and there are no different applications, the application instance ID is likely to be 1. The counter keeps rotating even between timestamps and can have 1024 different values. That makes the chance to guess a Fluid 1 in `1 * 1024 * ChanceToGuessTimestampInMilliseconds`.

With an application generating 1 ID every second, consistently, the chance is 1 in `1 * 1024 * 1000`, or 1 in 1 million.

To reduce guessability to 1/2^128, see **[PublicIdentities](#public-identities)**.

#### Application Instance ID Sources

_While created to support the Fluid ID generator, the application instance ID sources can also be used as their own feature._

Multiple applications may be writing IDs to the same table, e.g. a web application and a background job server. Also, each application may be hosted by multiple servers, for High Availability (HA) purposes. As such, it makes sense to speak of application instances.

The Fluid ID generator uses a unique identifier assigned to the current instance of the current application. This ensures that no collisions occur between IDs generated by different application instances within the same context. In this case, the context is shaped by application instances sharing a synchronization mechanism.

The package offers various application instance ID sources. Most sources use some form of centralized external storage. Such sources claim an ID on startup and release it again on shutdown. Of course, the occasional unclean shutdown may cause an application instance ID or two to linger. However, the default bit distribution allows for 2048 different application instance IDs, leaving plenty of room. Still, you can manually delete registrations from the external storage, should the need arise.

- `UseFixedSource`. This source allows you to manually provide the application instance ID.
- `UseSqlServer`. This source uses a SQL Server or Azure SQL database, creating the application instance ID tracking table if it does not yet exist.
- `UseMySql`. This source uses a MySQL database, creating the application instance ID tracking table if it does not yet exist.
- `UseStandardSql`. This source works with most SQL databases, allowing other databases to be used without the need for custom extensions. However, since table creation syntax rarely follows the standard, this method throws an exception if the required table does not exist. The exception provides an example `CREATE TABLE` query to guide you in the right direction.
- `UseAzureBlobStorageContainer`. Offered in a [separate package](https://www.nuget.org/packages/Architect.Identities.Azure). This source uses an Azure blob storage container to store application instance IDs that are in use.

Third party libraries may provide additional sources through further extension methods.

#### Clock Synchronization

Because a Fluid contains a time component, it relies on the host system's clock. This would introduce the risk of collisions if the clock were to be adjusted backwards. To counter this, the generator will allow the clock to catch up if necessary (i.e. if the last generated value has a timestamp _greater_ than the current timestamp), by up to several seconds. However, the host system is responsible for keeping potential clock adjustments under a few seconds. This is generally achieved by **having the system clock synchronized using the Network Time Protocol (NTP)**, which is a recommended practice in general. (Note that the timestamps are based on UTC, so daylight savings adjustments are of no concern.)

#### Attack Surface

We know that a Fluid does not leak volume information like an auto-increment ID does. Still, since it is hard to reveal as little as a fully random ID, we should consider what information we are revealing.

- Most obviously, the timestamp component reveals the entity's creation datetime.
- Next, the application instance ID component reveals: "there are _likely_ at least this many application instances within the bounded context".
- Given sufficient determination and the ability to obtain new IDs at will, an attacker could determine which entities are created by the same applications, and how many instances of such applications are active.
- Given sufficient determination and the ability to obtain new IDs at will, an attacker could determine at which rate activate instances are _currently_ producing IDs. Note that applications tend to create various entities, and this does not reveal how the production rates are distributed between them.

## Public Identities

Sometimes, revealing even the creation timestamp is too much. For example, if the ID represents a bank account, that makes sense.

Still, it would be good to have only one ID, and one that is efficient as a primary key, at that. To achieve that, we can create a public representation of that ID - one that reveals nothing.

#### Example

- The regular ID can be any `long`, `ulong`, or `decimal`, from a Fluid or CompanyUniqueId to an auto-increment ID: `29998545287255040`
- Public `Guid` value: `32f0edac-8063-2c68-5c43-c889b058556e` (16 bytes)
- Alphanumeric encoding: `EqUxdTU1Ih27v7dmQVilag` (22 alphanumeric characters)

#### Example Usage

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
	long id = IdGeneratorScope.Current.Generator.CreateId(); // 29998545287255040
	
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
	long id = IdGeneratorScope.Current.Generator.CreateId(); // 29998545287255040
	
	// We can use Guid's own methods to get a hexadecimal representation
	Guid publicId = publicIdConverter.GetPublicRepresentation(id); // 32f0edac-8063-2c68-5c43-c889b058556e
	string publicIdString = publicId.ToString("N").ToUpperInvariant(); // "32F0EDAC80632C685C43C889B058556E" (32 chars)
	
	// Convert back to internal ID
	long originalId = publicIdConverter.GetLongOrDefault(new Guid(publicIdString))
		?? throw new ArgumentException("Not a valid ID.");
}
```

#### Implementation

Public identities are implemented by using AES encryption under a secret key. With the key, the original ID can be retrieved. _Without the key_, the data is indistinguishable from random noise. In fact, it is exactly the size of a UUID, and it can be formatted to look just like one, for that familiar feel.

Obviously, it is important that the secret key is kept safe. Moreover, the key must not be changed. Doing so would render any previously provided public identities invalid.

#### Forgery Resistance

Without possession of the key, it is extremely hard to forge a valid public identity.

When a public identity is converted back into the original ID, its structure is validated. If it is invalid, `null` or `false` is returned, depending on the method used.

For `long` and `ulong` IDs, the chance to forge a valid ID is 1/2^64. For `decimal` IDs, the chance is 1/2^32. Even if a valid value were to be forged, the resulting internal ID is unlikely to be an existing one.

Generally, when an ID is taken as client input, something is loaded based on that ID. As such, it is often best to simply turn an invalid public identity into a nonexistent local ID, such as 0:

```cs
// Any invalid input will result in id=0
long id = publicIdConverter.GetLongOrDefault(IdEncoder.GetGuidOrDefault(publicIdString) ?? Guid.Empty)
	?? 0L;

var entity = this.Repository.GetEntityById(id);
if (entity is null) return this.NotFound();
```
