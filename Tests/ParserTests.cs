using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobocopyGui;

namespace RobocopyGui.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestExitCodeMapping()
        {
            Assert.AreEqual("No files copied (source and destination identical)", RobocopyRunner.MapExitCode(0));
            Assert.AreEqual("Success (Files copied successfully)", RobocopyRunner.MapExitCode(1));
            Assert.AreEqual("Error (Some files failed to copy; check log)", RobocopyRunner.MapExitCode(8));
            Assert.AreEqual("Fatal Error (No files were copied; serious network or path error)", RobocopyRunner.MapExitCode(16));
        }

        [TestMethod]
        public void TestPercentageParsing()
        {
            var runner = new RobocopyRunner();
            int reportedPercentage = -1;

            runner.ProgressChanged += (sender, args) =>
            {
                reportedPercentage = args.Percentage;
            };

            runner.ParseLine("               45%   ");
            Assert.AreEqual(45, reportedPercentage);

            runner.ParseLine(" 99.8% ");
            Assert.AreEqual(100, reportedPercentage);
        }

        [TestMethod]
        public void TestNewFileAndPercentageParsing()
        {
            var runner = new RobocopyRunner();
            int reportedPercentage = -1;
            string reportedFile = "";

            runner.ProgressChanged += (sender, args) =>
            {
                reportedPercentage = args.Percentage;
                reportedFile = args.CurrentFile;
            };

            // Test line where filename and percentage are combined
            runner.ParseLine("	    New File  		      15	file1.txt100%");

            Assert.AreEqual("file1.txt", reportedFile);
            Assert.AreEqual(100, reportedPercentage);
        }

        [TestMethod]
        public void TestNewFileOnlyParsing()
        {
            var runner = new RobocopyRunner();
            string reportedFile = "";

            runner.ProgressChanged += (sender, args) =>
            {
                reportedFile = args.CurrentFile;
            };

            runner.ParseLine("	    New File  		      1024	MyDocument.pdf");

            Assert.AreEqual("MyDocument.pdf", reportedFile);
        }

        [TestMethod]
        public void TestOverallProgressAndSummaryParsing()
        {
            var runner = new RobocopyRunner();
            int total = -1;
            int copied = -1;
            int percentage = -1;

            runner.OverallProgressChanged += (sender, args) =>
            {
                total = args.TotalFiles;
                copied = args.CopiedFiles;
                percentage = args.OverallPercentage;
            };

            // First file copied
            runner.ParseLine("	    New File  		      1024	file1.txt");
            Assert.AreEqual(0, total);
            Assert.AreEqual(1, copied);
            Assert.AreEqual(0, percentage);

            // Second file copied
            runner.ParseLine("	    New File  		      2048	file2.txt");
            Assert.AreEqual(0, total);
            Assert.AreEqual(2, copied);

            // Summary line arrives: 2 total files
            runner.ParseLine("   Files :         2         2         0         0         0         0");
            Assert.AreEqual(2, total);
            Assert.AreEqual(2, copied);
            Assert.AreEqual(100, percentage);
        }
    }
}
