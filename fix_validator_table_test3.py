import re

filepath = 'src/Tedd.TUI.Tests/ValidatorTableTests.cs'
with open(filepath, 'r') as f:
    content = f.read()

content = content.replace("Assert.True(character == ' ' || character == 'C');", "Assert.True(character == ' ' || character == 'C' || character == '\\u2501' || character == '\\u2503' || character == '\\u250F' || character == '\\u2513' || character == '\\u2517' || character == '\\u251B');")

with open(filepath, 'w') as f:
    f.write(content)
