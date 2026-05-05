import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'features/auth/auth_notifier.dart';
import 'features/auth/login_screen.dart';
import 'features/home/home_screen.dart';

class App extends ConsumerWidget {
  const App({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);

    return MaterialApp(
      title: 'Planthor Spike',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.teal),
        useMaterial3: true,
      ),
      home: authState.when(
        loading: () => const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        ),
        error: (e, _) => LoginScreen(error: e.toString()),
        data: (status) => switch (status) {
          AuthStatus.authenticated   => const HomeScreen(),
          AuthStatus.unauthenticated => const LoginScreen(),
        },
      ),
    );
  }
}
