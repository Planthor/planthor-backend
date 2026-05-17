abstract final class AppConfig {
  static const keycloakBase = 'http://localhost:8180/realms/planthor';
  static const authEndpoint = '$keycloakBase/protocol/openid-connect/auth';
  static const tokenEndpoint = '$keycloakBase/protocol/openid-connect/token';
  static const endSessionUrl = '$keycloakBase/protocol/openid-connect/logout';

  // Alternative: use OIDC discovery instead of explicit endpoints
  // static const discoveryUrl = '$keycloakBase/.well-known/openid-configuration';

  static const clientId = 'planthor-ios';
  static const redirectUri = 'planthor://callback';
  static const postLogoutUri = 'planthor://callback';
  static const scopes = ['openid', 'profile', 'email', 'offline_access'];

  // Resource API — matches launchSettings.json "http://localhost:5008"
  // 10.0.2.2 is the Android emulator alias for host machine's localhost
  static const apiBase = 'http://localhost:5008';
}
