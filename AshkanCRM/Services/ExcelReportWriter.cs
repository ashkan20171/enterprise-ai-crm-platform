using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace AshkanCRM.Services;

/// <summary>Creates an Office Open XML workbook without third-party packages.</summary>
public static class ExcelReportWriter
{
    public static byte[] Create(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        if (headers.Count == 0 || headers.Count > 100) throw new ArgumentOutOfRangeException(nameof(headers));
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            Write(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Write(zip, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"" + Escape(sheetName.Length > 31 ? sheetName[..31] : sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Write(zip, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs></styleSheet>");
            var entry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
            using var writer = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = true });
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            writer.WriteStartDocument(); writer.WriteStartElement("worksheet", ns); writer.WriteStartElement("sheetData", ns);
            var rowIndex = 0;
            WriteRow(headers.Select(x => (object?)x).ToArray(), true);
            foreach (var row in rows) { if (row.Count != headers.Count) throw new ArgumentException("Column count mismatch"); WriteRow(row, false); }
            writer.WriteEndElement(); writer.WriteEndElement(); writer.WriteEndDocument();
            void WriteRow(IReadOnlyList<object?> values, bool header)
            {
                rowIndex++; writer.WriteStartElement("row", ns); writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < values.Count; i++)
                {
                    writer.WriteStartElement("c", ns);
                    writer.WriteAttributeString("r", Column(i) + rowIndex.ToString(CultureInfo.InvariantCulture));
                    if (header) writer.WriteAttributeString("s", "1");
                    var v = values[i];
                    if (!header && v is not null && IsNumber(v))
                    {
                        writer.WriteStartElement("v", ns); writer.WriteString(Convert.ToString(v, CultureInfo.InvariantCulture) ?? "0"); writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteAttributeString("t", "inlineStr"); writer.WriteStartElement("is", ns); writer.WriteStartElement("t", ns);
                        writer.WriteString(Convert.ToString(v, CultureInfo.InvariantCulture) ?? "");
                        writer.WriteEndElement(); writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }
        return stream.ToArray();
    }
    private static bool IsNumber(object value) => value is byte or short or int or long or float or double or decimal;
    private static string Column(int index) { var result = ""; for (++index; index > 0; index = (index - 1) / 26) result = (char)('A' + (index - 1) % 26) + result; return result; }
    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? "";
    private static void Write(ZipArchive archive, string path, string value) { using var writer = new StreamWriter(archive.CreateEntry(path, CompressionLevel.Optimal).Open(), new UTF8Encoding(false)); writer.Write(value); }
}
