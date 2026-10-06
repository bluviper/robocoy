using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobocopyGui;

namespace RobocopyGui.Tests
{
    [TestClass]
    public class IconAndSquircleTests
    {
        [TestMethod]
        public void TestSquirclePathGeneration()
        {
            var bounds = new RectangleF(0, 0, 100, 50);
            using var path = MainForm.CreateSquirclePath(bounds, 10f);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.PointCount > 0);
            RectangleF pathBounds = path.GetBounds();
            Assert.IsTrue(pathBounds.Width <= 100.5f);
            Assert.IsTrue(pathBounds.Height <= 50.5f);
        }

        [TestMethod]
        public void TestRobotBitmapAndIcoGeneration()
        {
            using var bmp32 = MainForm.CreateRobotBitmap(32);
            Assert.IsNotNull(bmp32);
            Assert.AreEqual(32, bmp32.Width);
            Assert.AreEqual(32, bmp32.Height);

            // Locate project root and output app.ico
            string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            string icoPath = Path.Combine(projectRoot, "app.ico");

            MainForm.SaveIcoFile(icoPath, new[] { 16, 32, 48, 64, 128, 256 });

            Assert.IsTrue(File.Exists(icoPath), "app.ico should be generated at project root");
            var fileInfo = new FileInfo(icoPath);
            Assert.IsTrue(fileInfo.Length > 500, "app.ico should contain multi-resolution icon data");
        }
    }
}
