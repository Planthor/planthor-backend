# Mobile Client Spike

Using a lightweight "throwaway" Flutter app specifically as a developer tool inside the backend repo is a great way to verify the end-to-end token flow locally.

## Flutter Android PKCE Client for Keycloak

The core stack is flutter_appauth (wraps the battle-tested AppAuth Android SDK), flutter_secure_storage, and dio with a smart interceptor.

### Package Stack

```yaml
# pubspec.yaml
dependencies:
  flutter_appauth: ^8.0.1        # PKCE OAuth2 - wraps AppAuth Android SDK
  flutter_secure_storage: ^9.2.2 # Encrypted token persistence (Android Keystore)
  dio: ^5.7.0                    # HTTP client
  riverpod_annotation: ^2.6.1    # State management (or swap for bloc/provider)
  riverpod: ^2.6.1
  flutter_riverpod: ^2.6.1

dev_dependencies:
  riverpod_generator: ^2.6.2
  build_runner: ^2.4.13
```

### Android Native Config

android/app/build.gradle

```groovy
android {
    defaultConfig {
        // ...
        manifestPlaceholders += [
            'appAuthRedirectScheme': 'com.yourcompany.yourapp' // must match redirect URI
        ]
    }
}
```

android/app/src/main/AndroidManifest.xml — AppAuth handles the intent filter automatically via the manifest placeholder above. Just make sure internet permission is present:

```xml
<uses-permission android:name="android.permission.INTERNET"/>
```

### Project Structure
```
lib/
├── main.dart
├── app.dart
│
├── core/
│   ├── config/
│   │   └── app_config.dart          # Keycloak + API base URLs, client ID, scopes
│   │
│   ├── auth/
│   │   ├── auth_service.dart        # PKCE login / logout / token refresh via AppAuth
│   │   ├── token_storage.dart       # Read/write/clear tokens in Android Keystore
│   │   └── auth_interceptor.dart    # Dio interceptor: attach Bearer + silent refresh
│   │
│   └── api/
│       └── api_client.dart          # Dio instance wired with auth interceptor
│
└── features/
    ├── auth/
    │   ├── auth_notifier.dart        # Riverpod notifier: AuthState machine
    │   └── login_screen.dart
    │
    └── home/
        ├── home_notifier.dart        # Calls protected API, handles 401
        └── home_screen.dart
```

###
Reverse port 8180 for keycloak authentication (adb from Android Emulator)

```
.\adb reverse tcp:8180 tcp:8180
```