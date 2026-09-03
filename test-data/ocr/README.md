# OCR corpus

The runtime models are bundled in `src/ScreenText/Assets/tessdata` from the official `tessdata_best` repository:

- `eng.traineddata`
- `rus.traineddata`

The automated tests cover normalization, language selection, model integrity, and the corpus harness. Real screenshot corpus files require capture on a Windows desktop and are intentionally not generated from the developer's screen.

For each fixture, add:

- `<name>.png` — the real screenshot crop;
- `<name>.txt` — one expected English/Russian phrase per line.

Suggested fixture names:

- `english-ui.png` / `english-ui.txt`
- `russian-ui.png` / `russian-ui.txt`
- `mixed-en-ru.png` / `mixed-en-ru.txt`
- `small-font.png` / `small-font.txt`
- `dark-theme.png` / `dark-theme.txt`
- `light-theme.png` / `light-theme.txt`
- `high-dpi.png` / `high-dpi.txt`

When fixtures are present, `OcrCorpusTests` runs them with the bundled `eng+rus` models and checks important phrases rather than requiring byte-perfect OCR output. Without fixtures, the test exits without assertions and reports that the corpus is dormant; this keeps CI deterministic while the private desktop screenshots are unavailable.
