import re

filepath = 'src/Tedd.TUI.Platform.Blazor/Components/TuiScrollViewer.cs'
with open(filepath, 'r') as f:
    content = f.read()

content = re.sub(r'VerticalScrollBarVisibility\s*=\s*true', 'VerticalScrollBarVisibility = ScrollBarVisibility.Visible', content)
content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*true', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Visible', content)
content = re.sub(r'VerticalScrollBarVisibility\s*=\s*false', 'VerticalScrollBarVisibility = ScrollBarVisibility.Hidden', content)
content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*false', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden', content)

with open(filepath, 'w') as f:
    f.write(content)
