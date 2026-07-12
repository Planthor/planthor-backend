# Dependencies

When you install with a package manager, these libraries are installed for you.

But, if you manually install .NET or you publish a self-contained app, you'll need to make sure these libraries are installed:

- ca-certificates
- libc6
- libgcc-s1
- libicu74
- liblttng-ust1
- libssl3
- libstdc++6
- libunwind8
- zlib1g

Dependencies can be installed with the apt install command. The following snippet demonstrates installing the zlib1g library:

```bash
sudo apt install zlib1g
```
