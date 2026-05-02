namespace QuickBite.QualityGates.Tests;

internal static class RepositoryPaths
{
    public static string File(params string[] segments)
    {
        return Path.Combine(FindRoot(), Path.Combine(segments));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (System.IO.File.Exists(Path.Combine(directory.FullName, "QuickBite.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the QuickBite repository root.");
    }
}
