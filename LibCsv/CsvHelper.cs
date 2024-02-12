using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibCsv
{
    internal record struct CsvCell(string Data, bool Quoted);

    internal static class CsvHelper
    {
        public static CsvCell[] SplitData(string row)
        {
            StringBuilder sb = new StringBuilder();
            List<CsvCell> cells = new List<CsvCell>();

            bool escape = false;
            bool quote = false;
            bool addedQuote = false;

            foreach (var c in row)
            {
                if (escape)
                {
                    sb.Append(c switch
                    {
                        't' => '\t',
                        'n' => '\n',
                        'r' => '\r',
                        'f' => '\f',
                        _ => c
                    });

                    escape = false;
                }
                else if (quote)
                {
                    if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        quote = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        quote = true;
                        addedQuote = true;
                    }
                    else if (c == ',')
                    {
                        cells.Add(new CsvCell(sb.ToString(), addedQuote));

                        // clear state
                        addedQuote = false;
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            cells.Add(new CsvCell(sb.ToString(), addedQuote));

            return cells.ToArray();
        }

        public static string FormatData(string data)
        {
            StringBuilder sb = new StringBuilder(data.Length);

            bool hasComma = false;
            foreach (var c in data)
            {
                if (c == ',')
                    hasComma = true;

                sb.Append(c switch
                {
                    '\n' => "\\n",
                    '\t' => "\\t",
                    '\r' => "\\r",
                    '\f' => "\\f",
                    '"' => "\\\"",
                    _ => c
                });
            }

            if (hasComma)
            {
                sb.Insert(0, '"');
                sb.Append('"');
            }

            return sb.ToString();
        }
    }
}