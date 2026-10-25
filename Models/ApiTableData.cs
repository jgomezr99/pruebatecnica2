namespace ZiurInventario.Models;

public sealed record ApiTableData(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    string JsonPath,
    string RootKind);
