# Frontend HTTP Error Pipeline Specification

## Purpose

`axiosUtils.ts`'s `sendGet()` currently has its own bespoke catch block and never calls `throwError`/`triggerStatusCodeHandlers` like `sendPost`, `sendPut`, and `sendDelete` do. As a result, the 401 → redirect-to-`/token-invalido` handler never fires on GET requests, which is the majority of read traffic. This spec defines the corrected behavior: `sendGet` routes through the same shared pipeline as the other HTTP verbs.

## Requirements

### Requirement: sendGet Routes Through Shared Error Pipeline

`sendGet` MUST delegate to the same shared request pipeline (`sendRequest` / `throwError`) used by `sendPost`, `sendPut`, and `sendDelete`, so that `triggerStatusCodeHandlers` fires uniformly for GET requests, including the 401 → `/token-invalido` redirect handler.

#### Scenario: GET request receiving 401 triggers redirect

- GIVEN an authenticated GET request is issued via `sendGet`
- WHEN the API responds with HTTP 401
- THEN the same redirect-to-`/token-invalido` handler that fires for `sendPost`/`sendDelete` on 401 is also triggered for the GET request
- AND this is verified with a Vitest test using a mocked axios interceptor/response (not an isolated unit test on an extracted helper)

#### Scenario: GET request receiving a fixed 404 is surfaced consistently

- GIVEN a GET request targets an endpoint covered by `api-not-found-semantics`
- WHEN the API responds with HTTP 404
- THEN `sendGet` propagates/throws an error object of the same shape as the other verbs produce for 404 (no bespoke catch swallowing or reshaping the status)

### Requirement: Error Handling Behavior Parity Across HTTP Verbs

The system MUST treat `sendGet` identically to `sendPost`/`sendPut`/`sendDelete` with respect to error-status handling — no verb-specific catch block bypasses the shared `throwError`/`triggerStatusCodeHandlers` logic.

#### Scenario: sendGet has no bespoke catch bypassing the shared pipeline

- GIVEN `axiosUtils.ts` after the fix
- WHEN `sendGet` is invoked and the underlying request fails with any error status
- THEN control reaches `throwError` the same way it does for `sendPost`
- AND this is verified by a test asserting the shared handler is invoked, not by inspecting source text alone

### Requirement: No New Frontend Business Logic or UI (non-goal boundary)

This change MUST NOT introduce new UI components, new routes, or new business logic beyond routing `sendGet` through the existing shared pipeline.

#### Scenario: No behavior change for existing consumers

- GIVEN no existing frontend code branches on HTTP 400 for not-found handling (confirmed via repository-wide grep)
- WHEN `sendGet` is updated to use the shared pipeline
- THEN no existing `sendGet` caller's success-path behavior changes
- AND the only observable behavior change is that GET requests now also trigger the shared 401/error handling that other verbs already had

## Acceptance Evidence

A Vitest test using a mocked axios interceptor (or equivalent HTTP-mock harness) MUST prove that a GET call receiving a 401 response triggers the same redirect handler already covered by existing `sendDelete`/`sendPost` tests. A unit test that only calls an isolated function in isolation, without exercising the interceptor/pipeline wiring, is insufficient evidence.
