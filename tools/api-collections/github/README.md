# GitHub API Collection

Bruno collection for investigating the [GitHub REST API v3](https://docs.github.com/en/rest) and prototyping the integration into Planthor Backend.

---

## Structure

```text
github/
├── opencollection.yml   # Bruno collection metadata
├── environments/
│   └── local.yml        # Base URL + secret access token
└── README.md            # This file
```

---

## Prerequisites

- A GitHub account
- A **Personal Access Token (PAT)** — classic or fine-grained → [Generate one here](https://github.com/settings/tokens)

> For OAuth App / GitHub App flows, refer to the [GitHub OAuth documentation](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps).

---

## Getting an Access Token

### Option A — Personal Access Token (quickest for local testing)

1. Go to [https://github.com/settings/tokens](https://github.com/settings/tokens).
2. Click **Generate new token (classic)** (or fine-grained for stricter scope control).
3. Select the scopes you need (see table below) and click **Generate token**.
4. Copy the token — GitHub shows it only once.

### Option B — OAuth 2.0 (for app-level integration testing)

1. Register an OAuth App at [https://github.com/settings/developers](https://github.com/settings/developers) and note the **Client ID** and **Client Secret**.
2. Direct users to:

   ```text
   https://github.com/login/oauth/authorize
     ?client_id=YOUR_CLIENT_ID
     &scope=read:user,repo
   ```

3. After authorisation, GitHub redirects to your callback URL with a `code` param. Exchange it:

   ```bash
   curl -X POST https://github.com/login/oauth/access_token \
     -H "Accept: application/json" \
     -d client_id=YOUR_CLIENT_ID \
     -d client_secret=YOUR_CLIENT_SECRET \
     -d code=XXXX
   ```

4. Copy the `access_token` from the JSON response.

---

## Configure the Environment

1. In Bruno, open this collection folder (`tools/api-collections/github`).
2. Select the **local** environment (top-right dropdown).
3. Click the **lock icon** next to `accessToken` and paste your token — it is stored as a **secret** and never committed to Git.

All requests use `{{baseUrl}}` and `{{accessToken}}` from this environment.

---

## Useful Scopes

| Scope | What it unlocks |
| --- | --- |
| `read:user` | Read public and private user profile data |
| `user:email` | Access user email addresses |
| `repo` | Full access to public and private repositories |
| `public_repo` | Read/write access to public repositories only |
| `read:org` | Read organisation membership and team data |
| `gist` | Create and read gists |

> For Planthor's use cases, `read:user` and `user:email` are the minimum required for social login / identity federation.

---

## Required Headers

All authenticated GitHub API requests must include:

```text
Authorization: Bearer {{accessToken}}
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28
```

The `Accept` and `X-GitHub-Api-Version` headers are set at the collection level so individual requests inherit them automatically.

---

## References

- [GitHub REST API Reference](https://docs.github.com/en/rest)
- [GitHub GraphQL API Reference](https://docs.github.com/en/graphql)
- [GitHub OAuth Apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [GitHub Apps](https://docs.github.com/en/apps/creating-github-apps/about-creating-github-apps/about-creating-github-apps)
- [Bruno Documentation](https://docs.usebruno.com)
