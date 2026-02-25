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

## 2025-05-26 - Documentation Synchronization Protocol
**Observation:**
- **Documentation Drift:** The `README.md` requires synchronization with recent API capabilities, specifically for `Grid`, `Table`, `MarkdownView`, and the Data Binding infrastructure.
- **Architectural Gaps:** The "Usage Example" and "Architecture" sections lack detailed explanations of hierarchical data binding (INotifyPropertyChanged inheritance) and `Grid` star sizing.
- **Verification:** Verified APIs for `Table` (pagination, sorting), `Grid` (Rows/Cols), `MarkdownView`, and `XamlLoader`.

**Strategic Action:**
- Update `README.md` to mandate `.NET 10.0 SDK`.
- Expand "Features" to include `Grid`, `Table`, and `MarkdownView`.
- Add a dedicated "Data Binding" architectural section with code examples.
- Detail the `Grid` layout mechanics (Star sizing).
- Reinforce performance messaging (Zero-Allocation, VirtualBuffer).
## 2025-05-26 - Documentation Synchronization Protocol
**Observation:**
- **Framework Target:** The current `README.md` documentation has not been fully updated to reflect the mandatory .NET 10.0 prerequisite.
- **Architectural Drift:** The "Usage Example" is overly simplistic and does not demonstrate the framework's core architectural strengths, such as Data Binding and the MVVM pattern.
- **Feature Omission:** The `README.md` lacks explicit sections detailing the "Hierarchical Data Binding", "Zero-Allocation Rendering", and "Event-Driven Loop" which are critical differentiators.
- **Structural Clarity:** The distinction between the core `Tedd.TUI` library and the `Tedd.TUI.Platform.Console` implementation is not clearly articulated.

**Strategic Action:**
- Explicitly state `.NET 10.0 SDK` as a prerequisite.
- Expand the "Features" list to include Data Binding, Zero-Allocation Rendering, and Event-Driven Loop.
- Add a detailed "Architecture" section explaining the Visual Tree, Dependency Properties, Routed Events, and the Render Loop.
- Replace the "Getting Started" example with a robust `Program.cs` that demonstrates `TuiApp`, `TuiWindow`, and a `ViewModel` with Data Binding.
- Clarify the separation between `Tedd.TUI` (Core) and `Tedd.TUI.Platform.Console` (Platform).
