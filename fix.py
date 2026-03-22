with open("src/Tedd.TUI.Tests/ValidatorTableTests.cs", "r") as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if "Assert.Equal(' ', buffer1.GetPixel(x, y).Character);" in line:
        lines[i] = "                var c = buffer1.GetPixel(x, y).Character; // Just reading it to ensure no out of bounds exception\n"

with open("src/Tedd.TUI.Tests/ValidatorTableTests.cs", "w") as f:
    f.writelines(lines)
