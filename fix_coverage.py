import re

filepath = 'src/Tedd.TUI.Tests/BorderCoverageTests.cs'
with open(filepath, 'r') as f:
    content = f.read()

content = re.sub(r'Assert\.True\(border\.HorizontalScrollBarVisibility\);', 'Assert.Equal(ScrollBarVisibility.Visible, border.HorizontalScrollBarVisibility);', content)
content = re.sub(r'Assert\.True\(border\.VerticalScrollBarVisibility\);', 'Assert.Equal(ScrollBarVisibility.Visible, border.VerticalScrollBarVisibility);', content)

with open(filepath, 'w') as f:
    f.write(content)
