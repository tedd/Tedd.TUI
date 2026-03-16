1. **Goal**: Implement the `Thumb` control as requested by the `Forge` directive.
   - The `Thumb` is a WPF primitive control for drag interactions.
   - Needs to be added to `Tedd.TUI`.
   - Inherits from `Control`.
   - Fires `DragStarted`, `DragDelta`, `DragCompleted` routed events.
   - Manages mouse capture internally for the duration of the drag.
2. **Details**:
   - Create `src/Tedd.TUI/DragEventArgs.cs` defining `DragEventArgs`, `DragStartedEventArgs`, `DragDeltaEventArgs`, `DragCompletedEventArgs`. They inherit from `RoutedEventArgs`.
   - Also create delegate types: `DragStartedEventHandler`, `DragDeltaEventHandler`, `DragCompletedEventHandler`.
   - Create `src/Tedd.TUI/Thumb.cs` defining `Thumb` class.
     - Routed events: `DragStartedEvent` (Bubble), `DragDeltaEvent` (Bubble), `DragCompletedEvent` (Bubble).
     - State tracking: `IsDragging` property (DependencyProperty?), internal mouse state tracking (start point).
     - Override `OnMouseDown`: Capture mouse (`GetRoot() as TuiWindow`) and fire `DragStartedEvent`. Set `IsDragging = true`.
     - Override `OnMouseMove`: If `IsDragging`, calculate delta from last known position, fire `DragDeltaEvent`. Update last known position.
     - Override `OnMouseUp`: If `IsDragging`, release mouse capture, set `IsDragging = false`, fire `DragCompletedEvent`.
     - Handle `OnLostFocus` or other interruption? WPF usually cancels dragging on mouse capture lost.
3. **Tests**:
   - Create `src/Tedd.TUI.Tests/ThumbTests.cs`.
   - Verify events fire with correct arguments.
   - Verify `IsDragging` state changes.
   - Use the Validator persona rules: isolate in container, coordinate validation.
4. **Integration**: Update `ScrollBar` and `Slider` to use `Thumb` in future PRs, but for now just implement `Thumb` itself.
5. **Pre-commit**: Run pre-commit script to format, test, build.
6. **Submit**: Create PR following Forge format.
