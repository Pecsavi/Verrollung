using System;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows.Forms;
using System.IO;
using NLog;

namespace Verrollungsnachweis
{
    
    class Export
    {
       
        string StartDir = Application.StartupPath;
        bool english=false;
        Excel.Range excelRange = null;
        Excel.Range excelRange2 = null;
        Excel.Range excelRange3 = null;
        Excel.Range excelRange4 = null;
        object[,] cellValuesToWrite = null;
        object[,] cellValuesToWrite2 = null;
        object[,] cellValuesToWrite3 = null;
        object[,] cellValuesToWrite4 = null;
        public HelperFunc RstabFv;
        int InsertableRows;
        public Export(HelperFunc rstab)
         {
            RstabFv=rstab;
         }
        public void ExportToXls()
        {
            string ModelName = RstabFv.modelName;
        
            Excel.Application excelApp = null;
            try
            {
                excelApp = new Excel.Application();
                try
                {

                    string myPath = Path.Combine(StartDir, "Datas", "Verrollung.xltx");

                    if (!File.Exists(myPath))
                    {
                        MessageBox.Show("Template file not found: " + myPath);
                        LoggerService.Error("Template file not found: " + myPath);

                        return;
                    }

   
                    excelApp.Workbooks.Open(myPath);
                }
                catch (Exception)
                {
                    StartDir = Application.StartupPath;

                    string myPath = Path.Combine(StartDir, "Datas", "Verrollung.xltx");

                    excelApp.Workbooks.Open(myPath);
                }
                excelApp.ScreenUpdating = false; // Turn off updating to make it faster
                Excel.Worksheet worksheet = excelApp.Worksheets[1];
                excelApp.Visible = true;
                #region Header
                worksheet.Cells[6, 3] = Environment.UserName;
                worksheet.Cells[5, 3] = DateTime.Today;
                worksheet.Cells[7, 3] = RstabFv.modelName;
                worksheet.Cells[8, 3] = RstabFv._case;
                worksheet.Cells[9, 3] = Math.Round(RstabFv.TDurchmesser, 2);
                worksheet.Cells[10, 3] = Math.Round(RstabFv.deg[0], 2);
                if (RstabFv.deg.Count > 0)
                {
                    worksheet.Cells[11, 3] = Math.Round(RstabFv.deg[1], 2);
                }
                if (ModelName.StartsWith("S"))
                {
                    string result;
                    int index_ = ModelName.IndexOf("A");
                    if (index_ != -1)
                    {
                        result = ModelName.Substring(0, index_ + 1);
                    }
                    else
                    {
                        result = ModelName.Substring(0, Math.Min(6, ModelName.Length));
                    }

                    worksheet.Cells[3, 3] = result;
                }
                if (ModelName.Contains("NL"))
                {
                    worksheet.Cells[4, 3] = ModelName.Substring(ModelName.IndexOf("NL"), 3);
                }
                if (english)
                {
                    worksheet.Cells[3, 2] = "Project:";
                    worksheet.Cells[4, 2] = "Gantry:";
                    worksheet.Cells[5, 2] = "Date:";
                    worksheet.Cells[6, 2] = "Editor:";
                    worksheet.Cells[7, 2] = "Rstab File:";
                    worksheet.Cells[8, 2] = "Remark:";
                }
                worksheet.Cells[1, 10] = (RstabFv.supportRotateDirection); // D16
                #endregion
                #region Gegengewicht
                if (RstabFv.GegengewichtIndex.HasValue)
                {
                    worksheet.Cells[16, 4] = Math.Round(RstabFv.GGforce[0].N, 2); // D16
                    worksheet.Cells[16, 5] = Math.Round(RstabFv.GGforce[0].T, 2); // E16
                    worksheet.Cells[16, 6].FormulaR1C1 = worksheet.Cells[14, 6].FormulaR1C1;
                    worksheet.Cells[16, 7].FormulaR1C1 = worksheet.Cells[14, 7].FormulaR1C1;
                    worksheet.Cells[16, 3].FormulaR1C1 = worksheet.Cells[14, 3].FormulaR1C1;

                  
                    worksheet.Cells[29, 4] = Math.Round(RstabFv.GGforce[1].N, 2); // D29
                    worksheet.Cells[29, 5] = Math.Round(RstabFv.GGforce[1].T, 2); // E29
                    worksheet.Cells[29, 6].FormulaR1C1 = worksheet.Cells[14, 6].FormulaR1C1;
                    worksheet.Cells[29, 7].FormulaR1C1 = worksheet.Cells[14, 7].FormulaR1C1;
                    worksheet.Cells[29, 3].FormulaR1C1 = worksheet.Cells[14, 3].FormulaR1C1;

                }
                #endregion
                #region ExcelRowsInsert
                Excel.Range line = null;
                InsertableRows = RstabFv.q_lastenList.Count;
                line = (Excel.Range)worksheet.Rows[28];
                for (int i = 0; i < InsertableRows; i++)
                {
                    line.Insert();
                    worksheet.Cells[28 + i, 7].FormulaR1C1 = worksheet.Cells[14, 7].FormulaR1C1;
                    worksheet.Cells[28 + i, 6].FormulaR1C1 = worksheet.Cells[14, 6].FormulaR1C1;
                    worksheet.Cells[28 + i, 3].FormulaR1C1 = worksheet.Cells[14, 3].FormulaR1C1;
                }
                line = (Excel.Range)worksheet.Rows[15];
                for (int i = 0; i < InsertableRows; i++)
                {
                    line.Insert();
                    worksheet.Cells[15 + i, 6].FormulaR1C1 = worksheet.Cells[14, 6].FormulaR1C1;
                    worksheet.Cells[15 + i, 7].FormulaR1C1 = worksheet.Cells[14, 7].FormulaR1C1;
                    worksheet.Cells[15 + i, 3].FormulaR1C1 = worksheet.Cells[14, 3].FormulaR1C1;
                }
                #endregion//müködik
                #region insert LCNames
                excelRange = worksheet.Range[worksheet.Cells[15, 2], worksheet.Cells[15 + InsertableRows - 1, 2]];
                excelRange2 = worksheet.Range[worksheet.Cells[28 + InsertableRows, 2], worksheet.Cells[28 + 2 * (InsertableRows) - 1, 2]];
                cellValuesToWrite = new object[InsertableRows, 1];
                int index = 0;
                foreach (var item2 in RstabFv.q_lastenList)
                {
                    cellValuesToWrite[index, 0] = item2.Name;
                    index++;
                }
                excelRange.Value2 = cellValuesToWrite;
                excelRange2.Value2 = cellValuesToWrite;
                #endregion
                #region Insert_Forces
                excelRange3 = worksheet.Range[worksheet.Cells[14, 4], worksheet.Cells[14 + InsertableRows, 5]];
                excelRange4 = worksheet.Range[worksheet.Cells[27 + InsertableRows, 4], worksheet.Cells[27 + 2 * (InsertableRows), 5]];
                cellValuesToWrite3 = new object[InsertableRows + 1, 4];
                cellValuesToWrite4 = new object[InsertableRows + 1, 4];
                int index2 = 0;
               
                int loopLimit = RstabFv.vertikal_Forces.Count;
               
                foreach (var item2 in RstabFv.vertikal_Forces)
                {

                    cellValuesToWrite3[index2, 0] = Math.Round(RstabFv.normal_Forces[index2][0], 2);
                    cellValuesToWrite3[index2, 1] = Math.Round(RstabFv.tangential_Forces[index2][0], 2);
                    cellValuesToWrite4[index2, 0] = Math.Round(RstabFv.normal_Forces[index2][1], 2);    
                    cellValuesToWrite4[index2, 1] = Math.Round(RstabFv.tangential_Forces[index2][1], 2);
                    index2++;
                    if (index2== loopLimit)
                    {
                        break;
                    }
                }
                excelRange3.Value2 = cellValuesToWrite3;
                excelRange4.Value2 = cellValuesToWrite4;
                //Falls ein separates Gegengewicht vorhanden ist, wird es automatisch in die dafür vorgesehenen Zeilen eingetragen (B16 und B29 → D16/E16 und D29/E29).
                #endregion
                #region NotRelavantRows
                Excel.Range excelRange5 = null;
                Excel.Range excelRange6 = null;
                bool match = false;
                bool match2 = false;
                for (int i = 0; i < RstabFv.q_lastenList.Count; i++)
                {
                    foreach (var item2 in RstabFv.RowNumberInExcel[0])
                    {
                        if (i == item2)
                        {
                            match = true;
                            break;
                        }
                    }
                    if (match)
                    {
                        match = false;
                        continue;
                    }
                    excelRange5 = worksheet.Range[worksheet.Cells[15 + i, 2], worksheet.Cells[15 + i, 7]];
                    excelRange5.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                    excelRange5.Font.Italic = true;
                }
                for (int i = 0; i < RstabFv.q_lastenList.Count; i++)
                {
                    foreach (var item3 in RstabFv.RowNumberInExcel[1])
                    {
                        if (i == item3)
                        {
                            match2 = true;
                            break;
                        }
                    }
                    if (match2)
                    {
                        match2 = false;
                        continue;
                    }
                    excelRange6 = worksheet.Range[worksheet.Cells[28+InsertableRows + i, 2], worksheet.Cells[28+InsertableRows + i, 7]];
                    excelRange6.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                    excelRange6.Font.Italic = true;
                }
                #endregion
                #region Summafunction
                char karakter = 'C';
                for (int i = 3; i < 8; i++)
                {
                    if (i==6)
                    {
                        continue;
                    }
                    switch (i)
                    {
                        case 3:
                            karakter = 'C';
                            break;
                        case 4:
                            karakter = 'D';
                            break;
                        case 5:
                            karakter = 'E';
                            break;
                        case 7:
                            karakter = 'G';
                            break;
                    }
                    string addend1 = "="+karakter+"14+"+karakter + (16 + InsertableRows).ToString();
                    string addend2 = "="+karakter+(27+InsertableRows).ToString()+"+"+karakter + (29 + 2*InsertableRows).ToString();
                    foreach (var item2 in RstabFv.RowNumberInExcel[0])
                    {
                        addend1 += "+"+karakter + (15 + item2).ToString();
                    }
                    foreach (var item3 in RstabFv.RowNumberInExcel[1])
                    {
                        addend2 += "+" + karakter + (28+ InsertableRows + item3).ToString();
                    }
                    worksheet.Cells[18 + InsertableRows, i] = addend1;
                    worksheet.Cells[31 + 2*InsertableRows, i] = addend2;
                }
                #endregion
                excelApp.ScreenUpdating = true;
                Marshal.ReleaseComObject(excelApp.Workbooks);
                Marshal.ReleaseComObject(excelApp);
            }
            catch (Exception e)
            {
                Marshal.ReleaseComObject(excelApp.Workbooks);
                Marshal.ReleaseComObject(excelApp);
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;
                string message = e.Message + "- Excel export";
                string caption = "Fehler";
                result = MessageBox.Show(message, caption, buttons);
            }
        }
    }
}
