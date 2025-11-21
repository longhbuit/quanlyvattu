# Database Schema

This document outlines the database schema and data storage for the QuanLyVatTu application.

## Authentication and Authorization

The application uses a dual-layer authentication system.

### SQL Server Authentication

Users are created as SQL Server Logins and mapped to database Users. This is used for the main database connection.

#### System Tables Used

*   `sys.server_principals`: Used to check for the existence of a login name before creating a new one.

### Application-Level Authentication

A local `users.json` file is used for application-level user management. This seems to be a separate user system, and it is unclear how it is used in conjunction with the SQL Server logins.

**File Location:** `bin/Debug/net10.0-windows/users.json` (relative to the executable)

**Structure:**
```json
[
  {
    "Username": "string",
    "PasswordHash": "string (SHA256)"
  }
]
```

## Business Tables

Currently the first business-oriented table being introduced is `Producers` (in each database `CTY`, `CN1`, `CN2`). Additional domain tables (materials, warehouses, orders, etc.) have not yet been defined.

#### Conceptual Stored Procedures
(All created in each database; scripts are idempotent.)
- `sp_AddProducer`: Insert a new producer; returns newly generated identity.
- `sp_GetProducerById`: Fetch a single producer by `@Id`.
- `sp_GetAllProducers(@OnlyActive BIT = 0)`: List producers; when `@OnlyActive = 1` filters `IsActive = 1`; ordered by `Name`.
- `sp_UpdateProducer`: Update mutable fields (`Name`, contact info, `IsActive`). Returns affected row count.
- `sp_DeleteProducer`: Physical delete by `@Id`. (Could be replaced later by soft delete toggling `IsActive`.)

#### Deployment Notes
- Same DDL/CRUD procedures deployed separately to `CTY`, `CN1`, `CN2`.
- Keep creation scripts idempotent: check existence before create/drop.
- Optional seed: Insert 1–2 sample rows only if table is empty to aid UI development.

#### Future Extensions
- Add unique index on `Name` (per database) if duplicates should be prevented.
- Add audit fields (`ModifiedAt`, `ModifiedBy`).
- Consider separating contact details into a `ProducerContacts` table if multiple contacts per producer become necessary.
- Add search indexes (e.g., nonclustered index on `Name`, `IsActive`).
