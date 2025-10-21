# AWS FileService (.NET 8 + S3 Pre-Signed URLs)

A production-ready, **multi-tenant** file service built with ASP.NET Core, EF Core (Repository + UoW), and Amazon S3.
Files are uploaded/downloaded via **pre-signed URLs** (the API never streams file bytes). Metadata lives in **PostgreSQL**.

---

## Features

* **Direct browser uploads/downloads** via S3 **pre-signed** PUT/GET (short TTLs)
* **Private** S3 bucket with server-side encryption (SSE)
* **Multi-tenant** model (`tenantId`) with tenant-first S3 key layout
* **Owner context**: `ownerType` (`"user"` | `"tenant"`) + `ownerId` + optional `category`
* **Metadata (jsonb)** for arbitrary key/values
* **Auditing**: `createdByUserId` (nullable), timestamps, soft delete
* **Search & paging** with Postgres `ILIKE` and optional trigram indexes
* **Repository + Unit of Work** pattern
* **Keycloak** JWT auth (SameTenant policy + simple owner guard)
* Swagger/OpenAPI

---

## Architecture

**API layer**

* ASP.NET Core Web API
* Keycloak JWT Bearer auth
* Controllers:

  * `TenantFilesController` (primary) under `/api/tenants/{tenantId}/files/*`
  * (Optional/legacy) `FilesController` under `/api/files/*` (deprecated)

**Domain / Core**

* `StoredFile` entity holds metadata only (S3 key, names, content type, size, ownership, auditing)
* DTOs (`InitUploadRequest`, `InitUploadResponse`, `PagedResult<T>`, etc.)
* `IFileStorageService` orchestrates S3 pre-signing & metadata persistence

**Persistence**

* PostgreSQL via EF Core
* `AppDbContext`, repositories (`StoredFileRepository`), `UnitOfWork`
* JSON metadata stored as **jsonb**
* Useful indexes for listing & singleton constraints

**Storage**

* Amazon S3 private bucket
* Tenant-first key convention:

  ```
  tenants/{tenantId}/users/{ownerId}/{fileId}/{fileName}     // ownerType=user
  tenants/{tenantId}/tenant/{tenantId}/{fileId}/{fileName}   // ownerType=tenant
  ```

**AuthN/Z**

* Keycloak (OIDC) for tokens
* Policy `SameTenant`: route `{tenantId}` must match token claim
* Owner guard: if `ownerType == "user"`, only that `ownerId == sub` may access (TenantAdmin bypass)

---

## Data Model

```csharp
StoredFile {
  Guid Id;
  string TenantId;                // partition (company/realm)
  string OwnerType;               // "user" | "tenant"
  string OwnerId;                 // userId or tenantId (for company assets)
  string? Category;               // e.g., "profile","branding","contracts"

  string Key;                     // S3 object key
  string FileName;
  string ContentType;
  long   SizeBytes;

  string? CreatedByUserId;        // uploader (nullable for system/integration jobs)
  Dictionary<string, object>? Metadata; // jsonb

  FileStatus Status;              // Pending / Uploaded / Deleted
  DateTime CreatedAtUtc;
  DateTime? UploadedAtUtc;
  DateTime? UpdatedAtUtc;
  DateTime? DeletedAtUtc;
}
```

**Indexes**

* Listing hot path: `(TenantId, OwnerType, OwnerId, Status, CreatedAtUtc)`
* Optional singleton (e.g., one logo/profile per owner), excluding deleted:

  * Unique on `(TenantId, OwnerType, OwnerId, Category)` with filter `Status <> Deleted`

---

## API Endpoints (tenant-explicit)

**Base:** `/api/tenants/{tenantId}/files`

* `POST  PresignUpload`
  Request:

  ```json
  {
    "fileName": "avatar.jpg",
    "contentType": "image/jpeg",
    "expectedSizeBytes": 524288,
    "ownerType": "user",
    "ownerId": "USER123",
    "category": "profile",
    "metadata": { "source": "web" }
  }
  ```

  Response:

  ```json
  {
    "id": "GUID",
    "key": "tenants/.../GUID/avatar.jpg",
    "uploadUrl": "https://s3...PUT...X-Amz-Signature=...",
    "expiresAtUtc": "2025-01-01T12:00:00Z"
  }
  ```

* `PATCH {id}/FinalizeUpload` → `204 No Content`
  (Server verifies object exists via `GetObjectMetadata`, records size, sets `UploadedAtUtc`.)

* `GET   GetFiles?ownerType=&ownerId=&category=&page=&pageSize=&search=&contentType=`
  Returns `PagedResult<StoredFile>`.

* `GET   GetFileById/{id}` → `StoredFile`

* `PATCH UpdateFileMetadata/{id}`
  Body: `{ "newFileName": "NewName.pdf", "description": "optional" }`

* `DELETE DeleteFileById/{id}` → soft delete in DB + delete object in S3

* `GET   PresignDownloadUrl/{id}` → `{ "url": "https://s3...GET...X-Amz-Signature=..." }`

> **Deprecated** legacy routes under `/api/files/*` still exist optionally; see mapping below.

---

## Auth (Keycloak)

* Configure JWT Bearer with:

  * `Authority`: `https://keycloak.example.com/realms/<Realm>`
  * `Audience`: `file-service-api` (Keycloak client for this API)
* Map roles from Keycloak (`realm_access.roles` / `resource_access[client].roles`) so `User.IsInRole("TenantAdmin")` works.
* **Policies**

  * `SameTenant`: route `{tenantId}` must equal token claim (`tenant` or similar).
  * Owner guard inside controller: if `ownerType == "user"`, `ownerId` must equal `sub` unless role `TenantAdmin`.

---

## Configuration

`appsettings.json`

```json
{
  "Aws": {
    "Region": "eu-central-1",
    "S3": { "BucketName": "your-private-bucket" },
    "Presign": { "UploadExpirySeconds": 600, "DownloadExpirySeconds": 300 }
  },
  "Authentication": {
    "Authority": "https://keycloak.example.com/realms/YourRealm",
    "Audience": "file-service-api",
    "RequireHttps": true
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=filesvc;Username=postgres;Password=postgres"
  }
}
```

**Do not** commit AWS credentials. Provide via env vars (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, …) or run with an IAM role.

---

## S3 CORS (browser uploads)

Example (adapt origin):

```xml
<CORSConfiguration>
  <CORSRule>
    <AllowedOrigin>https://your-frontend.example</AllowedOrigin>
    <AllowedMethod>PUT</AllowedMethod>
    <AllowedMethod>GET</AllowedMethod>
    <AllowedHeader>*</AllowedHeader>
    <ExposeHeader>ETag</ExposeHeader>
    <MaxAgeSeconds>300</MaxAgeSeconds>
  </CORSRule>
</CORSConfiguration>
```

---

## IAM policy (least privilege)

Adjust bucket name:

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "s3:PutObject","s3:GetObject","s3:DeleteObject",
      "s3:AbortMultipartUpload","s3:ListBucket","s3:GetBucketLocation"
    ],
    "Resource": [
      "arn:aws:s3:::your-private-bucket",
      "arn:aws:s3:::your-private-bucket/*"
    ]
  }]
}
```

---

## Project Structure

```
/FileService.Api
  Program.cs
  Controllers/TenantFilesController.cs          # primary (tenant routes)
  Controllers/FilesController.cs (deprecated)   # optional, legacy

/FileService.Core
  Contracts/IFileStorageService.cs
  DTO/InitUploadRequest.cs, InitUploadResponse.cs, PagedResult.cs, UpdateFileRequest.cs
  Entities/StoredFile.cs
  Enums/FileStatus.cs
  Options/AwsOptions.cs

/FileService.Persistance
  AppDbContext.cs
  Abstractions/ (IRepository, IUnitOfWork)
  Repository/ (EfRepository, StoredFileRepository, UnitOfWork)
  Migrations/

/FileService.Core.Services
  S3FileStorageService.cs
```

---

## Setup & Migrations

Install tools (once):

```bash
dotnet tool update -g dotnet-ef
```

Create/apply migrations:

```bash
# initial
dotnet ef migrations add InitialCreate -p FileService.Persistance -s FileService.Api
dotnet ef database update -p FileService.Persistance -s FileService.Api

# after tenant/owner/metadata changes
dotnet ef migrations add AddTenantOwnerMetadata -p FileService.Persistance -s FileService.Api
dotnet ef database update -p FileService.Persistance -s FileService.Api
```

**Optional search acceleration (Postgres):**

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX IF NOT EXISTS ix_storedfiles_filename_trgm
  ON "StoredFiles" USING gin ("FileName" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_storedfiles_description_trgm
  ON "StoredFiles" USING gin ("Description" gin_trgm_ops);
```

---

## Frontend Upload Flow (example)

```ts
// 1) ask API for a pre-signed PUT
const r = await fetch(`/api/tenants/${tenantId}/files/PresignUpload`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  },
  body: JSON.stringify({
    fileName: file.name,
    contentType: file.type,
    expectedSizeBytes: file.size,
    ownerType: "user",
    ownerId: userId,
    category: "profile",
    metadata: { source: "web" }
  })
});
const { id, uploadUrl } = await r.json();

// 2) upload directly to S3 (Content-Type must match presign)
await fetch(uploadUrl, {
  method: "PUT",
  headers: { "Content-Type": file.type },
  body: file
});

// 3) finalize
await fetch(`/api/tenants/${tenantId}/files/${id}/FinalizeUpload`, {
  method: "PATCH",
  headers: { "Authorization": `Bearer ${token}` }
});
```

---

## Security Checklist

* S3 **Block Public Access** ON, ACLs disabled (Object Ownership: Bucket owner enforced)
* **SSE** enabled (AES256 or KMS)
* Short presigned TTLs (e.g., 5–10 min)
* Validate filename / MIME allowlist / optional max size before presigning
* Rate-limit `PresignUpload`
* Authorization: `SameTenant` + owner guard; `TenantAdmin` bypass
* Log audit events (upload/finalize/delete); consider a dedicated audit table

---

## Troubleshooting

* **403 SignatureDoesNotMatch** on PUT → The `Content-Type` header used for PUT **must** equal the one used when signing.
* **CORS error** in browser → Bucket CORS must allow your origin + methods `PUT, GET`; expose `ETag`.
* **`EF.Functions.ILike` not found** → Ensure Npgsql EF provider is installed & `using Npgsql.EntityFrameworkCore.PostgreSQL;`.
* **Design-time DbContext errors** → Add `IDesignTimeDbContextFactory<AppDbContext>` to Persistence and load `ConnectionStrings:Default` from appsettings/env.

---

## Deprecated (migration guide)

Legacy routes under `/api/files/*` are deprecated. Use tenant-scoped routes:

| Old                                | New                                                           |
| ---------------------------------- | ------------------------------------------------------------- |
| `POST /api/files/init-upload`      | `POST /api/tenants/{tenantId}/files/PresignUpload`            |

---

## Roadmap

* Multipart uploads (pre-sign each part) for very large files

---

## License

MIT (or your preferred license).
