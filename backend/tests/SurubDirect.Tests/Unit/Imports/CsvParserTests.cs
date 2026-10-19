using SurubDirect.BuildingBlocks;
using SurubDirect.Store.Application.Imports;
using Xunit;
namespace SurubDirect.Tests;
public sealed class CsvParserTests
{
    [Fact]
    public void Handles_bom_escaped_quotes_delimiters_newlines_and_physical_line_numbers()
    {
        var records = CsvParser.Parse("\uFEFFa;b\r\n\"Șurub; \"\"inox\"\"\nM6\";2\r\nx;y\r\n");
        Assert.Equal(3, records.Count);
        Assert.Equal("Șurub; \"inox\"\nM6", records[1].Fields[0]);
        Assert.Equal(4, records[2].Line);
    }
    [Theory]
    [InlineData("\"open")]
    [InlineData("a\"b;c")]
    [InlineData("\"a\"x;b")]
    [InlineData("a;\u0000")]
    [InlineData("a;\uFFFD")]
    [InlineData("")]
    public void Rejects_malformed_csv(string csv) => Assert.Throws<BusinessException>(() => CsvParser.Parse(csv));
    [Fact]
    public void Enforces_utf8_byte_and_row_limits()
    {
        Assert.Throws<BusinessException>(() => CsvParser.Parse(new string('ș', 600000)));
        Assert.Throws<BusinessException>(() => CsvParser.Parse(string.Concat(Enumerable.Repeat("a;b\n", 502))));
    }
    [Fact]
    public void Rejects_decimal_comma_and_thousands_separators()
    {
        var record = CsvParser.Parse(ImportCsvFormat.Template)[1];
        var fields = record.Fields.ToArray(); fields[15] = "1,50";
        Assert.Throws<BusinessException>(() => ImportRowReader.Read(new(2, fields)));
        fields[15] = "1,000.50";
        Assert.Throws<BusinessException>(() => ImportRowReader.Read(new(2, fields)));
    }
}
