import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class TokenStorage {
  TokenStorage()
    : _storage = const FlutterSecureStorage(
        aOptions: AndroidOptions(),
      );

  final FlutterSecureStorage _storage;

  static const _accessKey = 'access_token';
  static const _refreshKey = 'refresh_token';
  static const _idTokenKey = 'id_token';
  static const _expiryKey = 'token_expiry'; // ISO8601

  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
    String? idToken,
    required DateTime expiry,
  }) => Future.wait([
    _storage.write(key: _accessKey, value: accessToken),
    _storage.write(key: _refreshKey, value: refreshToken),
    if (idToken != null) _storage.write(key: _idTokenKey, value: idToken),
    _storage.write(key: _expiryKey, value: expiry.toIso8601String()),
  ]);

  Future<String?> get accessToken => _storage.read(key: _accessKey);
  Future<String?> get refreshToken => _storage.read(key: _refreshKey);
  Future<String?> get idToken => _storage.read(key: _idTokenKey);
  Future<DateTime?> get expiry async {
    final raw = await _storage.read(key: _expiryKey);
    return raw == null ? null : DateTime.tryParse(raw);
  }

  Future<bool> get hasValidToken async {
    final exp = await expiry;
    return exp != null &&
        exp.isAfter(DateTime.now().add(const Duration(seconds: 30)));
  }

  Future<void> clear() => _storage.deleteAll();
}
