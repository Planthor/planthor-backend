import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'auth_notifier.g.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

@riverpod
class AuthNotifier extends _$AuthNotifier {
  @override
  Future<AuthStatus> build() async {
    final storage = ref.read(tokenStorageProvider);
    return await storage.hasValidToken
        ? AuthStatus.authenticated
        : AuthStatus.unauthenticated;
  }

  Future<void> login() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(authServiceProvider).login();
      return AuthStatus.authenticated;
    });
  }

  Future<void> logout() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(authServiceProvider).logout();
      return AuthStatus.unauthenticated;
    });
  }
}
