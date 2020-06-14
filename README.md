# Architect.Identities

This package provides tools for ID management.

#TODO: Table of Contents

## TLDR

Auto-increment IDs expose sensitive information. UUIDs (also known as GUIDs) are inefficient as primary keys in a database. Having two IDs is silly and cumbersome.

- For a 93-bit UUID replacement that is *efficient as a primary key* and has *virtually no caveats*, use **CompanyUniqueId**.
- For a 64-bit UUID replacement that is *extremely efficient as a primary key* but *requires dependency registration and a synchronization mechanism*, use **Fluid**.
- To expose IDs externally in a sensitive environment *where zero metadata can be leaked*, transform them with **PublicIdentities**.
- To assign a unique ID to each distinct application or instance thereof, use **ApplicationInstanceIdSource**.

## Introduction

Many applications assign IDs to their entities. Unfortunately, most ID schemes leave something to be desired. Auto-increment IDs are highly efficient as primary keys in a database, but expose sensitive information (row count approximations). UUIDs usually reveal nothing, but are large and usually random, leading to extremely poor performance when used as primary keys.

The **CompanyUniqueId** aims to maintain the advantages of a UUID while adding the incremental property that makes it efficient as a primary key.

The **Fluid** aims to replace the auto-increment ID, being extremely efficient as a primary key, while adding the property of not exposing sensitive information.

Additionally, for sensitive scenarios where zero metadata may be leaked, **PublicIdentities** can transform an ID into a public representation that reveals nothing, without introducing an unrelated secondary ID.

Finally, there are cases where it is useful to assign unique IDs to applications (or instances thereof, in High Availability scenarios). An **ApplicationInstanceIdSource** accomplishes this. It is used by the Fluid ID generator, to guarantee uniqueness.

#TODO: Remove

The **CompanyUniqueId** replaces a UUID/GUID as a unique ID generated on-the-fly, but is also incremental and suitable for use as a primary key in databases.

The **Fluid** ID generator, which generates **F**lexible, **L**ocally-**U**nique **ID**s, generates IDs intended to replace the combination of auto-increment ID and UUID. It is 64-bit and incremental (i.e. efficient as a primary key) and does not leak the sensitive information that an auto-increment ID does.

**PublicIdentities** is a set of tools for converting local IDs to public IDs. When a short, numeric ID (such as a Fluid) is still considered to leak too much information to be exposed publically, PublicIdentities provides an alternative. It converts 64-bit (or smaller) IDs into deterministic, reversible public IDs that are indistinguishable from random noise without possession of the configured key. Using the key, public IDs can be converted back to the original IDs. These can take the place of UUIDs, but without the additional bookkeeping.

Various **ApplicationInstanceIdSource** implementations provide a unique ID to each distinct application (or instance thereof) within a chosen bounded context, by using a centralized storage component, such as a SQL database or an Azure Blob Storage Container. The Fluid ID generator relies on this feature to ensure that generated IDs are unique.

## CompanyUniqueId

A CompanyUniqueId is a UUID replacement represented as a `decimal`. It can be generated on-the-fly without any prerequisites, and is much more efficient as a primary key in databases than a random UUID.

Note that a CompanyUniqueId exposes its creation timestamp, which may be considered sensitive data in certain contexts.

#### Example

- Regular representation: `448147911486426236008828585` (27-28 digits)
- Short representation: `1dw14L86uHcPoQJd` (16 alphanumeric characters)

#### Example Usage

```
// Generating an ID
decimal id = CompanyUniqueId.CreateId(); // 448147911486426236008828585

// Compactly encoding an ID (if a shorter ID is required)
string compactId = CompanyUniqueId.ToShortString(id); // "1dw14L86uHcPoQJd"

// Decoding an ID from a request URL/body, loading the corresponding entity, and returning 404 if the ID or entity is nonexistent or not the user's
decimal decodedId = CompanyUniqueId.FromStringOrDefault(inputFromRequest) ?? 0m;
var entity = this.Repository.GetEntityById(decodedId);
if (entity is null || entity.UserId != currentUserId) return this.NotFound();
```

#### Benefits

- Is incremental, making it significantly more efficient as a primary key.
- Is shorter than a UUID, making it more efficient as a primary key.
- Like UUIDs, can be generated on-the-fly with no registrations whatsoever.
- Like UUIDs, makes collisions extremely unlikely.
- Consists of digits only.
- Can be encoded as 16 alphanumeric characters, for a shorter representation.
- Uses the common decimal type, which is easily represented and manipulated in .NET and most databases (which is not always the case for UUIDs).
- Is suitable for use in URLs.
- Can by selected (such as for copying) by double-clicking, as it consists of only word characters in all supported representations.

#### Trade-offs

- Exposes the creation timestamp in milliseconds.
- Is rate-limited to 1,000 generated IDs per millisecond (i.e. 1 million IDs per second) per application.
- Is company-unique rather than globally unique.
- Still exceeds 64 bits, the common CPU register size. (For an extremely efficient option that fits in 64 bits, see **Fluid**.)

#### Structure

- Is represented as a positive `decimal` of up to 28 digits, with 0 decimal places.
- Occcupies 16 bytes in memory.
- Is represented as `DECIMAL(28, 0)` in SQL databases.
- Requires 13 bytes of storage in many SQL databases, including SQL Server and MySQL. (This is more compact than a UUID, which requires 16 bytes.)
- Can be represented in a natural, workable form by most SQL databases, being a simple decimal type. (By contrast, not all databases have a UUID type, requiring the use of binary types, which make manual queries cumbersome.)
- Is ordered intuitively and consistently in .NET and most databases. (By contrast, SQL Server orders UUIDs starting with some of the middle bytes, making it very hard to implement an ordered UUID type.)
- Can be represented numerically, i.e. as up to 28 digits.
- Can be represented alphanumerically, i.e. as exactly 16 alphanumeric characters.
- Contains the number of milliseconds since the epoch in its first 45 bits.
- Contains a cryptographically-secure pseudorandom sequence in its last 48 bits.
- Uses 32-bit pseudorandom increments to remain incremental intra-millisecond.
- Can represent timestamps beyond the year 3000.

#### Collision Resistance

CompanyUniqueIds have strong collision resistance.

Collisions between different timestamps are impossible, since the millisecond values differ.

Within a single application, collisions during a particular millisecond are avoided (while maintaining the incremental nature) by reusing the previous random value (48 bits) and incrementing it by a smaller random value (32 bits). Combined with the rate limiting, this guarantees unique IDs within the application, as long as the clock is not rewinded.

If the system clock is rewinded, until it has caught up, collisions are _technically_ possible on timestamps for which IDs have been previously generated. The probabilities are extremely low, the same as for separate applications generating values simultaneously, as described next.

Between separate applications, collisions during a particular millisecond are technically possible, with extremely low probability. First, two applications need to be generating IDs on the same timestamp. Granted that this occurs, if 24,000 servers were to each generate one ID with the same timestamp, the odds of a collision are about 1/1M (following the [birthday paradox](https://en.wikipedia.org/wiki/Birthday_problem#Probability_table)). This means 1 collision per 24 billion IDs on average, in this degenerate scenario.

Realistically, the probability is drastically lower. Most companies do not have 24,000 servers, let alone all generating an ID during the same millisecond. If the same 24,000 IDs were generated by 24 servers instead (each generating 1,000), then the IDs from any one server could not collide with one another, drastically lowering the odds. If the same 24,000 IDs were spread out across several milliseconds, the IDs from different timestamps could not collide with one another, again drastically lowering the odds.

CompanyUniqueId's collision resistance makes it practically unique company-wide for almost all companies. On a global scale, with extensive use, some collisions can be expected, which is not something the design aims to avoid.

Note that company-wide uniquess exceeds what is generally needed in practice. Just like you would not assume that another company's IDs never collide with your own, a company's applications tend to be grouped into separate bounded contexts that treat each other as external. Usually, the requirement is uniqueness within the context. Since CompanyUniqueIds overdeliver in this regard, they provide an extremely large safety margin, rendering the chance of collisions negligible.

#### Guessability

Presuming knowledge of a timestamp on which an ID was generated, the probability of guessing an ID is 1/2^48, thanks to the 48-bit cryptographically-secure pseudorandom sequence. In practice, the millisecond timestamp component tends to reduce the guessability, since there might not be any IDs generated on a particular timestamp.

Given a particular ID, _if_ subsequent IDs were generated by the same application during the same millisecond, the guessability is greater. For example, this might happen during batch processing for multiple tenants. A tenant could take one of their ID values and assume that data for another tenant was created directly after. The probability of guessing the next ID for that timestamp, if one exists, is 1/2^32. The odds of 1 in 4 billion are considered hard enough, as security through obscurity must not be the deciding factor.

To reduce guessability to 1/2^128, see **PublicIdentities**.

## Fluid

The **F**lexible, **L**ocally-**U**nique **ID** is a 63-bit ID value guaranteed to be unique within its configured context. It is extremely efficient as a primary key, and it avoids leaking the sensitive information that an auto-increment ID does.

An application using Fluids **must run on a clock-synchronized system** with clock adjustments of no more than 2 seconds. This is because if the clock is adjusted backwards right after an ID was generated, the system needs to wait for the clock to reach its original value again in order to guarantee ID uniqueness. In other words, once we generate an ID on a timestamp, we will no longer generate IDs on _earlier_ timestamps. ID generation will wait for up to 2 seconds to let the clock catch up, or throw an exception.

## Example

- Regular representation: `29998545287255040` (17-19 digits)
- Short representation: #TODO

## Example Usage

#TODO

#### Benefits

- Is incremental, making it efficient as a primary key.
- Fits within a 64-bit integral type, making it extremely efficient as a primary key.
- Guarantees uniqueness (see Trade-offs for preconditions).
- Consists of digits only.
- Can be encoded as 11 alphanumeric characters, for a shorter representation.
- Uses the common `ulong` (`UInt64`) type (`BIGINTEGER` in SQL databases), which is easily and efficiently represented and manipulated in .NET and databases.
- Is suitable for use in URLs.
- Can by selected (such as for copying) by double-clicking, as it consists of only word characters in all supported representations.

#### Trade-offs

- Requires dependency registration on startup (but not in unit tests).
- Requires a synchronization mechanism (such as the database that is already used by the application, or Azure blob storage).
- Requires clock synchronization (such as through NTP).
- Exposes the creation timestamp in milliseconds (see Attack Surface).
- Exposes very minor information about which instance created the ID and how many IDs it has created this second (see Attack Surface).
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
- Contains a unique application instance ID in its middle 11 bits (see ApplicationInstanceIdSource), allowing applications to generate unique IDs simultaneously.
- Contains a counter value in its last 10 bits, allowing multiple IDs to be generated during a single millisecond.
- Can represent timestamps beyond the year 2150.
- The epoch can be changed, allowing future applications to have the same number of years of capacity.
- The bit distribution can be tweaked, allowing a custom balance between years of capacity, number of applications, and IDs per millisecond.

#TODO: Left off here...

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
