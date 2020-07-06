using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

using ARCore.Core;
using ARCore.Types;
using ARCore.Helpers;
using ARCore.RealTimeTools;

namespace ARCore.DataDumpTools
{
    public class ARSheet
    {
        private IServiceProvider Services;
        private ARTimer Timer;
        private ARData Data;

        public ARSheet(IServiceProvider services)
        {
            Timer = services.GetRequiredService<ARTimer>();
            Data = services.GetRequiredService<ARData>();
            Services = services;
        }

        public ExcelWorksheet SetupSheets(ExcelWorkbook book)
        {
            var ConstantSheet = book.Worksheets.Add("World");
            ConstantSheet.Cells[1, 1].Value = "Nations";
            ConstantSheet.Cells[1, 2].Value = Data.NumNations();

            ConstantSheet.Cells[2, 1].Value = "Major Length";
            ConstantSheet.Cells[2, 2].Value = HelpersStatic.SecondsToTime(Data.MajorUpdate.UpdateLength);

            ConstantSheet.Cells[3, 1].Value = "Time per Nation";
            ConstantSheet.Cells[3, 2].Formula = "B2/B1";
            
            ConstantSheet.Cells[4, 1].Value = "Minor Length";
            ConstantSheet.Cells[4, 2].Value = HelpersStatic.SecondsToTime(Data.MinorUpdate.UpdateLength);
            
            ConstantSheet.Cells[1, 5].Value = "ARCore 20XX";
            ConstantSheet.Cells[1, 6].Value = $"Ver.{ARCoordinator.Verison}";
            ConstantSheet.Cells[2, 5].Value = "Generation Date";
            ConstantSheet.Cells[2, 6].Value = DateTime.Now.ToString();

            ConstantSheet.Cells[5, 1].Value = "Time per Nation";
            ConstantSheet.Cells[5, 1].Formula = "B4/B1";
            // Create the target sheet - the part that everyone actually cares about.

            var TargetSheet = book.Worksheets.Add("Target Sheet");
            TargetSheet.Cells[1,1].Value = "Region Name";       //A
            // Hidden Values
            TargetSheet.Cells[1,2].Value = "Exec Delegate";     //B
            TargetSheet.Cells[1,3].Value = "Passworded";        //C
            TargetSheet.Cells[1,4].Value = "Founderless";       //D

            TargetSheet.Cells[1,5].Value = "Region Link";       //E
            TargetSheet.Cells[1,6].Value = "Population";        //F
            TargetSheet.Cells[1,7].Value = "Delegate Votes";    //G
            TargetSheet.Cells[1,8].Value = "Minor Time";        //H
            TargetSheet.Cells[1,9].Value = "Major Time";        //I
            TargetSheet.Cells[1,10].Value = "Major LastUpdate";  //J
            TargetSheet.Cells[1,11].Value = "Embassies";         //K
            TargetSheet.Cells[1,12].Value = "WFE";               //L

            return TargetSheet;
        }

        public void FillCells(ARCore.Types.Region Region, ExcelWorksheet TargetSheet)
        {    
            int CellIndex = (int)Region.Index+2;
            bool Exec = Region.DelegateAuth.Contains("X");
            bool Password =  Region.hasPassword;
            bool Founder = Region.hasFounder;

            Color FillColor = Color.Green;
            if(!Exec) FillColor = Color.Salmon;
            else if(Password) FillColor = Color.Yellow;
            else if(Founder) FillColor = Color.YellowGreen;

            TargetSheet.Cells[CellIndex, 1].Value = Region.name;
            TargetSheet.Cells[CellIndex, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            TargetSheet.Cells[CellIndex, 1].Style.Fill.BackgroundColor.SetColor(FillColor);

            TargetSheet.Cells[CellIndex, 2].Value = Exec;
            TargetSheet.Cells[CellIndex, 3].Value = Password;
            TargetSheet.Cells[CellIndex, 4].Value = Founder;

            TargetSheet.Cells[CellIndex, 5].Formula = $"HYPERLINK(\"https://nationstates.net/region={Region.name}\",\"{Region.Name}\")";
            TargetSheet.Cells[CellIndex, 6].Value = Region.NumNations;
            TargetSheet.Cells[CellIndex, 7].Value = Region.DelegateVotes;
            TargetSheet.Cells[CellIndex, 8].Value = HelpersStatic.SecondsToTime(Timer.BadEstimate(Region.Name, false));
            TargetSheet.Cells[CellIndex, 9].Value = HelpersStatic.SecondsToTime(Timer.BadEstimate(Region.Name, true));
            TargetSheet.Cells[CellIndex, 10].Value = HelpersStatic.SecondsToTime(Region.LastUpdate);
            if(Region.Embassies != null && Region.Embassies.Length > 0)
                TargetSheet.Cells[CellIndex, 11].Value = string.Join(',', Region.Embassies);
            TargetSheet.Cells[CellIndex, 12].Value = Region.Factbook;
        }

        public void CreateSpreadsheet()
        {
            Logger.Log(LogEventType.Information, "Initializing spreadsheet.");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // Initialize the spreadsheet
            using var package = new ExcelPackage();
            // Create the constants sheet
            var TargetSheet = SetupSheets(package.Workbook);

            Logger.Log(LogEventType.Information, "Filling out spreadsheet.");
            
            var Regions = Data.GetRegions();
            foreach(string RegionName in Regions)
            {
                var Region = Data.GetRegion(RegionName);
                FillCells(Region, TargetSheet);
            }
            Logger.Log(LogEventType.Verbose, "Cleaning up.");
            TargetSheet.Cells.AutoFitColumns(0);

            Logger.Log(LogEventType.Information, "Saving spreadsheet.");
            string filename = $"${DateTime.Now.ToString("yyyy-MM-dd")}-20XX Targetsheet.xlsx";
            try{File.Delete(filename);}catch(Exception){}
            using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None)){
                package.SaveAs(fs);
            }
        }

        public async Task CreateSpreadsheetAsync()
        {
            Logger.Log(LogEventType.Information, "Initializing spreadsheet.");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // Initialize the spreadsheet
            using var package = new ExcelPackage();
            // Create the constants sheet
            var TargetSheet = SetupSheets(package.Workbook);

            Logger.Log(LogEventType.Information, "Filling out spreadsheet.");
            // Calculating the update times requires multiple calls to the database
            // I went for a less memory intensive approach owing to that
            var Regions = Data.GetRegions();
            var TaskList = Regions.Select(async (Region) => {
               var tmp = await Data.GetRegionAsync(Region);
               FillCells(tmp, TargetSheet);
            });
            await Task.WhenAll(TaskList);

            Logger.Log(LogEventType.Verbose, "Cleaning up.");
            TargetSheet.Cells.AutoFitColumns(0);

            Logger.Log(LogEventType.Information, "Saving spreadsheet.");
            string filename = $"${DateTime.Now.ToString("yyyy-MM-dd")}-20XX Targetsheet.xlsx";
            try{File.Delete(filename);}catch(Exception){}
            using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None)){
                package.SaveAs(fs);
            }
        }
    }
}