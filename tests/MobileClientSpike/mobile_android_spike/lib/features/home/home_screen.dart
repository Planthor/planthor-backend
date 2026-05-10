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
                color: Theme.of(context).colorScheme.errorContainer,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Token Retrieval Error',
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.onErrorContainer,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        e.toString(),
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.onErrorContainer,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
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
                            'Token Debug Info',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      _TokenRow(
                        label: 'Access Token',
                        value: info.accessToken,
                        displayValue: info.truncateToken(info.accessToken),
                      ),
                      const Divider(height: 24),
                      _TokenRow(
                        label: 'Refresh Token',
                        value: info.refreshToken,
                        displayValue: info.truncateToken(info.refreshToken),
                      ),
                      const Divider(height: 24),
                      _TokenRow(
                        label: 'ID Token',
                        value: info.idToken,
                        displayValue: info.truncateToken(info.idToken),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          const Icon(Icons.timer_outlined, size: 14),
                          const SizedBox(width: 4),
                          Text(
                            'Expires: ${info.expiryDisplay}',
                            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                  color: Theme.of(context).colorScheme.outline,
                                ),
                          ),
                        ],
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

class _TokenRow extends StatelessWidget {
  const _TokenRow({
    required this.label,
    required this.value,
    required this.displayValue,
  });

  final String label;
  final String? value;
  final String displayValue;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              label,
              style: Theme.of(context).textTheme.labelMedium?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                    fontWeight: FontWeight.bold,
                  ),
            ),
            if (value != null)
              SizedBox(
                height: 24,
                width: 24,
                child: IconButton(
                  padding: EdgeInsets.zero,
                  icon: const Icon(Icons.copy, size: 16),
                  tooltip: 'Copy $label',
                  onPressed: () {
                    Clipboard.setData(ClipboardData(text: value!));
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text('$label copied to clipboard'),
                        duration: const Duration(seconds: 2),
                      ),
                    );
                  },
                ),
              ),
          ],
        ),
        const SizedBox(height: 4),
        Text(
          displayValue,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                fontFamily: 'monospace',
                color: Theme.of(context).colorScheme.outline,
              ),
        ),
      ],
    );
  }
}
