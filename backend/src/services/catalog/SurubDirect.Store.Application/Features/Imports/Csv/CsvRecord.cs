namespace SurubDirect.Store.Application.Imports;
public sealed record CsvRecord(int Line, IReadOnlyList<string> Fields);
