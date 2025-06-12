using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malshinon
{
    internal class Program
    {
        // DAL instances are accessible to the entire class
        private static readonly PersonDAL _personDal = new PersonDAL();
        private static readonly IntelReportsDAL _intelReportsDal = new IntelReportsDAL();
        private static readonly AlertsDAL _alertsDal = new AlertsDAL();
        private static CsvImporter _csvImporter;

        /// <summary>
        /// Main entry point. Initializes handlers and shows the main menu.
        /// </summary>
        static void Main(string[] args)
        {
            // Initialize ReportHandler with DALs
            ReportHandler.Initialize(_personDal, _intelReportsDal, _alertsDal);

            // Create CSV importer object
            _csvImporter = new CsvImporter(_personDal, _intelReportsDal);

            // Use Menu class for main menu
            Menu.ShowMenu(_csvImporter, _intelReportsDal, _alertsDal);
        }

    }
}