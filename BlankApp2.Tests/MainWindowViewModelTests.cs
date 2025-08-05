using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeyBoardApp;
using KeyBoardApp.ViewModels;

namespace BlankApp2.Tests
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        [TestMethod]
        public void CmdCaptureStart_ClearsHistoryAndEnablesCapture()
        {
            var vm = new MainWindowViewModel();
            vm.KeyHist.Add((new KeyDto(), TimeSpan.Zero));
            vm.MouseHist.Add((new MouseDto(), TimeSpan.Zero));

            vm.CmdCaptureStart.Execute();

            Assert.IsTrue(vm.IsCapture);
            Assert.AreEqual(0, vm.KeyHist.Count);
            Assert.AreEqual(0, vm.MouseHist.Count);
        }

        [TestMethod]
        public void CmdCaptureStop_DisablesCapture()
        {
            var vm = new MainWindowViewModel();
            vm.CmdCaptureStart.Execute();

            vm.CmdCaptureStop.Execute();

            Assert.IsFalse(vm.IsCapture);
        }
    }
}
