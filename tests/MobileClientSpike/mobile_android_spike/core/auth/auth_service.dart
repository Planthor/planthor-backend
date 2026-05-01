import 'package:flutter_appauth/flutter_appauth.dart';

import '../config/app_config.dart';
import 'token_storage.dart';

class AuthService {
  AuthService(this._storage) : _appAuth = const FlutterAppAuth();

  final FlutterAppAuth _appAuth;
  final TokenStorage _storage;

  static final _serviceConfig = AuthorizationServiceConfiguration(
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
        // AppAuth generates the PKCE verifier + S256 challenge automatically
      ),
    );

    await _storage.saveTokens(
      accessToken: result.accessToken!,
      refreshToken: result.refreshToken!,
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
      ),
    );

    await _storage.saveTokens(
      accessToken: result.accessToken!,
      refreshToken:
          result.refreshToken ?? refreshToken, // Keycloak may rotate it
      expiry: result.accessTokenExpirationDateTime!,
    );
  }

  /// Clears local tokens AND ends the Keycloak SSO session.
  Future<void> logout() async {
    final token = await _storage.accessToken;
    await _storage.clear();

    await _appAuth.endSession(
      EndSessionRequest(
        idTokenHint: token, // signals Keycloak which session
        postLogoutRedirectUrl: AppConfig.postLogoutUri,
        serviceConfiguration: _serviceConfig,
      ),
    );
  }
}
