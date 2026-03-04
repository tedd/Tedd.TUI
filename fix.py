with open("src/Tedd.TUI.Tests/TextEditorTests.cs", "r") as f:
    text = f.read()

# Home setup
# "WABY||ZVC"
# Cursor at Line 1, Col 2? The test says expected "AWBY||HZCV" which was replaced from "WABY||HZVC".
# If starting is "WABY||ZVC" and we press Home, cursor goes to Line 1 Col 0.
# Then 'H' makes it "WABY||HZVC".
# So the expected text should be "WABY||HZVC", but my previous regex replaced it. Let's fix.
text = text.replace('"AWBY||HZCV"', '"WABY||HZVC"')
text = text.replace('"AWBY||HZCVE"', '"WABY||HZVCE"')

with open("src/Tedd.TUI.Tests/TextEditorTests.cs", "w") as f:
    f.write(text)
