# AWS FileService (.NET 8 + S3 Pre-signed URLs)

A production-ready file service built with ASP.NET Core, EF Core, Repository + Unit of Work, and Amazon S3.
The service stores **metadata in PostgreSQL** and file **blobs in S3**, using **direct browser uploads** via pre-signed URLs.

---

## Features

* Direct upload/download via **S3 pre-signed URLs** (API never streams the bytes)
* Private S3 bucket, server-side encryption, short URL TTLs
* CRUD for file metadata (rename, describe, list, delete)
* Optional **finalize** step (server verifies uploaded object and records size)
* Repository Pattern + **Unit of Work**
* Pagination + search (PostgreSQL `ILIKE`) with optional `pg_trgm` acceleration
* Swagger/OpenAPI enabled

---

## Architecture

* **API:** ASP.NET Core Web API
* **Storage:** Amazon S3 (private bucket)
* **DB:** PostgreSQL via EF Core
* **Security:** AuthZ check per owner; pre-signed URLs with short TTL
* **Upload flow:** Client → API (`init-upload`) → Client PUTs to S3 → Client → API (`finalize`)

---

## Requirements

* .NET 8 SDK
* PostgreSQL 13+ (local or cloud)
* AWS account & S3 bucket
* AWS credentials available to the app (environment variables or IAM role)

---

## Quick Start

### 1) Clone & restore

```bash
git clone <your-repo-url>
cd <repo-root>
dotnet restore
```

### 2) Configure `appsettings.json`

```json
{
  "Aws": {
    "Region": "eu-central-1",
    "S3": { "BucketName": "your-private-bucket" },
    "Presign": { "UploadExpirySeconds": 600, "DownloadExpirySeconds": 300 }
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=filesvc;Username=postgres;Password=postgres"
  }
}
```

> Credentials are **not** stored here. Provide them via env vars or IAM:
>
> * `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` (and optionally `AWS_SESSION_TOKEN`)
> * or run inside AWS with an instance/task role.

### 3) Database setup

Install EF packages (already included in the project; if not):

```bash
dotnet add <PersistenceProject> package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.*
dotnet add <ApiProject> package Microsoft.EntityFrameworkCore.Design --version 8.*
```

Create & apply initial migration:

```bash
dotnet ef migrations add InitialCreate -p FileService.Persistance -s FileService.Api
dotnet ef database update -p FileService.Persistance -s FileService.Api
```

### 4) AWS S3 bucket CORS

Set CORS on your bucket (adapt origin):

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

### 5) IAM permissions (execution role/user)

Least-privilege example (adjust bucket name):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow",
      "Action": ["s3:PutObject","s3:GetObject","s3:DeleteObject","s3:AbortMultipartUpload","s3:ListBucket","s3:GetBucketLocation"],
      "Resource": [
        "arn:aws:s3:::your-private-bucket",
        "arn:aws:s3:::your-private-bucket/*"
      ]
    }
  ]
}
```

### 6) Run

```bash
dotnet run --project FileService.Api
```

Open Swagger at `https://localhost:5001/swagger` (or the shown port).

---

## Project Structure

```
/FileService.Api
  Program.cs
  Controllers/FilesController.cs

/FileService.Core
  DTO/ (InitUploadRequest, InitUploadResponse, PagedResult, etc.)
  Contracts/IFileStorageService.cs
  Options/AwsOptions.cs
  Entities/StoredFile.cs
  Enums/FileStatus.cs

/FileService.Persistance
  AppDbContext.cs
  Abstractions/ (IRepository, IStoredFileRepository, IUnitOfWork)
  Repository/ (EfRepository, StoredFileRepository, UnitOfWork)
  Migrations/ (EF Core)
  
/FileService.Core.Services
  S3FileStorageService.cs
```

---

## Dependency Injection (excerpt)

```csharp
// Program.cs (API)
builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection("Aws"));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
    return new AmazonS3Client(RegionEndpoint.GetBySystemName(opts.Region));
});

builder.Services.AddScoped<IStoredFileRepository, StoredFileRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
```

---

## Endpoints

### Initialize upload (Create)

`POST /api/files/init-upload`

Request:

```json
{ "fileName": "report.pdf", "contentType": "application/pdf", "expectedSizeBytes": 5242880 }
```

Response:

```json
{
  "id": "GUID",
  "key": "users/<userId>/<GUID>/report.pdf",
  "uploadUrl": "https://s3...X-Amz-Signature=...",
  "expiresAtUtc": "2025-01-01T12:00:00Z"
}
```

### Finalize

`PATCH /api/files/{id}/finalize` → `204 No Content`

### Get metadata (Read one)

`GET /api/files/{id}` → `StoredFile`

### List (Read many)

`GET /api/files?page=1&pageSize=50&search=&contentType=`

Returns `PagedResult<StoredFile>`:

```json
{ "items": [ ... ], "page": 1, "pageSize": 50, "total": 123 }
```

### Update metadata

`PATCH /api/files/{id}`
Request:

```json
{ "newFileName": "Q4_report.pdf", "description": "Final" }
```

### Delete

`DELETE /api/files/{id}` → soft-delete + S3 delete

### Download URL

`GET /api/files/{id}/download-url` → `{ "url": "https://s3...GET..." }`

---

## Frontend Upload Flow (example)

```ts
// 1) Ask API for a pre-signed PUT
const r = await fetch("/api/files/init-upload", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ fileName: file.name, contentType: file.type, expectedSizeBytes: file.size })
});
const { id, uploadUrl } = await r.json();

// 2) Upload directly to S3
await fetch(uploadUrl, { method: "PUT", headers: { "Content-Type": file.type }, body: file });

// 3) Finalize
await fetch(`/api/files/${id}/finalize`, { method: "PATCH" });
```

---

## Security Checklist

* S3 **bucket is private** (no public ACLs)
* Pre-signed URL TTLs: **5–10 minutes**
* **Validate** filename, MIME type, and (optionally) expected size before issuing URLs
* Server-side encryption: `AES256` or KMS (enforce via headers or bucket policy)
* Rate-limit `init-upload` to prevent abuse
* Log/audit: user, object key, and issued URL timestamps
* AuthZ: only owner (or admin) may read/download/delete

---

## PostgreSQL Search Performance

For case-insensitive contains on large tables:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS ix_storedfiles_filename_trgm
ON "StoredFiles" USING gin ("FileName" gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_storedfiles_description_trgm
ON "StoredFiles" USING gin ("Description" gin_trgm_ops);
```

In queries use:

```csharp
var pattern = $"%{search}%";
q = q.Where(f => EF.Functions.ILike(f.FileName, pattern) ||
                 EF.Functions.ILike(f.Description ?? string.Empty, pattern));
```

---

## Troubleshooting

* **`UseNpgsql` not found**
  Install provider where `AppDbContext` compiles and add:

  ```csharp
  using Microsoft.EntityFrameworkCore;
  ```

  ```
  dotnet add FileService.Persistance package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.*
  ```

* **`EF.Functions.ILike` not found**
  Add:

  ```csharp
  using Npgsql.EntityFrameworkCore.PostgreSQL;
  ```

  Ensure queries run against a PostgreSQL provider (not in-memory).

* **S3 CORS errors**:
  Verify bucket CORS allows your origin and `PUT/GET`.

* **403 on upload**:
  Check that the `Content-Type` header on the PUT **matches** what you used when signing the URL.
  Verify IAM policy and bucket region.

---

## Roadmap / TODO

* Multipart uploads (pre-sign each part) for very large files
* Virus scanning / quarantine (Lambda + SQS) after upload
* Admin access policies & cross-tenant support
* Object versioning + retention policies
* Optional CDN (CloudFront) for downloads

---

## License

MIT (or your preferred license). Add a `LICENSE` file if publishing publicly.
