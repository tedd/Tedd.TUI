# Codex: Documentation Intelligence

## 2025-05-24 - Documentation Synchronization Protocol
**Observation:**
- **Framework Target:** The current `README.md` specifies `.NET 8.0 SDK` as a prerequisite, whereas the `Tedd.TUI.csproj` explicitly targets `.NET 10.0`.
- **Architectural Drift:** The "Usage Example" demonstrates a manual `Measure`/`Arrange`/`Render` loop, failing to reflect the modern `TuiApp` abstraction used in `Tedd.TUI.Demo`.
- **Feature Omission:** The `README.md` omits critical components such as the Routed Event System (Bubble/Tunnel strategies), `Table` control pagination/sorting features, and `MarkdownView`.
- **Performance Articulation:** The documentation lacks explicit mention of the "Zero-allocation" rendering architecture and `VirtualBuffer` diffing mechanism, which are key selling points.

**Strategic Action:**
- Update `README.md` to explicitly require `.NET 10.0 SDK`.
- Replace the legacy usage example with a concise `TuiApp` implementation.
- Articulate the Routed Event System and Layout Engine mechanics in a dedicated "Architecture" section.
- Expand the "Features" list to include `Table`, `Grid`, `MarkdownView`, and performance characteristics.
- Delineate "XAML Support" as foundational/experimental versus established capabilities.

## 2025-05-25 - Project Structure Alignment
**Observation:**
- **Operational Reality:** The root `Tedd.TUI.sln` solution file contains broken project references, causing `dotnet build` to fail in the root directory. The functional solution file resides in `src/`.
- **Roadmap Drift:** The "XAML Support" feature was listed as a future capability despite being fully implemented and documented with examples.
- **Architectural Omission:** The efficient, event-driven nature of the render loop (using `WaitForMultipleObjects`/`WaitHandle`) was not explicitly documented in the Architecture section.

**Strategic Action:**
- Updated `README.md` build and test instructions to direct users to the `src/` directory.
- Removed "XAML Support" from the Roadmap section.
- Added "Event-Driven Loop" description to the Rendering Pipeline section.
