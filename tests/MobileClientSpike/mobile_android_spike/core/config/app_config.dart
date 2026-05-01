abstract final class AppConfig {
  static const keycloakBase = 'https://keycloak.example.com/realms/my-realm';
  static const authEndpoint = '$keycloakBase/protocol/openid-connect/auth';
  static const tokenEndpoint = '$keycloakBase/protocol/openid-connect/token';
  static const endSessionUrl = '$keycloakBase/protocol/openid-connect/logout';

  static const clientId = 'planthor-ios';
  static const redirectUri = 'com.yourcompany.yourapp:/oauth2redirect';
  static const postLogoutUri = 'com.yourcompany.yourapp:/oauth2redirect';
  static const scopes = ['openid', 'profile', 'email', 'offline_access'];

  // Resource API
  static const apiBase = 'https://api.example.com';
}
