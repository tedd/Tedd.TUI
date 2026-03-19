import re

filepath = 'src/Tedd.TUI/ScrollViewer.cs'
with open(filepath, 'r') as f:
    content = f.read()

# Fix ScrollViewer measure logic for Auto/Visible
content = re.sub(r'if \(VerticalScrollBarVisibility != ScrollBarVisibility.Disabled\) contentAvailable\.Height = int\.MaxValue;', 'if (VerticalScrollBarVisibility == ScrollBarVisibility.Visible || VerticalScrollBarVisibility == ScrollBarVisibility.Auto) contentAvailable.Height = int.MaxValue;', content)
content = re.sub(r'if \(HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled\) contentAvailable\.Width = int\.MaxValue;', 'if (HorizontalScrollBarVisibility == ScrollBarVisibility.Visible || HorizontalScrollBarVisibility == ScrollBarVisibility.Auto) contentAvailable.Width = int.MaxValue;', content)

# But wait, Hidden means scrollbars are hidden but you CAN scroll (programmatically), so it SHOULD give infinite space.
# Disabled means you CANNOT scroll, so it restricts space.
# So `!= ScrollBarVisibility.Disabled` is correct for infinite space!
# Why did it fail?
# Expected 49, Actual 2147483646 (int.MaxValue - 1)
# Because HorizontalScrollBarVisibility was set to Hidden!
# So contentAvailable.Width was set to int.MaxValue.
# If the test expects it to be constrained, it should be Disabled, or the test is wrong.
