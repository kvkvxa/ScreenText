# ScreenText

Small open-source Windows tray utility for local OCR:

`Win + Shift + O` → select a screen region → recognize text locally → copy to Clipboard.

## Requirements

- Windows 10/11 x64
- No account or network connection
- OCR models are bundled with the application (`tessdata_best`)

## Run a release build

1. Download `ScreenText-v0.1.0-win-x64.zip`.
2. Verify the accompanying `.sha256` checksum.
3. Extract the ZIP to a folder.
4. Run `ScreenText.exe`.

The application starts in the system tray. Use the tray menu to capture text, capture a screenshot directly to the image Clipboard, open Settings, or exit. Settings also allow changing the global hotkey and startup behavior. Launching a second instance activates the existing one.

After startup, the selected OCR engine is warmed up in the background. This keeps model initialization out of the first capture path; a very quick capture immediately after launch may still wait for that one-time initialization.

## Language models

The language setting accepts one or more Tesseract model codes separated by `+`, for example `eng+rus`, `spa+chi_sim`, or `auto`. A valid code is never silently replaced with English or Russian: the corresponding `*.traineddata` file must be present and listed in `Assets/tessdata/SHA256SUMS.txt`.

`auto` uses Tesseract OSD to detect writing systems in the captured image and its vertical regions, then selects matching local models. It fails closed when a detected Chinese/Japanese/etc. script has no matching model, instead of fabricating it as Latin or Cyrillic. The current repository bundles `eng` and `rus`. Additional models can be added from Settings with **Add model...**; they are stored under `%LocalAppData%\ScreenText\models` and are not downloaded by the application. Automatic distinction between languages that share Latin script (for example Spanish and French) requires their respective language models.

## Build from source

```powershell
dotnet restore ScreenText.sln --locked-mode
dotnet build ScreenText.sln --configuration Release
dotnet test ScreenText.sln --configuration Release
```

Create a portable self-contained package:

```powershell
.\scripts\publish.ps1
```

Verify a generated release package:

```powershell
.\scripts\verify-release.ps1 -Version 0.1.0
```

## Manual smoke test

1. Extract the release ZIP and run `ScreenText.exe`.
2. Confirm the ScreenText icon and startup notification appear in the tray.
3. Use `Win + Shift + O`; confirm the screen dims, the cursor becomes a crosshair, and the selection hint appears.
4. Drag over English or Russian text, release the mouse, and paste it with `Ctrl + V`.
5. Open the tray menu, choose `Capture screenshot`, select an area, and paste it into Paint.
6. Press `Esc` during selection and confirm that the application remains available.
7. Open Settings, press a new shortcut in the key field, save, restart the application, and confirm the setting persists.

For the full manual matrix, also verify 100%/150% DPI, left and upper monitors with negative coordinates, mixed-DPI monitors, cross-monitor selection, monitor disconnect/reconnect, fullscreen video/browser content, and light/dark themes. Add real OCR fixtures under `test-data/ocr` before relying on corpus results.

## Privacy

ScreenText is local-only. It has no accounts, telemetry, analytics, cloud OCR, OCR history, screenshot history, or clipboard history. Screenshots exist only in memory during the current capture and are never written to disk. The Windows Clipboard or third-party clipboard managers may retain content after ScreenText copies it.

## Architecture

WPF + Win32 P/Invoke + GDI BitBlt + Tesseract 5.x. Capture coordinates are physical pixels, with one overlay window per monitor and PerMonitorV2 DPI awareness.

## License

ScreenText source is MIT licensed. See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
