# Strava API Collection

Bruno collection for investigating the [Strava API v3](https://developers.strava.com/docs/reference/) and prototyping the integration into Planthor Backend.

---

## Structure

```text
strava/
├── opencollection.yml              # Bruno collection metadata
├── environments/
│   └── local.yml                   # Base URL + secret access token
├── athlete/
│   └── get-authenticated-athlete.yml  # GET /athlete
└── README.md                       # This file
```

---

## Prerequisites

- A Strava account
- A registered Strava API application → [My API Application](https://www.strava.com/settings/api)

---

## Getting an Access Token

Strava uses **OAuth 2.0**. For quick local testing you can generate a short-lived token manually:

1. Go to [https://www.strava.com/settings/api](https://www.strava.com/settings/api) and note your **Client ID** and **Client Secret**.
2. Open this URL in a browser (replace `YOUR_CLIENT_ID`):

   ```text
   https://www.strava.com/oauth/authorize
     ?client_id=YOUR_CLIENT_ID
     &redirect_uri=http://localhost
     &response_type=code
     &scope=read,activity:read_all
   ```

3. Authorise the app. You will be redirected to `http://localhost?code=XXXX` — copy the `code` value.
4. Exchange the code for a token (run in a terminal):

   ```bash
   curl -X POST https://www.strava.com/oauth/token \
     -d client_id=YOUR_CLIENT_ID \
     -d client_secret=YOUR_CLIENT_SECRET \
     -d code=XXXX \
     -d grant_type=authorization_code
   ```

5. Copy the `access_token` from the JSON response.

---

## Configure the Environment

1. In Bruno, open this collection folder (`tools/api-collections/strava`).
2. Select the **local** environment (top-right dropdown).
3. Click the **lock icon** next to `accessToken` and paste your token — it is stored as a **secret** and never committed to Git.

---

## Useful Scopes

| Scope | What it unlocks |
| --- | --- |
| `read` | Public athlete profile |
| `read_all` | Private athlete profile fields |
| `activity:read` | Public activities |
| `activity:read_all` | All activities (including private) |

---

## References

- [Strava API v3 Reference](https://developers.strava.com/docs/reference/)
- [Strava OAuth 2.0 Guide](https://developers.strava.com/docs/authentication/)
- [Bruno Documentation](https://docs.usebruno.com)
