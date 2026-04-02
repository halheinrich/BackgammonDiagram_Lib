# Code Review — BackgammonDiagram_Lib

Reviewed at commit `e119998` (2026-04-02).
Each finding links to the file and describes what to do.
Check off items as fixes are committed.

---

## Priority — fix before next feature work

- [x] **#14 `DiagramRenderer` — checker placement bug when `HomeBoardOnRight = false`**
  `AppendCheckers` calls `layout.ColumnCentreX(pt, panelOnLeft)` without passing
  `request.HomeBoardOnRight`, so the default `true` is always used. Checkers render
  in the wrong columns while point triangles render correctly.
  Fix: pass `request.HomeBoardOnRight` as the third argument, matching `AppendPoints`
  and `AppendPointNumbers`.

- [x] **#16 `DiagramRenderer.F()` — not locale-safe; produces invalid SVG on non-English Windows**
  `v.ToString("0.##")` uses the current thread's culture. On a machine where the
  decimal separator is a comma (e.g. German locale), the SVG contains `"14,5"` instead
  of `"14.5"`, making the viewBox unparseable by SkiaSharp.
  Fix: `v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)`

- [x] **#25 `PdfBuilder` — library sets QuestPDF license, silently overwriting caller's commercial license**
  `EnsureLicense()` sets `QuestPDF.Settings.License = LicenseType.Community`. A caller
  already holding a commercial QuestPDF license will be silently downgraded.
  Also has a benign race condition (`static bool` flag without synchronisation).
  Fix options: (a) remove `EnsureLicense` entirely and document that the caller must
  configure the QuestPDF license before use; or (b) only set if not already set, using
  a proper check of the existing license value.

- [x] **#28 `PptxConformanceTests.NamespaceDeclarations_NotScatteredOnChildren` — always fails**
  The test calls `Assert.True(ancestorHasIt)` then immediately `Assert.Fail(...)`.
  If the fix worked (`ancestorHasIt == true`), `Assert.Fail` fires. If it didn't
  (`ancestorHasIt == false`), `Assert.True` fires. The test cannot pass either way and
  provides no regression protection.
  Fix: rewrite to assert that *no descendant* carries a redundant namespace declaration —
  i.e. walk descendants and assert that `a:` and `r:` namespace declarations appear only
  on the root element.

---

## Medium severity

- [x] **#1 `DiagramRequest` — `Mop` and `Dice` array contents are mutable after `Build()`**
  `int[]` properties are `init`-only (reference immutable) but content-mutable.
  `request.Mop[6] = 99` after `Build()` silently corrupts a supposedly immutable object.
  Fix: in `Builder.Build()`, copy arrays defensively before assigning:
  `Mop = Mop.ToArray(), Dice = Dice.ToArray()`.
  Consider exposing as `IReadOnlyList<int>` on the class.

- [x] **#3 `DiagramRequest` — `Plays` and `AnalysisDepths` lists are mutable after `Build()`**
  Same issue as #1. `request.Plays.Add(...)` works silently post-construction.
  Fix: in `Build()`, assign `Plays = new List<PlayCandidate>(Plays)` (or `.AsReadOnly()`),
  and expose the property as `IReadOnlyList<PlayCandidate>`.

- [x] **#12 `BoardHitRegions` — `Points` dictionary is mutable**
  `Dictionary<int, HitRect>` is exposed as `required init` but callers can mutate
  contents after construction.
  Fix: expose as `IReadOnlyDictionary<int, HitRect>`.

---

## Low severity

- [x] **#8 `DiagramSize` — `static` preset properties allocate a new instance on every access**
  `Small`, `Medium`, `Large` are `static` properties (`=> new() { ... }`), not fields.
  A new object is allocated on each call. `ThemeRegistry` uses `static readonly` fields
  correctly; `DiagramSize` should match.
  Fix: change to `public static readonly DiagramSize Small = new() { ... };`

- [x] **#9 `DiagramSize` — `Custom` preset constructible without dimensions**
  `new DiagramSize { Preset = DiagramSizePreset.Custom }` produces a broken size with
  null `CustomWidth`/`CustomHeight`; the renderer silently falls back to 1000px.
  Fix: add validation (matching `DiagramRequest.Builder.Build()` style), or make `Custom`
  the only construction path for that preset.

- [x] **#10 `DiagramOptions` — should be a `record`**
  Pure immutable value object with all `init` properties and no validation. A `record`
  gives value equality and `with`-expression support for free.
  Fix: `public record DiagramOptions { ... }` (update any callers using `==` for
  identity rather than equality).

- [x] **#15 `DiagramRenderer.Darken()` — fragile hex parsing**
  Assumes 6-character uppercase hex after `#`. Will silently corrupt or throw on
  3-char shorthand, lowercase, or RGBA. Safe for now since themes are library-controlled,
  but a custom `ITheme` implementor returning `#rgb` would hit this.
  Fix: add a guard asserting 6-char input, and use
  `int.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber)` instead of
  `Convert.ToInt32(hex[..2], 16)`.

- [x] **#17 `DiagramRenderer` — lazy `IEnumerable` passed to `PptxBuilder.Build` and `PdfBuilder.Build`**
  `requests.Select(r => (RenderPng(r, options), r.Title))` is passed unevaluated.
  If either builder ever iterates twice, each PNG renders twice. Exceptions from
  `RenderPng` are thrown inside the builder, obscuring the stack trace.
  Fix: add `.ToList()` before passing to the builder.

- [x] **#18 `SkiaSharpRasterizer` — `Regex.Match` not cached**
  `ParseViewBox` calls `Regex.Match(string, string)` on every rasterisation.
  Fix: `private static readonly Regex ViewBoxRegex = new(@"...", RegexOptions.Compiled);`
  or use `[GeneratedRegex]` (available .NET 7+).

- [x] **#27 `DiagramRendererTests` — disk-write tests assert only file existence and byte length**
  Tests like `RenderSvg_WritesProblemModeToDisk` provide no semantic regression protection.
  Fix: mark with `[Trait("Category", "Visual")]` and exclude from CI runs, making clear
  these are for manual inspection only. Ensure every rendering path has at least one
  content-asserting test alongside the disk-write test.

---

## Trivial / housekeeping

- [ ] **#4 `DiagramRequest.Validate()` — dead null checks on always-initialised fields**
  `if (Mop == null || ...)` — `Mop` defaults to `new int[26]` on the Builder; the null
  branch is unreachable through normal use. Either remove, or document why it's needed.

- [ ] **#5 `DiagramRequest` — `IsPowerOfTwo` belongs in a utility class, not on `Builder`**
  Pure mathematical function with no dependency on Builder state.
  Move to an internal `MathUtils` or `ValidationHelpers` static class.

- [ ] **#7 `DiagramRequestExtensions` — `base_` is a poor identifier**
  Trailing underscore used to avoid keyword clash with `base`. Rename to `titlePrefix`
  or `prefix`.

- [ ] **#13 `DiagramRenderer` — dead `using` directives**
  `using DocumentFormat.OpenXml.Office2016.Excel;` and
  `using DocumentFormat.OpenXml.Wordprocessing;` are unused. Remove both.

- [ ] **#21 `PptxBuilder` — `TableStylesPart` raw XML declaration not canonical**
  Written without `standalone="yes"`, relying on `FixXmlDeclarations` to correct it.
  Write the canonical declaration to begin with so the fix pass is idempotent.

- [ ] **#22 `PptxBuilder` — `CoreFilePropertiesPart` writer uses implicit encoding**
  `new StreamWriter(stream)` defaults to UTF-8 with BOM on Windows. All other writers
  in the file use `new StreamWriter(stream, new UTF8Encoding(false))` explicitly.
  Fix: make this consistent.

- [ ] **#26 `DiagramRendererTests` — dead `using ExCSS`**
  `ExCSS` is imported but nothing in the file uses it. Remove the `using` and verify
  the NuGet reference is also removed if unused elsewhere.

- [ ] **#29 `HitRegionsTests` — inline `DiagramRequest.Builder` construction duplicates `TestFixtures`**
  `MinimalRequest()` is re-implemented inline with different field values rather than
  delegating to `TestFixtures.MinimalBuilder()`.
  Fix: use `TestFixtures.MinimalBuilder()` and override only what the test needs.

---

## By-design / informational (no action needed)

- **#2 Builder property duplication** — unavoidable consequence of the class + inner Builder
  design decision. Noted in INSTRUCTIONS.md Key Decisions.

- **#6 `ToProblemSolutionPair` full-field enumeration** — same cause as #2. Would collapse
  to a one-liner if `DiagramRequest` were a record with `with`-expressions.

- **#11 `SvgViewBox` / `HitRect` are records, rest of model layer uses classes** — the records
  are correct here. The inconsistency is that several classes *should* also be records (#10).

- **#19 `ParseViewBox` regex fragility** — acceptable because the SVG is hand-rolled and the
  format is fully under library control.

- **#20 `PptxBuilder.Build` length** — the post-processing fix methods are already well-extracted.
  The main method could extract a `AddRequiredParts` helper but this is cosmetic.

- **#23 `NewId()` result always overwritten** — consequence of working around the OpenXml SDK.
  Explained in the `Build` comment block.

- **#24 `FixRelationshipIds` string replacement** — XML-parser-based replacement would be
  significantly more complex. The string approach is safe for known SDK output patterns.