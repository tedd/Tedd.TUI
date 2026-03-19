import os
import re

tests_dir = 'src/Tedd.TUI.Tests'

for root, _, files in os.walk(tests_dir):
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            with open(filepath, 'r') as f:
                content = f.read()

            new_content = re.sub(r'VerticalScrollBarVisibility\s*=\s*true', 'VerticalScrollBarVisibility = ScrollBarVisibility.Visible', content)
            new_content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*true', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Visible', new_content)
            new_content = re.sub(r'VerticalScrollBarVisibility\s*=\s*false', 'VerticalScrollBarVisibility = ScrollBarVisibility.Hidden', new_content)
            new_content = re.sub(r'HorizontalScrollBarVisibility\s*=\s*false', 'HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden', new_content)

            # For BorderCoverageTests method call args
            new_content = re.sub(r'SetProperty\("VerticalScrollBarVisibility", true\)', 'SetProperty("VerticalScrollBarVisibility", ScrollBarVisibility.Visible)', new_content)
            new_content = re.sub(r'SetProperty\("HorizontalScrollBarVisibility", true\)', 'SetProperty("HorizontalScrollBarVisibility", ScrollBarVisibility.Visible)', new_content)
            new_content = re.sub(r'SetProperty\("VerticalScrollBarVisibility", false\)', 'SetProperty("VerticalScrollBarVisibility", ScrollBarVisibility.Hidden)', new_content)
            new_content = re.sub(r'SetProperty\("HorizontalScrollBarVisibility", false\)', 'SetProperty("HorizontalScrollBarVisibility", ScrollBarVisibility.Hidden)', new_content)

            if new_content != content:
                with open(filepath, 'w') as f:
                    f.write(new_content)
                print(f"Updated {filepath}")
