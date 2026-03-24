using System;

namespace Tedd.TUI.Archive
{
    public class ParseMouseSGRLegacy
    {
        public static (int btn, int x, int y, bool isDown)? ParseMouseSGR_Legacy(string seq)
        {
            try
            {
                var clean = seq.Substring(2); // Remove [<
                var lastChar = clean[clean.Length - 1];
                clean = clean.Substring(0, clean.Length - 1);

                var parts = clean.Split(';');
                if (parts.Length >= 3)
                {
                    int btn = int.Parse(parts[0]);
                    int x = int.Parse(parts[1]) - 1;
                    int y = int.Parse(parts[2]) - 1;

                    bool isDown = (lastChar == 'M');
                    return (btn, x, y, isDown);
                }
            }
            catch { }
            return null;
        }
    }
}
