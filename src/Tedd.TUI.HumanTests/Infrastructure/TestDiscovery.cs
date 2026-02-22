using System.Reflection;

namespace Tedd.TUI.HumanTests.Infrastructure;

public static class TestDiscovery
{
    public static List<TestPage> GetAllTests()
    {
        var type = typeof(TestPage);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract && p != type);

        var tests = new List<TestPage>();
        foreach (var t in types)
        {
            try
            {
                var instance = (TestPage)Activator.CreateInstance(t)!;
                tests.Add(instance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to instantiate test {t.Name}: {ex.Message}");
            }
        }
        return tests.OrderBy(t => t.Name).ToList();
    }
}
