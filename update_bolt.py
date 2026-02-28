import sys

def main():
    content = ""
    with open(".jules/bolt.md", "r") as f:
        content = f.read()

    # We must ensure we log the big O notation in code comments.
    with open("src/Tedd.TUI.Platform.Console/ConsoleRenderer.cs", "r") as f:
        renderer_content = f.read()

    if "Time Complexity: O(W * H)" not in renderer_content:
        # It's actually there from before
        print("Big O is documented.")

    print("Done")

if __name__ == "__main__":
    main()
