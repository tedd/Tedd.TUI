import re

with open("src/Tedd.TUI.Tests/GridSplitterTests.cs", "r") as f:
    content = f.read()

# Make HorizontalDrag use VerticalAlignment = Stretch
content = content.replace("var splitter = new GridSplitter();", "var splitter = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };", 1)

# Make VerticalDrag use HorizontalAlignment = Stretch
content = content.replace("var splitter = new GridSplitter();", "var splitter = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };", 1)

with open("src/Tedd.TUI.Tests/GridSplitterTests.cs", "w") as f:
    f.write(content)
