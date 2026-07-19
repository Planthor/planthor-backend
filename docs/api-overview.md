# API overview

All v1 controller routes require a JWT bearer token, except the anonymous health
endpoint:

```text
GET /v1/healthz
```

In Development, inspect the complete contract and try authenticated requests through
Scalar at `/scalar/v1`. Its OpenAPI document is served from `/openapi/v1.json`.

## Authentication

The API validates bearer tokens from the configured Keycloak authority. Send the
token on each protected request:

```http
Authorization: Bearer <token>
```

## v1 routes

| Resource | Routes |
| --- | --- |
| Members | `GET /v1/members`, `GET /v1/members/{id}`, `PUT /v1/members/{id}` |
| Member creation | `POST /v1/members` |
| Personal plans | `GET`, `POST`, and `PATCH /v1/members/{identifier}/personalplans`; `GET` and `PUT /v1/members/{identifier}/personalplans/{planId}`; `POST /v1/members/{identifier}/personalplans/{planId}:cancel`; `POST /v1/members/{identifier}/personalplans/{planId}:activate` |
| External connections | `GET /v1/members/{identifier}/externalconnections`, `GET /v1/members/{identifier}/externalconnections/{id}` |
| Activity logs | `GET`, `POST`, and `PATCH /v1/plans/{planId}/activitylogs`; `GET`, `PUT`, and `DELETE /v1/plans/{planId}/activitylogs/{logId}` |

`identifier` supports `@me` where the corresponding controller allows resolving the
currently authenticated member.

## Important behavior

`POST /v1/members` is marked `DevelopmentOnly`; it returns 404 outside the
Development environment. Member creation derives the identity name from the JWT,
not from the request body.

Use the OpenAPI schema rather than this overview for required fields, response
models, and status-code details.
