# ADR-0005: Split `ISender` and `IPublisher` Interfaces

## Status

Accepted

## Context

Request/response dispatch (exactly one handler, one result, exceptions propagate directly) and notification fan-out (zero-to-many handlers, independent success/failure, aggregated exceptions) have fundamentally different contracts and failure semantics. A single combined interface (a "mediator" doing both) forces every consumer to depend on both capabilities even when only one is used, and blurs two distinct behavioral contracts into one API surface.

## Decision

Expose two separate interfaces: `ISender` (request/response dispatch via `Send`) and `IPublisher` (notification fan-out via `Publish`). Application code injects only the one(s) it needs.

## Alternatives Considered

1. **Single `IMediator` interface with both `Send` and `Publish`** — rejected: violates Interface Segregation Principle; a class that only sends commands still couples to notification-publishing capability it never uses, complicating mocking/testing and misrepresenting the class's actual dependencies.
2. **Two separate interfaces (chosen)** — each is independently mockable/testable, each is explainable in one sentence per the API design non-negotiable, and each can evolve independently (e.g., adding `CreateStream` to `ISender` doesn't touch `IPublisher`'s contract).

## Tradeoffs

- Consumers needing both capabilities inject two constructor parameters instead of one — a minor ergonomic cost accepted in exchange for interface clarity.

## Consequences

- `Conduit.Testing`'s `FakeSender` and a corresponding future `FakePublisher` can be tested and evolved independently.
- Static analysis of "which classes send requests vs. publish notifications" becomes trivial by inspecting constructor dependencies, aiding the request-graph tooling in [Diagnostics](../13-diagnostics.md).
