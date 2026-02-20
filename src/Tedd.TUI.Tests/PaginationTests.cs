using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class PaginationTests
    {
        [Fact]
        public void ShortWidth_ReturnsStatusString()
        {
            var table = new Table();
            table.PageSize = 10;
            // Set TotalRows such that TotalPages = 100
            table.TotalRows = 1000;
            table.CurrentPage = 50;

            // < 51 of 100 > length is 13
            // If availableWidth < 13, returns < >
            // If availableWidth >= 13 but <= 30, returns < 51 of 100 >

            Assert.Equal("< >", table.GetPaginationString(10, 100));
            Assert.Equal("< 51 of 100 >", table.GetPaginationString(20, 100));
        }

        [Fact]
        public void Detailed_Fit_ReturnsDetailedString()
        {
            var table = new Table();
            table.PageSize = 10;
            table.TotalRows = 1000; // 100 pages
            table.CurrentPage = 50; // Page 51

            // Logic: 1 ... 49 50 [51] 52 53 ... 100
            // "< 1 ... 49 50 [51] 52 53 ... 100 >"
            // Length check:
            // < (2) + 1 (2) + ... (4) + 49 (3) + 50 (3) + [51] (4) + 52 (3) + 53 (3) + ... (4) + 100 (4) + > (2)
            // 2+2+4+3+3+4+3+3+4+4+2 = 34 chars approx.

            string expected = "< 1 ... 49 50 [51] 52 53 ... 100 >";
            string actual = table.GetPaginationString(100, 100);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Detailed_NoFit_ReturnsStatusString()
        {
            var table = new Table();
            table.PageSize = 10;
            table.TotalRows = 1000;
            table.CurrentPage = 50;

            // Detailed string is ~34 chars.
            // If availableWidth is 31 (trigger detailed check) but < 34, should return status string.

            string status = "< 51 of 100 >";
            // width 32
            string actual = table.GetPaginationString(32, 100);

            Assert.Equal(status, actual);
        }

        [Fact]
        public void SmallTotalPages_NoDots()
        {
            var table = new Table();
            table.PageSize = 10;
            table.TotalRows = 50; // 5 pages
            table.CurrentPage = 2; // Page 3

            // Logic: 1 2 [3] 4 5
            // "< 1 2 [3] 4 5 >"

            string expected = "< 1 2 [3] 4 5 >";
            string actual = table.GetPaginationString(100, 5);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FirstPage()
        {
            var table = new Table();
            table.PageSize = 10;
            table.TotalRows = 1000;
            table.CurrentPage = 0; // Page 1

            // Logic: [1] 2 3 ... 100
            // "< [1] 2 3 ... 100 >"

            string expected = "< [1] 2 3 ... 100 >";
            string actual = table.GetPaginationString(100, 100);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LastPage()
        {
            var table = new Table();
            table.PageSize = 10;
            table.TotalRows = 1000;
            table.CurrentPage = 99; // Page 100

            // Logic: 1 ... 98 99 [100]
            // "< 1 ... 98 99 [100] >"

            string expected = "< 1 ... 98 99 [100] >";
            string actual = table.GetPaginationString(100, 100);

            Assert.Equal(expected, actual);
        }
    }
}
