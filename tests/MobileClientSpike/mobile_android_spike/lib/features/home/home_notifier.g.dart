// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'home_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(HomeNotifier)
final homeProvider = HomeNotifierProvider._();

final class HomeNotifierProvider
    extends $AsyncNotifierProvider<HomeNotifier, String> {
  HomeNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'homeProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$homeNotifierHash();

  @$internal
  @override
  HomeNotifier create() => HomeNotifier();
}

String _$homeNotifierHash() => r'1d85b52b941adc730e8fc4b7db509509ddb872ad';

abstract class _$HomeNotifier extends $AsyncNotifier<String> {
  FutureOr<String> build();
  @$mustCallSuper
  @override
  void runBuild() {
    final ref = this.ref as $Ref<AsyncValue<String>, String>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<AsyncValue<String>, String>,
              AsyncValue<String>,
              Object?,
              Object?
            >;
    element.handleCreate(ref, build);
  }
}

/// Exposes token info for debugging the PKCE flow.

@ProviderFor(tokenDebugInfo)
final tokenDebugInfoProvider = TokenDebugInfoProvider._();

/// Exposes token info for debugging the PKCE flow.

final class TokenDebugInfoProvider
    extends
        $FunctionalProvider<
          AsyncValue<TokenDebugInfo>,
          TokenDebugInfo,
          FutureOr<TokenDebugInfo>
        >
    with $FutureModifier<TokenDebugInfo>, $FutureProvider<TokenDebugInfo> {
  /// Exposes token info for debugging the PKCE flow.
  TokenDebugInfoProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'tokenDebugInfoProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$tokenDebugInfoHash();

  @$internal
  @override
  $FutureProviderElement<TokenDebugInfo> $createElement(
    $ProviderPointer pointer,
  ) => $FutureProviderElement(pointer);

  @override
  FutureOr<TokenDebugInfo> create(Ref ref) {
    return tokenDebugInfo(ref);
  }
}

String _$tokenDebugInfoHash() => r'92fb67516ffe7fd3da466eaab6d115cdd5a296f9';
