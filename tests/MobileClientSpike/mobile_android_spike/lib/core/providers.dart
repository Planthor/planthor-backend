import 'package:dio/dio.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

import 'auth/token_storage.dart';
import 'auth/auth_service.dart';
import 'api/api_client.dart';

part 'providers.g.dart';

@Riverpod(keepAlive: true)
TokenStorage tokenStorage(Ref ref) => TokenStorage();

@Riverpod(keepAlive: true)
AuthService authService(Ref ref) =>
    AuthService(ref.watch(tokenStorageProvider));

@Riverpod(keepAlive: true)
Dio apiClient(Ref ref) => buildApiClient(
    ref.watch(tokenStorageProvider),
    ref.watch(authServiceProvider));
