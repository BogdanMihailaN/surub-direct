using System.Globalization;
using SurubDirect.BuildingBlocks;
namespace SurubDirect.Store.Application.Imports;
public static class ImportRowReader
{
    public static ImportRow Read(CsvRecord record)
    {
        var f = record.Fields;
        if (f.Count != 17) throw new BusinessException("Sunt necesare exact 17 coloane separate prin punct și virgulă.");
        decimal Number(int index)
        {
            if (!decimal.TryParse(f[index], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var value))
                throw new BusinessException($"{ImportCsvFormat.Header.Split(';')[index]}: număr cu punct zecimal, fără separator de mii.");
            return value;
        }
        int Integer(int index)
        {
            if (!int.TryParse(f[index], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                throw new BusinessException($"{ImportCsvFormat.Header.Split(';')[index]}: număr întreg pozitiv.");
            return value;
        }
        return new(record.Line, f[0], f[1].ToLowerInvariant(), f[2].ToLowerInvariant(), f[3], f[4].ToUpperInvariant(),
            f[5], f[6] == "" ? null : Number(6), f[7] == "" ? null : Number(7), f[8], f[9],
            f[10].ToUpperInvariant(), f[11], Integer(12), Integer(13), Integer(14), Number(15), Number(16));
    }
}
