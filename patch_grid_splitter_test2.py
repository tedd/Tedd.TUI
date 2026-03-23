import re

with open("src/Tedd.TUI.Tests/GridSplitterTests.cs", "r") as f:
    content = f.read()

# Fix both tests by making them use the explicit alignment
content = content.replace("var splitter = new GridSplitter();", "var splitter = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };")

with open("src/Tedd.TUI.Tests/GridSplitterTests.cs", "w") as f:
    f.write(content)
