#!/bin/bash
set -e

echo "🔑 Fetching secrets from Azure Key Vault..."
KEYVAULT_NAME="planthor-keyvault" # Update this to your actual Key Vault name

# 1. Fetch the secrets dynamically and strip hidden newlines/carriage returns
FACEBOOK_SECRET=$(az keyvault secret show --name "fb-app-secret" --vault-name "$KEYVAULT_NAME" --query "value" -o tsv | tr -d '\r\n')
BACKEND_SECRET="Planthor_123"

# 2. Create the output directory
mkdir -p infrastructure/keycloak/processed_realms

echo "⚙️  Templating Keycloak Realm JSON with sed..."
# 3. Use sed to replace the variables
# We use | as the delimiter in sed to avoid issues if secrets contain forward slashes
sed -e "s|\${FACEBOOK_CLIENT_SECRET}|$FACEBOOK_SECRET|g" \
    -e "s|\${PLANTHOR_BACKEND_SECRET}|$BACKEND_SECRET|g" \
    infrastructure/keycloak/realms/planthor-realm.json > infrastructure/keycloak/processed_realms/planthor-realm.json

# Copy user seed files to the processed_realms directory so Keycloak can import them
cp infrastructure/keycloak/realms/planthor-users-*.json infrastructure/keycloak/processed_realms/ 2>/dev/null || true

echo "🚀 Starting Infrastructure..."
# 4. Start Docker Compose
docker compose -f infrastructure/compose.yaml up --build -d
