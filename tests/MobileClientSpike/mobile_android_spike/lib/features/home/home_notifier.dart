import 'package:dio/dio.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

import '../../core/providers.dart';

part 'home_notifier.g.dart';

@riverpod
class HomeNotifier extends _$HomeNotifier {
  @override
  Future<String> build() async {
    return 'Tap the button to call a protected API';
  }

  Future<void> callProtectedApi() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final dio = ref.read(apiClientProvider);
      try {
        final response = await dio.get('/v1/Members');
        return 'Status: ${response.statusCode}\n\n${response.data}';
      } on DioException catch (e) {
        if (e.response != null) {
          return 'Error ${e.response!.statusCode}: ${e.response!.data}';
        }
        rethrow;
      }
    });
  }
}

/// Exposes token info for debugging the PKCE flow.
@riverpod
Future<TokenDebugInfo> tokenDebugInfo(Ref ref) async {
  final storage = ref.watch(tokenStorageProvider);
  final accessToken = await storage.accessToken;
  final refreshToken = await storage.refreshToken;
  final idToken = await storage.idToken;
  final expiry = await storage.expiry;

  return TokenDebugInfo(
    accessToken: accessToken,
    refreshToken: refreshToken,
    idToken: idToken,
    expiry: expiry,
  );
}

class TokenDebugInfo {
  const TokenDebugInfo({
    this.accessToken,
    this.refreshToken,
    this.idToken,
    this.expiry,
  });

  final String? accessToken;
  final String? refreshToken;
  final String? idToken;
  final DateTime? expiry;

  String truncateToken(String? token) {
    if (token == null) return 'No token';
    if (token.length <= 30) return token;
    return '${token.substring(0, 20)}…${token.substring(token.length - 10)}';
  }

  String get expiryDisplay {
    if (expiry == null) return 'Unknown';
    final remaining = expiry!.difference(DateTime.now());
    if (remaining.isNegative) return 'EXPIRED';
    return '${remaining.inMinutes}m ${remaining.inSeconds % 60}s remaining';
  }
}
