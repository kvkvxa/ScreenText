# Release security plan

- The application requests `asInvoker`; it does not require administrator privileges.
- Release builds must verify `src/ScreenText/Assets/tessdata/SHA256SUMS.txt` before publishing.
- GitHub Actions references are pinned to immutable commit SHAs.
- The portable ZIP is accompanied by a SHA-256 checksum.
- `scripts/verify-release.ps1 -RequireAuthenticodeSignature` can gate a release on a valid Authenticode signature.
- Authenticode signing is intentionally not automated in this repository: the signing certificate and private key must stay in a protected release environment. Before public distribution, sign `ScreenText.exe` and document the certificate thumbprint and timestamp authority used for the release.
