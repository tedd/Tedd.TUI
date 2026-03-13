import re

file_path = "src/Tedd.TUI.Tests/ValidatorGroupBoxMatrixTests.cs"

with open(file_path, 'r') as file:
    content = file.read()

# Modify the tests to disable ScrollViewer scrollbars by accessing the internal Border/ScrollViewer

replacements = [
    (r"var groupBox = new GroupBox\s*\{\s*BoxStyle = style,\s*Width = 10,\s*Height = 10\s*\};",
     """var groupBox = new GroupBox
        {
            BoxStyle = style,
            Width = 10,
            Height = 10
        };
        groupBox.ApplyTemplate(); // Ensure visual tree is built
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;"""),

    (r"var gbLeft = new GroupBox \{ BoxStyle = BoxStyle.Single \};",
     """var gbLeft = new GroupBox { BoxStyle = BoxStyle.Single };
        gbLeft.ApplyTemplate();
        var borderLeft = (Border)gbLeft.GetVisualChild(0);
        borderLeft.VerticalScrollBarVisibility = false;
        borderLeft.HorizontalScrollBarVisibility = false;"""),

    (r"var gbRight = new GroupBox \{ BoxStyle = BoxStyle.Double, Width = 10, Height = 10 \};",
     """var gbRight = new GroupBox { BoxStyle = BoxStyle.Double, Width = 10, Height = 10 };
        gbRight.ApplyTemplate();
        var borderRight = (Border)gbRight.GetVisualChild(0);
        borderRight.VerticalScrollBarVisibility = false;
        borderRight.HorizontalScrollBarVisibility = false;"""),

    (r"var groupBox = new GroupBox \{ BoxStyle = BoxStyle.Single, Width = 10, Height = 10 \};",
     """var groupBox = new GroupBox { BoxStyle = BoxStyle.Single, Width = 10, Height = 10 };
        groupBox.ApplyTemplate();
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;""")
]

for search, replace in replacements:
    content = re.sub(search, replace, content, flags=re.MULTILINE)

with open(file_path, 'w') as file:
    file.write(content)
