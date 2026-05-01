import 'package:dio/dio.dart';

import 'auth_service.dart';
import 'token_storage.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._storage, this._authService);

  final TokenStorage _storage;
  final AuthService _authService;
  bool _isRefreshing = false;

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Proactively refresh if token is about to expire
    if (!await _storage.hasValidToken) {
      try {
        await _authService.refresh();
      } catch (_) {
        /* handled on 401 */
      }
    }

    final token = await _storage.accessToken;
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode != 401 || _isRefreshing) {
      return handler.next(err);
    }

    _isRefreshing = true;

    try {
      await _authService.refresh();
      _isRefreshing = false;

      // Retry original request with new token
      final token = await _storage.accessToken;
      final opts = err.requestOptions
        ..headers['Authorization'] = 'Bearer $token';
      final clone = await err.requestOptions.extra['dio'].fetch(opts);
      handler.resolve(clone);
    } catch (_) {
      _isRefreshing = false;
      await _storage.clear(); // force re-login
      handler.next(err);
    }
  }
}
