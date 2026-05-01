namespace FormSqlTranslator.Models;

public enum SqlBlockType
{
    PlainSql,
    AnonymousBlock
}

public sealed record ExtractedSqlBlock(
    string BlockId,
    string FilePath,
    string ComponentType,
    string? ComponentName,
    string? Condition,
    int Order,
    string Sql,
    string OriginPath,
    SqlBlockType BlockType,
    bool IsTranslatedBranch)
{
    public string SafeFileId => BlockId.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
}
