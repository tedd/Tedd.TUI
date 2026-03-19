import os
import re

directories = ['src/Tedd.TUI.HumanTests', 'src/Tedd.TUI.Demo', 'src/Tedd.TUI.Platform.Blazor', 'src/Tedd.TUI.Tests']

for d in directories:
    for root, _, files in os.walk(d):
        for file in files:
            if file.endswith('.cs') or file.endswith('.razor'):
                filepath = os.path.join(root, file)
                with open(filepath, 'r') as f:
                    content = f.read()

                new_content = re.sub(r'VerticalScrollBarVisibility\s*=\s*true', 'VerticalScrollBarVisibility = ScrollBarVisibility.Visible', content)
                new_content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*true', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Visible', new_content)
                new_content = re.sub(r'VerticalScrollBarVisibility\s*=\s*false', 'VerticalScrollBarVisibility = ScrollBarVisibility.Hidden', new_content)
                new_content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*false', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden', new_content)

                # Property assignments in object initializers
                new_content = re.sub(r'VerticalScrollBarVisibility\s*=\s*true', 'VerticalScrollBarVisibility = ScrollBarVisibility.Visible', new_content)

                # Check for Assert.Throws<ArgumentException>(() => b.VerticalScrollBarVisibility = (bool)o); etc in BorderCoverageTests
                new_content = re.sub(r'\(bool\)o', '(ScrollBarVisibility)o', new_content)

                # Assert.Equal(false, border.VerticalScrollBarVisibility)
                new_content = re.sub(r'Assert\.Equal\(false,\s*(.*?\.VerticalScrollBarVisibility)\)', r'Assert.Equal(ScrollBarVisibility.Hidden, \1)', new_content)
                new_content = re.sub(r'Assert\.Equal\(true,\s*(.*?\.VerticalScrollBarVisibility)\)', r'Assert.Equal(ScrollBarVisibility.Visible, \1)', new_content)
                new_content = re.sub(r'Assert\.Equal\(false,\s*(.*?\.HorizontalScrollBarVisibility)\)', r'Assert.Equal(ScrollBarVisibility.Hidden, \1)', new_content)
                new_content = re.sub(r'Assert\.Equal\(true,\s*(.*?\.HorizontalScrollBarVisibility)\)', r'Assert.Equal(ScrollBarVisibility.Visible, \1)', new_content)

                if new_content != content:
                    with open(filepath, 'w') as f:
                        f.write(new_content)
                    print(f"Updated {filepath}")
