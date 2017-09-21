using System;
using System.Diagnostics;
using System.IO;
using Moq;
using Xunit;

namespace ExpectNet.Test
{
    public class ProcessSpawnableTest
    {
        [Fact]
        public void CtorFilenameTest()
        {
            string filename = "testFileName";
            ProcessSpawnable proc = new ProcessSpawnable(filename);

            Assert.NotNull(proc.Process);
            Assert.Equal(filename, proc.Process.StartInfo.FileName);
            Assert.Equal("", proc.Process.StartInfo.Arguments);
        }

        [Fact]
        public void CtorFilenameArgumentsTest()
        {
            string filename = "testFileName";
            string args = "arg1 arg2";
            ProcessSpawnable proc = new ProcessSpawnable(filename, args);

            Assert.NotNull(proc.Process);
            Assert.Equal(filename, proc.Process.StartInfo.FileName);
            Assert.Equal(args, proc.Process.StartInfo.Arguments);
        }

        [Fact]
        public void CtorProcessTest()
        {
            string filename = "testFileName";
            string args = "arg1 arg2";
            Process p = new Process();
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = args;
            ProcessSpawnable proc = new ProcessSpawnable(p);

            Assert.NotNull(proc.Process);
            Assert.Equal(p.StartInfo, proc.Process.StartInfo);
        }

        [Fact]
        public void NullFilenameTest()
        {
            var proc = new Mock<IProcess>();
            ProcessStartInfo psi = new ProcessStartInfo(null);
            proc.Setup(p => p.StartInfo).Returns(psi);
            Exception caughtException = null;

            try
            {
                ProcessSpawnable process = new ProcessSpawnable(proc.Object);
                process.Init();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<ArgumentException>(caughtException);
            Assert.Equal("_process.StartInfo.FileName", (caughtException as ArgumentException).ParamName);

        }

        [Fact]
        public void EmptyFilenameTest()
        {
            var proc = new Mock<IProcess>();
            ProcessStartInfo psi = new ProcessStartInfo(null);
            proc.Setup(p => p.StartInfo).Returns(psi);
            Exception caughtException = null;

            try
            {
                ProcessSpawnable process = new ProcessSpawnable(proc.Object);
                process.Init();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<ArgumentException>(caughtException);
            Assert.Equal("_process.StartInfo.FileName", (caughtException as ArgumentException).ParamName);

        }

        [Fact]
        public void InitProcessTest()
        {
            var proc = new Mock<IProcess>();
            ProcessStartInfo psi = new ProcessStartInfo(null);
            psi.FileName = "filename";
            psi.RedirectStandardError = false;
            psi.RedirectStandardInput = false;
            psi.RedirectStandardOutput = false;
            psi.UseShellExecute = true;
            proc.Setup(p => p.StartInfo).Returns(psi);

            ProcessSpawnable ps = null;
            Exception caughtException = null;
            try
            {
                ps = new ProcessSpawnable(proc.Object);
                ps.Init();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            Assert.Null(caughtException);
            Assert.True(ps.Process.StartInfo.RedirectStandardError);
            Assert.True(ps.Process.StartInfo.RedirectStandardInput);
            Assert.True(ps.Process.StartInfo.RedirectStandardOutput);
            Assert.False(ps.Process.StartInfo.UseShellExecute);
            proc.Verify(p => p.Start());
        }

        [Fact]
        public void WriteTest()
        {
            //Arrange
            string testText = "This is text to write";
            Stream so = new MemoryStream();
            so.WriteByte(1);
            so.Seek(0, SeekOrigin.Begin);
            StreamReader output = new StreamReader(so);
            Assert.False(output.EndOfStream);

            Stream se = new MemoryStream();
            se.WriteByte(2);
            se.Seek(0, SeekOrigin.Begin);
            StreamReader error = new StreamReader(se);
            Assert.False(error.EndOfStream);

            Stream si = new MemoryStream();
            StreamWriter input = new StreamWriter(si);

            var proc = new Mock<IProcess>();
            ProcessStartInfo psi = new ProcessStartInfo("filename");
            proc.SetupGet<ProcessStartInfo>(p => p.StartInfo).Returns(psi);
            proc.SetupGet<StreamReader>(p => p.StandardError).Returns(error);
            proc.SetupGet<StreamReader>(p => p.StandardOutput).Returns(output);
            proc.SetupGet<StreamWriter>(p => p.StandardInput).Returns(input);
            ProcessSpawnable ps = new ProcessSpawnable(proc.Object);
            ps.Init();

            //Act
            ps.Write(testText);

            //Assert
            ps.Process.StandardInput.Flush();
            int maxSize = 4096;
            byte[] tmp = new byte[maxSize];
            si.Seek(0, SeekOrigin.Begin);
            int n = si.Read(tmp, 0, maxSize);
            string writtenText = System.Text.Encoding.UTF8.GetString(tmp, 0, n);

            Assert.True(output.EndOfStream);
            Assert.True(error.EndOfStream);
            Assert.Equal(testText, writtenText);
        }
    }
}
