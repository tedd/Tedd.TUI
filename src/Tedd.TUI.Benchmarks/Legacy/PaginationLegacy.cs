using System;

namespace Tedd.TUI.Benchmarks.Legacy;

public static class PaginationLegacy
{
    // Legacy implementation from Table.cs, made static and taking currentPage as argument
    public static string GetPaginationString(int availableWidth, int totalPages, int currentPage)
    {
        int cp = currentPage + 1;

        // Calculate status string length: "< {cp} of {totalPages} >"
        // "< " (2) + digits(cp) + " of " (4) + digits(totalPages) + " >" (2)
        int statusLen = 8 + GetDigitCount(cp) + GetDigitCount(totalPages);

        if (statusLen > availableWidth)
        {
             return "< >";
        }
        else
        {
            // detailed check
            if (availableWidth > 30)
            {
                 // Try generate detailed string
                 Span<char> buffer = stackalloc char[256];
                 int pos = 0;

                 buffer[pos++] = '<';

                 // Page 1
                 AppendPage(buffer, ref pos, 1, cp);

                 int start = Math.Max(2, cp - 2);
                 int end = Math.Min(totalPages - 1, cp + 2);

                 if (start > 2) AppendDots(buffer, ref pos);

                 for(int i = start; i <= end; i++)
                 {
                     AppendPage(buffer, ref pos, i, cp);
                 }

                 if (end < totalPages - 1) AppendDots(buffer, ref pos);

                 if (totalPages > 1) AppendPage(buffer, ref pos, totalPages, cp);

                 buffer[pos++] = ' ';
                 buffer[pos++] = '>';

                 if (pos <= availableWidth)
                 {
                     return new string(buffer.Slice(0, pos));
                 }
                 else
                 {
                     // Fallback to status string
                     return CreateStatusString(cp, totalPages, statusLen);
                 }
            }
            else
            {
                return CreateStatusString(cp, totalPages, statusLen);
            }
        }
    }

    private static void AppendPage(Span<char> span, ref int pos, int p, int cp)
    {
        if (p == cp)
        {
             // " [{p}]"
             span[pos++] = ' '; span[pos++] = '[';
             p.TryFormat(span.Slice(pos), out int chars);
             pos += chars;
             span[pos++] = ']';
        }
        else
        {
             // " {p}"
             span[pos++] = ' ';
             p.TryFormat(span.Slice(pos), out int chars);
             pos += chars;
        }
    }

    private static void AppendDots(Span<char> span, ref int pos)
    {
        " ...".CopyTo(span.Slice(pos));
        pos += 4;
    }

    private static string CreateStatusString(int cp, int totalPages, int len)
    {
        return string.Create(len, (cp, totalPages), (span, state) =>
        {
            var (c, t) = state;
            span[0] = '<'; span[1] = ' ';
            int written;
            c.TryFormat(span.Slice(2), out written);
            var slice2 = span.Slice(2 + written);
            " of ".CopyTo(slice2);
            t.TryFormat(slice2.Slice(4), out written);
            var slice3 = slice2.Slice(4 + written);
            slice3[0] = ' '; slice3[1] = '>';
        });
    }

    private static int GetDigitCount(int n)
    {
        if (n < 10) return 1;
        if (n < 100) return 2;
        if (n < 1000) return 3;
        if (n < 10000) return 4;
        if (n < 100000) return 5;
        if (n < 1000000) return 6;
        if (n < 10000000) return 7;
        if (n < 100000000) return 8;
        if (n < 1000000000) return 9;
        return 10;
    }
}
