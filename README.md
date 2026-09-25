# TestJob API

Minimal .NET 10 REST API for the supplied test assignment. The endpoint decodes
the URL and HTML page, parses the page with AngleSharp, extracts element
attributes and email addresses, decrypts AES-256-ECB data, and stores discovered
elements in PostgreSQL through Dapper.

## Current implementation

- `POST /api/process-page`
- FluentValidation request validation
- AngleSharp HTML parsing and CSS selectors
- compiled email regular expression with a timeout
- AES-256, ECB, `PaddingMode.None` decryption
- asynchronous PostgreSQL writes through Dapper/Npgsql
- indented JSON responses with the required snake_case field names
- Swagger UI at `/api/swagger`
- unit tests against both supplied payloads

Docker Compose is intentionally deferred to a later step.

## Run locally

Prerequisites: .NET 10 SDK and a running PostgreSQL instance.

The default connection string is in
`src/TestJob.Api/appsettings.json`. It can be overridden with the environment
variable `ConnectionStrings__Postgres`.

```powershell
dotnet restore TestJob.slnx
dotnet test TestJob.slnx
dotnet run --project src/TestJob.Api --urls http://localhost:8090
```

Open `http://localhost:8090/api/swagger` and submit either
`json_payload_1.txt` or `json_payload_2.txt` to `POST /api/process-page`.

The `elements` table is created automatically on the first successful request.

## Why the endpoint is asynchronous

The controller, validator, HTML parser, and repository expose asynchronous APIs.
Most importantly, opening the PostgreSQL connection and executing commands are
I/O operations; awaiting them releases the request thread while the database is
working, allowing the server to handle other requests efficiently.

Base64 decoding, regular-expression matching, CSS selection, and AES decryption
are in-memory CPU work. .NET does not provide genuinely asynchronous variants
for these operations, and wrapping them in `Task.Run` would only move the work to
another thread while adding scheduling overhead. They therefore remain
synchronous inside the otherwise asynchronous request flow.
