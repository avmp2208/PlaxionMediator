### Postman collections

This folder contains Postman collections and a shared environment for exercising the PlaxionMediator sample apps manually.

#### Why two collections, not one

There are two collections — one per sample app — instead of a single combined collection, because the two apps are genuinely different products with different concerns:

- `PlaxionMediator.Sample.MinimalApi.postman_collection.json` — the MVP smoke-test sample. Endpoints are hand-written Minimal API delegates calling `ISender`/`IPublisher` directly (`/ping`, `/echo`, `/notify`, `/flow`, `/nested`, `/fail`). There is **no** `PlaxionMediator.AspNetCore` exception-handling middleware here, so `/fail` returns a plain ASP.NET Core 500, not `problem+json`.
- `PlaxionMediator.Sample.WebApi.postman_collection.json` — the Phase B sample. Endpoints are registered via `MapPlaxionMediatorPost/Get/Put/Patch/Delete` (`PlaxionMediator.MinimalApis`) and wired with `UsePlaxionMediatorExceptionHandling()` (`PlaxionMediator.AspNetCore`), so thrown `PlaxionMediatorException` subtypes come back as RFC 7807 `problem+json`.

Merging them into one collection would blur two different route sets, two different error-handling behaviors, and two different `Program.cs` entry points that are never run at the same time — keeping them separate makes each collection a focused, accurate reference for its own sample, while a single shared environment (`PlaxionMediator.postman_environment.json`) is enough since only one sample runs on `http://localhost:5000` at a time.

#### Usage

1. In Postman, **Import** both collection files and the environment file from this folder.
2. Select the **PlaxionMediator - Local** environment (default `baseUrl` = `http://localhost:5000`).
3. Run the sample you want to exercise:
   - `dotnet run --project samples/PlaxionMediator.Sample.MinimalApi`
   - `dotnet run --project samples/PlaxionMediator.Sample.WebApi`
4. Run the matching collection's requests (top to bottom for the WebApi collection, since later requests reuse the `itemId` collection variable captured by the "Create Item" request's test script).

If a sample listens on a different port, update the `baseUrl` variable in the environment (or the collection's own `variable` array) accordingly.
