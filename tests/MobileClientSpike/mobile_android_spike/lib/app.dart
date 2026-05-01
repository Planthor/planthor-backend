import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class App extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authNotifierProvider);

    return MaterialApp(
      home: authState.when(
        loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
        error:   (e, _) => LoginScreen(error: e.toString()),
        data:    (status) => switch (status) {
          AuthStatus.authenticated   => const HomeScreen(),
          AuthStatus.unauthenticated => const LoginScreen(),
          AuthStatus.unknown         => const SplashScreen(),
        },
      ),
    );
  }
}
