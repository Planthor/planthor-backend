import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';


import '../auth/auth_notifier.dart';
import 'home_notifier.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final homeState = ref.watch(homeProvider);
    final tokenInfo = ref.watch(tokenDebugInfoProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Planthor — Home'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Logout',
            onPressed: () =>
                ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(tokenDebugInfoProvider);
          await ref.read(homeProvider.notifier).callProtectedApi();
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Status card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.check_circle,
                            color: Theme.of(context).colorScheme.primary),
                        const SizedBox(width: 8),
                        Text(
                          'Authenticated',
                          style: Theme.of(context)
                              .textTheme
                              .titleMedium
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'You are signed in via Keycloak PKCE flow.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),

            // Token debug card
            tokenInfo.when(
              loading: () => const Card(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Center(child: CircularProgressIndicator()),
                ),
              ),
              error: (e, _) => Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text('Token error: $e'),
                ),
              ),
              data: (info) => Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(Icons.token,
                              size: 20,
                              color: Theme.of(context).colorScheme.secondary),
                          const SizedBox(width: 8),
                          Text(
                            'Token Info',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(fontWeight: FontWeight.bold),
                          ),
                          const Spacer(),
                          if (info.accessToken != null)
                            IconButton(
                              icon: const Icon(Icons.copy, size: 18),
                              tooltip: 'Copy full token',
                              onPressed: () {
                                Clipboard.setData(
                                    ClipboardData(text: info.accessToken!));
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(
                                    content: Text('Token copied to clipboard'),
                                    duration: Duration(seconds: 2),
                                  ),
                                );
                              },
                            ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        info.truncatedToken,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              fontFamily: 'monospace',
                              color: Theme.of(context).colorScheme.outline,
                            ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Expires: ${info.expiryDisplay}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Theme.of(context).colorScheme.outline,
                            ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Call API button
            FilledButton.icon(
              onPressed: homeState is AsyncLoading
                  ? null
                  : () => ref
                      .read(homeProvider.notifier)
                      .callProtectedApi(),
              icon: homeState is AsyncLoading
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.api),
              label: const Text('Call Protected API (GET /v1/Members)'),
            ),
            const SizedBox(height: 16),

            // API response
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: homeState.when(
                  loading: () =>
                      const Center(child: CircularProgressIndicator()),
                  error: (e, _) => SelectableText(
                    'Error: $e',
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                  data: (message) => SelectableText(
                    message,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          fontFamily: 'monospace',
                        ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
