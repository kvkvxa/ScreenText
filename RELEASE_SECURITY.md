# Release security plan

- The application requests `asInvoker`; it does not require administrator privileges.
- Release builds must verify `src/ScreenText/Assets/tessdata/SHA256SUMS.txt` before publishing.
- GitHub Actions references are pinned to immutable commit SHAs.
- The portable ZIP is accompanied by a SHA-256 checksum.
- `scripts/verify-release.ps1 -RequireAuthenticodeSignature` can gate a release on a valid Authenticode signature.
- Authenticode signing is intentionally not automated in this repository: the signing certificate and private key must stay in a protected release environment. Before public distribution, sign `ScreenText.exe` and document the certificate thumbprint and timestamp authority used for the release.

## Release checklist for v0.1.0 (and later versions)

1. **Tests and build**
   - `dotnet restore ScreenText.sln --locked-mode`
   - `dotnet build ScreenText.sln --configuration Release --no-restore`
   - `dotnet test ScreenText.sln --configuration Release --no-build`
2. **Package**
   - `.\scripts\publish.ps1 -Version <version>`
   - `.\scripts\verify-release.ps1 -Version <version> [-RequireAuthenticodeSignature]`
   - Confirm the ZIP has no `x86` entries and that `Assets/tessdata/{eng,rus}.traineddata` and `SHA256SUMS.txt` are present.
3. **Signing (before public distribution)**
   - Sign `ScreenText.exe` (the extracted `artifacts/release/ScreenText-v<version>-win-x64/ScreenText.exe`) with an Authenticode certificate in a protected environment.
   - Record the certificate thumbprint and the timestamp authority (for example `http://timestamp.digicert.com`) in the release notes.
   - Re-run `verify-release.ps1 -RequireAuthenticodeSignature` after signing.
4. **Manual smoke test**
   - Extract the signed ZIP and run `ScreenText.exe`.
   - Verify: tray icon, `Win + Shift + O` capture to clipboard, image capture, `Esc` cancellation, settings persistence, single-instance activation.
   - Verify at 100%/150% DPI, with left/upper monitors at negative coordinates, mixed-DPI setups, and a monitor disconnect/reconnect.
5. **Publish**
   - Tag the release: `git tag -a v<version> -m "ScreenText <version>"` and push.
   - GitHub Actions (on `v*` tags) builds the package, verifies it, and attaches `*.zip` and `*.sha256` to the GitHub Release.