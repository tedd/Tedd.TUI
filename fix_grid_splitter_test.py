import re

filepath = 'src/Tedd.TUI.Tests/GridSplitterTests.cs'
with open(filepath, 'r') as f:
    content = f.read()

content = re.sub(r'var splitter = new GridSplitter\(\) \{ HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center \};', 'var splitter = new GridSplitter();', content)
content = re.sub(r'var splitter = new GridSplitter\(\);', 'var splitter = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };', content, count=1)
content = re.sub(r'var splitter = new GridSplitter\(\);', 'var splitter = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };', content, count=1)

with open(filepath, 'w') as f:
    f.write(content)
