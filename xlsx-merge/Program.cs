using System;

using System.Data;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Office.Interop.Excel;
using System.Reflection;


namespace xlsx_merge
{
    class Program
    {
        static void Main(string[] args)
        {

            string csvOutputFilePath = System.Configuration.ConfigurationSettings.AppSettings["csvOutputFilePath"];
            string xlsxFilesFolderPath = System.Configuration.ConfigurationSettings.AppSettings["xlsxFilesFolderPath"];
            string FirstCellHeader = "";

            int i = 0;
            string Val = "";
            System.Data.DataTable dt = null;
            System.Data.DataTable dtMerged = new System.Data.DataTable("Metadata");
            DataRow dr = null;
            Hashtable ht = null;
            Queue headers = new Queue();
            StringBuilder sb = new StringBuilder();
            StringBuilder sbFileRemoveSpaces = new StringBuilder();
            String[] columns = null;
            string line = "";
            //object[] columnNames = null;
            DirectoryInfo dir = new DirectoryInfo(xlsxFilesFolderPath);
            StringBuilder xslxRow = new StringBuilder();
            Application xlApp=null;
            Workbook xlWorkbook;
            Worksheet xlWorksheet;
            Range xlRange;
            foreach (FileInfo f in dir.GetFiles())
            {
                Console.WriteLine("processing file " + f.Name);
                try
                {
                    int r = 1;
                    xlApp = new Application();
                    xlWorkbook = xlApp.Workbooks.Open(f.FullName);
                    xlWorksheet = xlWorkbook.Sheets[1];
                    xlRange = xlWorksheet.UsedRange;

                    int rowCount = xlRange.Rows.Count;
                    int colCount = xlRange.Columns.Count;

                    for (r = 1; r <= rowCount; r++)
                    {
                        xslxRow.Clear();
                        for (int j = 1; j < colCount; j++)
                        {
                            if (xlRange.Cells[r, j].Value2!=null)
                                xslxRow.Append(xlRange.Cells[r, j].Value2.ToString());
                            xslxRow.Append(',');
                        }

                        line = xslxRow.ToString().Remove(xslxRow.ToString().Length - 1);




                        if (line.Trim().Length > 0)
                        {
                            line = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(line));
                            // if it is not a header, i.e not start of a new table, add a new row to target
                            //if (!line.StartsWith(FirstCellHeader))
                            if (r != 1)
                            {
                                if (dt == null)
                                    dt = new System.Data.DataTable();
                                dt.Rows.Add(dt.NewRow());
                            }
                            else //if it is a header i.e. start of new table
                            {
                                // merge with existing table, if it exists
                                if (dt != null)
                                    dtMerged.Merge(dt, true, MissingSchemaAction.Add);
                                ht = new Hashtable();
                                dt = new System.Data.DataTable();


                            }


                            sbFileRemoveSpaces.Append(line);
                            sbFileRemoveSpaces.Append(@"
");
                            columns = line.Split(',');

                            i = 0;

                            //foreach (string s in columns)
                            for(i=0;i<columns.Length;i++)
                            {
                                 {
                                    Console.WriteLine("In Row: " + r.ToString());

                                    var s = columns[i];
                                    if (r == 1)
                                    //line.StartsWith(FirstCellHeader))
                                    {
                                        if (string.IsNullOrEmpty(s))
                                            s = "Col" + i.ToString();

                                        if (dt.Columns.Contains(s) == false)
                                        {
                                            dt.Columns.Add(s);
                                            ht.Add(i, s);
                                        }
                                        else // to solve the problem of columns with same name
                                        {
                                            if (dt.Columns.Contains(s + i.ToString()) == false)
                                            {
                                                dt.Columns.Add(s + i.ToString());
                                                ht.Add(i, s + i.ToString());
                                            }
                                        }


                                        if (headers.Contains(s) == false)
                                        {
                                            headers.Enqueue(s);

                                            sb.Append(s);
                                            sb.Append(@"
");
                                        }
                                    }

                                    else
                                    {
                                        Val = s;
                                        if (s.IndexOf('#') > 0)
                                            Val = s.Substring(1 + s.IndexOf('#'));

                                        if (i < dt.Columns.Count && !string.IsNullOrEmpty(ht[i].ToString()))
                                            dt.Rows[dt.Rows.Count - 1][ht[i].ToString()] = Val;//dt.Columns[i].ColumnName
                                    }
                                }
                                //i++;
                            }

                        }
                    }

                    sbFileRemoveSpaces.Append(@"
");


                }
                catch (Exception e){
                    Console.WriteLine("Error: " + e.Message);
                }
                finally {
                    xlApp.Quit(); }
            }
            StreamWriter sw = new StreamWriter(csvOutputFilePath + @"_columns.txt");
            sw.Write(sb.ToString());
            sw.Flush();
            sw.Close();
            /*
                        sw = new StreamWriter(@"c:\test\allmetadata.csv");
                        sw.Write(sbFileRemoveSpaces.ToString());
                        sw.Flush();
                        sw.Close();
                        */
            //dtMerged.WriteXml(@"C:\test\SIG\new_csv_files\logs\allmetadata.xml");
            Console.WriteLine("writing results file ");
            StringBuilder sbFinalData = new StringBuilder();
            for (int colIdx = 0; colIdx < dtMerged.Columns.Count; colIdx++)
            {
                sbFinalData.Append("");
                sbFinalData.Append(dtMerged.Columns[colIdx].ColumnName);
                if (colIdx < dtMerged.Columns.Count - 1)
                    sbFinalData.Append(",");
            }

            sbFinalData.Append(@"
");
            foreach (DataRow drData in dtMerged.Rows)
            {
                if (!string.IsNullOrEmpty(drData[1].ToString()))
                {
                    for (int colIdx = 0; colIdx < dtMerged.Columns.Count; colIdx++)
                    {
                        sbFinalData.Append("");
                        sbFinalData.Append(drData[colIdx].ToString());
                        if (colIdx < dtMerged.Columns.Count - 1)
                            sbFinalData.Append(",");
                    }

                    sbFinalData.Append(@"
");
                }

            }
            sw = new StreamWriter(csvOutputFilePath);
            sw.Write(sbFinalData.ToString());
            sw.Flush();
            sw.Close();

        }

    }
}