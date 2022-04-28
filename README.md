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

## TLDR

The **[DistributedId](#distributed-ids)** is a single ID that combines the advantages of auto-increment IDs and UUIDs.

For sensitive scenarios where zero metadata must be leaked from an ID, **[PublicIdentities](#public-identities)** can transform any ID into a public representation that reveals nothing, without ever introducing an unrelated secondary ID.

## Introduction

Should entity IDs use UUIDs or auto-increment?

Auto-increment IDs are ill-suited for exposing publically: they leak hints about the row count and are easy to guess. Moreover, they are generated very late, on insertion, posing challenges to the creation of aggregates.

UUIDs, on the other hand, tend to be random, causing poor performance as database keys.

Using both types of ID on an entity is cumbersome and may leak a technical workaround into the domain model.

Luckily, we can do better.

## Distributed IDs

The DistributedId is a UUID replacement that is generated on-the-fly (without orchestration), unique, hard to guess, easy to store and sort, and highly efficient as a database key.

A DistributedId is created as a 93-bit decimal value of 27-28 digits, but can also be represented as a (case-sensitive) 16-char alphanumeric value or as a `Guid`.

Distributed applications can create unique DistributedIds with no synchronization mechanism between them. This holds true under almost any load. Even under extreme conditions, [collisions](#collision-resistance) (i.e. duplicates) tend to be far under 1 collision per 350 billion IDs generated.

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

- Is incremental (even intra-millisecond), making it _significantly_ more efficient as a primary key than a UUID.
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
- Throttles when the _sustained_ generation rate exceeds 128K IDs per second, per application replica. (Note that most cloud applications scale out rather than up, and do not have a single replica generate over 128K IDs per second.)
- Is designed to be unique within a chosen context rather than globally. (For most companies, the context could easily by the entire company's landscape.)
- Is slightly less efficient than a 64-bit integer.
- Requires a `TEXT` column type with SQLite, which truncates decimals to 8 bytes.

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
- Contains the number of milliseconds since the start of the year 1900 in its first 45 bits.
- Contains a cryptographically-secure pseudorandom sequence in its last 48 bits.
- Uses 41-bit cryptographically-secure pseudorandom increments to remain incremental even intra-millisecond.
- Can represent timestamps beyond the year 3000.

### Collision Resistance

DistributedIds have strong collision resistance. The probability of generating the same ID twice is neglible for almost all contexts.

Most notably, collisions across different timestamps are impossible, since the millisecond values differ.

Within a single application replica, collisions during a particular millisecond are avoided (while maintaining the incremental nature) by reusing the previous random value (48 bits) and incrementing it by a smaller random value (41 bits). This guarantees unique IDs within the application replica.

The scenario where collisions can occur is when multiple application replicas are generating IDs at the same millisecond. It is detailed below and should be negligible.

#### The degenerate worst case

The chances of a collision occurring have been measured. Under the worst possible circumstances, they are as follows:

- On average, with 2 application replicas at maximum throughput, there is **1 collision per 3500 billion IDs**. (That is 3,500,000,000,000. As a frame of reference, it takes 2 billion IDs to exhaust an `int` primary key.)
- On average, with 10 application replicas at maximum throughput, there is **1 collision 350 billion IDs**.
- On average, with 100 application replicas at maximum throughput, there is **1 collision per 35 billion IDs**.

It is important to note that **the above is only in the degenerate scenario** where _all replicas_ are generating IDs _at the maximum rate per millisecond_, and always _on the exact same millisecond_. In practice, far fewer IDs tend to be generated per millisecond, thus spreading IDs out over more timestamps. This significantly reduces the realistic probability of a collision, to 1 per many trillions, a negligible number.

#### Absolute Certainty

Luckily, we can protect ourselves even against the extremely unlikely event of a collision.

For contexts where even a single collision could be catastrophic, such as in certain financial domains, it is advisable to avoid "upserts", and always explicitly separate inserts from updates. This way, even if a collision did occur, it would merely cause one single transaction to fail (out of billions or trillions), rather than overwriting an existing record. This is good practice in general.

### Guessability

Presupposing knowledge of the millisecond timestamp on which an ID was generated, the probability of guessing that ID is between 1/2^41 and 1/2^48, thanks to the 48-bit cryptographically-secure pseudorandom sequence. In practice, the timestamp component tends to reduce the guessability, since for most milliseconds no IDs at will will have been generated.

The difference between the two probabilities (given knowledge of the timestamp) stems from the way the incremental property is achieved. If only one ID was generated on a timestamp, as tends to be common, the probability is 1/2^48. If the maximum number of IDs were generated on that timestamp, or if another ID from the same timestamp is known, an educated guess has a 1/2^41 probability of being correct.

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
