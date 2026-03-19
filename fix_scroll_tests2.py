import re

filepath = 'src/Tedd.TUI.Tests/ScrollViewerTests.cs'
with open(filepath, 'r') as f:
    content = f.read()

content = re.sub(r'sv\.HorizontalScrollBarVisibility = ScrollBarVisibility\.Hidden;', 'sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;', content)
content = re.sub(r'sv\.VerticalScrollBarVisibility = ScrollBarVisibility\.Hidden;', 'sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;', content)

with open(filepath, 'w') as f:
    f.write(content)
