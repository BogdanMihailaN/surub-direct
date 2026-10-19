using System.Text;
using SurubDirect.BuildingBlocks;
namespace SurubDirect.Store.Application.Imports;
public static class CsvParser
{
    public static IReadOnlyList<CsvRecord> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new BusinessException("Fișierul CSV este gol.");
        if (Encoding.UTF8.GetByteCount(text) > ImportCsvFormat.MaximumBytes)
            throw new BusinessException("Fișierul CSV poate avea maximum 1 MB.", 413);
        text = text.TrimStart('\uFEFF');
        var records = new List<CsvRecord>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var closed = false;
        var line = 1;
        var start = 1;
        void EndField() { fields.Add(field.ToString().Trim()); field.Clear(); closed = false; }
        void EndRecord()
        {
            EndField();
            if (fields.Count != 1 || fields[0].Length != 0) records.Add(new(start, fields.ToArray()));
            fields.Clear();
            if (records.Count > ImportCsvFormat.MaximumRows + 1) throw new BusinessException("Maximum 500 de rânduri de date per import.");
        }
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\0' || c == '\uFFFD') throw new BusinessException($"Linia {line}: fișierul trebuie codificat UTF-8, fără caractere nule.");
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else { quoted = false; closed = true; }
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    field.Append('\n'); line++;
                }
                else field.Append(c);
                continue;
            }
            if (c == ';') { EndField(); continue; }
            if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                EndRecord(); line++; start = line; continue;
            }
            if (closed) throw new BusinessException($"Linia {line}: după ghilimeaua finală trebuie separator sau sfârșit de rând.");
            if (c == '"')
            {
                if (field.Length != 0) throw new BusinessException($"Linia {line}: ghilimelele trebuie să încadreze întregul câmp.");
                quoted = true;
            }
            else field.Append(c);
        }
        if (quoted) throw new BusinessException($"Linia {start}: câmp cu ghilimele neînchise.");
        if (fields.Count > 0 || field.Length > 0 || closed) EndRecord();
        return records;
    }
}
