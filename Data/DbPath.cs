namespace Journal.Data;

public static class DbPath
{
    public static string GetSqlitePath()
    {
        var folder = FileSystem.AppDataDirectory;
        return Path.Combine(folder, "journal.db");
    }
}
