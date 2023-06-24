using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace uwu_mew_mew.Misc;

public static class XmlQuery
{
    private static readonly DataTable dataTable;

    static XmlQuery()
    {
        var files = Directory.GetFiles("D:/rchatgpt/messages");
        
        var messages = files.SelectMany(file => XDocument.Load(file).Root.Elements()).Where(e => e.Attribute("content") != null && !e.Attribute("content").Value.Contains('\n')&& e.Attribute("content").Value.Length < 150);
        
        if(!messages.Any())
            messages = files.SelectMany(file => XDocument.Load(file).Root.Elements());

        dataTable = new DataTable();
        var firstElement = true;

        foreach (var result in messages)
        {
            if (firstElement)
            {
                foreach (var attribute in result.Attributes())
                {
                    dataTable.Columns.Add(attribute.Name.ToString());
                }
                firstElement = false;
            }

            var newRow = dataTable.NewRow();

            foreach (var attribute in result.Attributes())
            {
                newRow[attribute.Name.ToString()] = attribute.Value;
            }

            dataTable.Rows.Add(newRow);
        }
        
        GC.Collect();
    }

    public static DataTable Query(string query)
    {
        var dataView = dataTable.DefaultView;
        dataView.RowFilter = query;

        var filteredTable = dataView.ToTable();
        return filteredTable;
    }
}