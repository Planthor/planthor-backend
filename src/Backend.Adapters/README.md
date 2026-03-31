# Setup for adapter

## Cloudflare tunnel for developer

Why is this needed?
Strava's webhook requires a URL it can call from the internet (e.g. https://yourdomain.com/v1/strava/webhook). Your localhost:5000 is not reachable from outside.

Cloudflare Tunnel solves this by creating a secure, named tunnel from Cloudflare's edge to your local machine — no port forwarding, no firewall changes.

This gives you a stable URL like https://planthor-dev.your-account.cfargotunnel.com that you register with Strava as your webhook URL — and it stays valid across restarts.
