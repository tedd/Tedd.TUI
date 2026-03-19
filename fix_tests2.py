import os
import re

filepath = 'src/Tedd.TUI.Tests/BorderCoverageTests.cs'
with open(filepath, 'r') as f:
    content = f.read()

# Fix SetProperty
content = re.sub(r'SetProperty\("VerticalScrollBarVisibility", ScrollBarVisibility.Visible\)', 'SetProperty("VerticalScrollBarVisibility", (object)ScrollBarVisibility.Visible)', content)
content = re.sub(r'SetProperty\("HorizontalScrollBarVisibility", ScrollBarVisibility.Visible\)', 'SetProperty("HorizontalScrollBarVisibility", (object)ScrollBarVisibility.Visible)', content)
content = re.sub(r'SetProperty\("VerticalScrollBarVisibility", ScrollBarVisibility.Hidden\)', 'SetProperty("VerticalScrollBarVisibility", (object)ScrollBarVisibility.Hidden)', content)
content = re.sub(r'SetProperty\("HorizontalScrollBarVisibility", ScrollBarVisibility.Hidden\)', 'SetProperty("HorizontalScrollBarVisibility", (object)ScrollBarVisibility.Hidden)', content)

with open(filepath, 'w') as f:
    f.write(content)
print(f"Updated {filepath}")
