import 'package:flutter_appauth/flutter_appauth.dart';

import '../config/app_config.dart';
import 'token_storage.dart';

class AuthService {
  AuthService(this._storage) : _appAuth = const FlutterAppAuth();

  final FlutterAppAuth _appAuth;
  final TokenStorage _storage;

  static AuthorizationServiceConfiguration get _serviceConfig =>
      AuthorizationServiceConfiguration(
        authorizationEndpoint: AppConfig.authEndpoint,
        tokenEndpoint: AppConfig.tokenEndpoint,
        endSessionEndpoint: AppConfig.endSessionUrl,
      );

  Future<void> login() async {
    final result = await _appAuth.authorizeAndExchangeCode(
      AuthorizationTokenRequest(
        AppConfig.clientId,
        AppConfig.redirectUri,
        serviceConfiguration: _serviceConfig,
        scopes: AppConfig.scopes,
        allowInsecureConnections: AppConfig.allowInsecureConnections,
        // AppAuth generates the PKCE verifier + S256 challenge automatically
      ),
    );

    await _storage.saveTokens(
      accessToken: result.accessToken!,
      refreshToken: result.refreshToken!,
      idToken: result.idToken!,
      expiry: result.accessTokenExpirationDateTime!,
    );
  }

  Future<void> refresh() async {
    final refreshToken = await _storage.refreshToken;
    if (refreshToken == null) throw Exception('No refresh token stored');

    final result = await _appAuth.token(
      TokenRequest(
        AppConfig.clientId,
        AppConfig.redirectUri,
        serviceConfiguration: _serviceConfig,
        refreshToken: refreshToken,
        scopes: AppConfig.scopes,
        allowInsecureConnections: AppConfig.allowInsecureConnections,
      ),
    );

    await _storage.saveTokens(
      accessToken: result.accessToken!,
      refreshToken:
          result.refreshToken ?? refreshToken, // Keycloak may rotate it
      idToken: result.idToken,
      expiry: result.accessTokenExpirationDateTime!,
    );
  }

  /// Clears local tokens AND ends the Keycloak SSO session.
  Future<void> logout() async {
    final idToken = await _storage.idToken;
    await _storage.clear();

    if (idToken != null) {
      await _appAuth.endSession(
        EndSessionRequest(
          idTokenHint: idToken, // signals Keycloak which session
          postLogoutRedirectUrl: AppConfig.postLogoutUri,
          serviceConfiguration: _serviceConfig,
          allowInsecureConnections: AppConfig.allowInsecureConnections,
        ),
      );
    }
  }
}
