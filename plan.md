1. **Implement `DragStartedEventArgs`, `DragDeltaEventArgs`, and `DragCompletedEventArgs`**:
    - Create a new file `src/Tedd.TUI/DragEventArgs.cs`.
    - These classes will inherit from `RoutedEventArgs` and encapsulate horizontal and vertical offset changes, aligned with typical drag payload expectations, allowing us to pass the offset to handlers in a structured way. `DragStartedEventArgs` has `HorizontalOffset` and `VerticalOffset`. `DragDeltaEventArgs` and `DragCompletedEventArgs` have `HorizontalChange` and `VerticalChange`. These align with standard .NET / WPF implementation for Thumb events.

2. **Create the `Thumb` primitive control**:
    - Create a new file `src/Tedd.TUI/Thumb.cs`.
    - Inherit from `Control`.
    - Define bubbling routed events: `DragStartedEvent`, `DragDeltaEvent`, `DragCompletedEvent` registered via `RoutedEvent.Register`.
    - Provide standard .NET event wrappers (`public event RoutedEventHandler DragStarted` etc).
    - Implement dragging mechanics by handling the inherited virtual handlers `OnMouseDown`, `OnMouseMove`, and `OnMouseUp`.
    - Use the inherited `GetRoot()` method to access the `TuiWindow` to capture the mouse pointer (`TuiWindow.CaptureMouse(this)`) when `OnMouseDown` occurs, and subsequently release capture (`TuiWindow.ReleaseMouseCapture()`) on `OnMouseUp`.
    - Use the provided `MouseEventArgs.GlobalX` and `GlobalY` to calculate the changes as the mouse moves across coordinates. Ensure that mouse coordinate properties match standard expectations.

3. **Verify Created Files**:
    - Use `list_files` or `read_file` to confirm `Thumb.cs` and `DragEventArgs.cs` have been successfully created and implemented.

4. **Add Tests**:
    - Create tests in `src/Tedd.TUI.Tests/ThumbTests/ThumbTests.cs`.
    - Verify that `DragStarted`, `DragDelta`, and `DragCompleted` routing events correctly trigger on mouse operations.
    - Validate that `TuiWindow` captures correctly happen during drag states.

5. **Run the project test suite**:
    - Run the project test suite using `dotnet test src/Tedd.TUI.Tests` to verify the new functionality and ensure no regressions.

6. **Log Architectural Changes**:
    - Add an entry reflecting the strategic addition of the `Thumb` control to `.jules/forge.md`.

7. **Complete pre commit steps**
   - Complete pre commit steps to make sure proper testing, verifications, reviews and reflections are done.

8. **Submit the change.**
   - Submit the PR matching the format: Title `🏗️ Forge: [Thumb] Parity Integration`.
