# Architect.Identities

Provides various tools related to IDs.

This package features the **Fluid** ID generator, which generates **F**lexible, **L**ocally-**U**nique **ID**s. A Fluid is intended to replace a pair of auto-increment ID + UUID. It is 64-bit and incremental (i.e. suitable as a primary key) and does not leak the sensitive information that an auto-increment ID does.

The package also features the **PublicIdentities** system, a set of tools for converting local IDs to public IDs. If the local IDs still leak too much information to be shared publically, PublicIdentities can be used. It converts 64-bit (or smaller) IDs into deterministic, reversible public IDs that are indistinguishable from random noise without possession of the configured key. These can replace UUIDs, without the additional bookkeeping.

Furthermore, this package features various **ApplicationInstanceIdSource** implementations. These implementations provide a unique ID to each distinct application (or instance thereof) in a bounded context, by using a centralized storage component, such as a SQL database or an Azure Blob Storage Container. The Fluid system relies on this.
