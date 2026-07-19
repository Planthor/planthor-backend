# API Collections

> **Bruno-compatible** API collections used to investigate, document, and prototype third-party API integrations for the **Planthor Backend** service.

---

## Purpose

Before wiring an external API into the Planthor backend codebase, we use this directory to:

- **Explore** the external API's capabilities, authentication flows, and data shapes.
- **Document** real-world request/response examples in a living, version-controlled format.
- **Prototype** integration logic (scripts, environment variables, assertions) that maps directly to what the backend will consume.
- **Onboard** future contributors quickly — clone the repo, open a collection in Bruno, and start making real calls within minutes.

Every collection here is written in the [Bruno](https://www.usebruno.com/) open collection format, making it **Git-friendly**, **local-first**, and free from vendor lock-in.

---

## Collections

| Folder | Status | Description |
| --- | --- | --- |
| [`strava/`](./strava/) | 🚧 In Progress | Strava API — activity feeds, athlete data, OAuth 2.0 PKCE flow |
| [`github/`](./github/) | 🚧 In Progress | GitHub REST & GraphQL API — repos, issues, webhooks |

> Collections are added as new integration needs arise. Each folder is self-contained and can be opened as an independent Bruno collection.

---

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| [Bruno](https://www.usebruno.com/downloads) | ≥ 1.x | Desktop app to run and manage collections |
| Git | any | Collections are plain files — just clone and open |

No account, no cloud sync, no API key for Bruno itself. Everything lives in this repository.

---

## Getting Started

### 1. Install Bruno

Download and install Bruno from the [official downloads page](https://www.usebruno.com/downloads). Bruno is available for macOS, Windows, and Linux.

### 2. Open a Collection

1. Launch Bruno.
2. Click **Open Collection**.
3. Navigate to any sub-folder inside `tools/api-collections/` (e.g. `tools/api-collections/strava`).
4. Bruno will load all requests and environments automatically.

### 3. Configure an Environment

Each collection contains an `environments/` folder with pre-defined variable sets (e.g. `local.yml`, `sandbox.yml`). Select the appropriate environment in the top-right of Bruno before running requests.

Secrets (tokens, client IDs, client secrets) are **never committed**. Use Bruno's **Secret Variables** feature or a local `.env`-style override to supply them.

### 4. Run Requests

Select any request from the sidebar and click **Send**. Scripts in the `Pre Request` and `Post Response` tabs automate token exchange, variable chaining, and assertions where applicable.

---

## Collection Structure Convention

Every collection folder follows this layout:

```text
<provider>/
├── bruno.json              # Bruno collection metadata
├── environments/
│   ├── local.yml           # Local / development environment variables
│   └── sandbox.yml         # Sandbox / staging environment (if applicable)
├── auth/
│   └── *.bru               # Authentication flows (OAuth, token exchange, etc.)
├── <resource-group>/
│   └── *.bru               # Grouped API requests by resource or domain
└── README.md               # Provider-specific notes, auth setup, gotchas
```

> New collections should follow this convention for consistency.

---

## Bruno File Format Basics

Bruno uses a plain-text `.bru` file format. A minimal request looks like:

```bru
meta {
  name: Get Athlete
  type: http
  seq: 1
}

get {
  url: {{baseUrl}}/athlete
  body: none
  auth: bearer
}

auth:bearer {
  token: {{accessToken}}
}
```

Variables wrapped in `{{double braces}}` are resolved from the active environment at runtime.

Refer to the [`bruno-starter-guide/`](./bruno-starter-guide/) collection and the [Bruno documentation](https://docs.usebruno.com) for a full format reference.

---

## Adding a New Collection

1. Create a new sub-folder: `tools/api-collections/<provider>/`.
2. Follow the [Collection Structure Convention](#collection-structure-convention) above.
3. Add a `README.md` inside the folder describing:
   - What the API does and why Planthor uses it.
   - How to obtain credentials / set up an app on the provider's developer portal.
   - Any non-obvious quirks (rate limits, token TTLs, scopes needed).
4. Update the [Collections](#collections) table in this file.
5. Open a PR — reviewers can pull the branch and open the collection locally to verify.

---

## Relationship to the Backend

These collections are **investigative tooling**, not production code. However, they directly inform the backend in the following ways:

- **Data contracts** discovered here (request/response shapes) are translated into C# DTOs and domain models.
- **Auth flows** prototyped here are implemented in the corresponding integration services (e.g. `StravaService`, `GitHubService`).
- **Edge cases and error responses** captured as assertions become the basis for unit and integration test scenarios.

---

## References

- [Bruno — Official Website](https://www.usebruno.com/)
- [Bruno Documentation](https://docs.usebruno.com)
- [Bruno GitHub Repository](https://github.com/usebruno/bruno)
- [Bruno OpenCollection Format Spec](https://docs.usebruno.com/introduction/open-collection)
- [Strava API Documentation](https://developers.strava.com/docs/reference/)
- [GitHub REST API Documentation](https://docs.github.com/en/rest)
- [GitHub GraphQL API Documentation](https://docs.github.com/en/graphql)
