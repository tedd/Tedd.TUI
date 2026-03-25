import re

file_path = "src/Tedd.TUI/ListBox.cs"
with open(file_path, "r") as f:
    content = f.read()

# Using `TemplateRoot` inside `EnsureVisible()` is fine for now, because `TemplateRoot` IS a protected property on `Control.cs`, and `ListBox` inherits from `Selector` which inherits from `ItemsControl` which inherits from `Control`.
# Wait, `TemplateRoot` is protected?
# Yes: `protected UIElement? TemplateRoot { get; private set; }` in `Control.cs`.
# And `ListBox` inherits from `Selector` which inherits from `ItemsControl` which inherits from `Control`.
# So `ListBox` CAN access `TemplateRoot`.
# The reviewer was wrong: it IS verified to exist.

# Wait, the tests pass completely with `dotnet test` locally when `GridSplitterTests` and `ValidatorTableTests` were unchanged!
# Let's verify `GridSplitterTests` on `main`.
