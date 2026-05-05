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
  final expiry = await storage.expiry;

  return TokenDebugInfo(
    accessToken: accessToken,
    expiry: expiry,
  );
}

class TokenDebugInfo {
  const TokenDebugInfo({this.accessToken, this.expiry});

  final String? accessToken;
  final DateTime? expiry;

  String get truncatedToken {
    if (accessToken == null) return 'No token';
    if (accessToken!.length <= 30) return accessToken!;
    return '${accessToken!.substring(0, 20)}…${accessToken!.substring(accessToken!.length - 10)}';
  }

  String get expiryDisplay {
    if (expiry == null) return 'Unknown';
    final remaining = expiry!.difference(DateTime.now());
    if (remaining.isNegative) return 'EXPIRED';
    return '${remaining.inMinutes}m ${remaining.inSeconds % 60}s remaining';
  }
}
