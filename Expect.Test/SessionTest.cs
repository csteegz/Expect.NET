using System;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Threading;

namespace ExpectNet.Test
{
    public class SessionTest
    {
        private async Task<string> ReturnStringAfterDelayAsync(string s, int delayInMs)
        {
            await Task.Delay(delayInMs);
            return s;
        }

        private string ReturnStringAfterDelay(string s, int delayInMs)
        {
            Thread.Sleep(delayInMs);
            return s;
        }

        [Fact]
        public void SendTest()
        {
            var spawnable = new Mock<ISpawnable>();
            Session session = new Session(spawnable.Object);
            string command = "test command";

            session.Send(command);

            spawnable.Verify(p => p.Write(command));
        }

        [Fact]
        public void BasicExpectTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.Read()).Callback(() => Thread.Sleep(1000)).Returns("test expected string test");
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            session.Expect("expected string", () => funcCalled = true);

            Assert.True(funcCalled);
        }

        [Fact]
        public void BasicExpectWithOutputTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.Read()).Returns(ReturnStringAfterDelay("test expected string test", 10));
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            string output = "";
            session.Expect("expected string", (s) => { funcCalled = true; output = s; });

            Assert.True(funcCalled);
            Assert.Equal("test expected string test", output);
        }

        [Fact]
        public void SplitResultExpectTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            string[] strings = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150)};
            spawnable.Setup(p => p.Read()).Returns(() => strings[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            session.Expect("expected string", () => funcCalled = true);

            Assert.True(funcCalled);
            Assert.Equal(2, i);
        }

        [Fact]
        public void SplitResultExpectWitOutputTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            string[] strings = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150)};
            spawnable.Setup(p => p.Read()).Returns(() => strings[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;
            string output = "";

            session.Expect("expected string", (s) => { funcCalled = true; output = s; });

            Assert.True(funcCalled);
            Assert.Equal(2, i);
            Assert.Equal("test expected string test", output);
        }

        [Fact]
        public void SendResetOutputTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            string[] strings = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150),
                                   ReturnStringAfterDelay("next expected string", 100)};
            spawnable.Setup(p => p.Read()).Returns(() => strings[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            string output = "";

            session.Expect("expected string", (s) => { session.Send("test"); });
            session.Expect("next expected", (s) => { output = s; });
            Assert.Equal("next expected string", output);
        }

        [Fact]
        public void TimeoutThrownExpectTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.Read()).Returns(() => ReturnStringAfterDelay("test expected string test", 1000));
            Session session = new Session(spawnable.Object);
            session.Timeout = 500;
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                session.Expect("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.IsType<TimeoutException>(exc);
            Assert.False(funcCalled);
        }

        [Fact]
        public void TimeoutNotThrownExpectTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.Read()).Returns(ReturnStringAfterDelay("test expected string test", 1200));
            Session session = new Session(spawnable.Object);
            session.Timeout = 2400;
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                session.Expect("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.Null(exc);
            Assert.True(funcCalled);
        }

        [Fact]
        public async Task TimeoutThrownExpectAsyncTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.ReadAsync()).Returns(ReturnStringAfterDelayAsync("test expected string test", 1200));
            Session session = new Session(spawnable.Object);
            session.Timeout = 500;
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                await session.ExpectAsync("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.NotNull(exc);
            Assert.IsType<TimeoutException>(exc);
            Assert.False(funcCalled);
        }

        [Fact]
        public async Task TimeoutNotThrownExpectAsyncTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.ReadAsync()).Returns(ReturnStringAfterDelayAsync("test expected string test", 1200));
            Session session = new Session(spawnable.Object);
            session.Timeout = 2400;
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                await session.ExpectAsync("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.Null(exc);
            Assert.True(funcCalled);
        }

        [Fact]
        public void SetGetTimeout2400Test()
        {
            var spawnable = new Mock<ISpawnable>();
            Session session = new Session(spawnable.Object);
            session.Timeout = 2400;
            Assert.Equal(2400, session.Timeout);
        }

        [Fact]
        public void SetGetTimeout200Test()
        {
            var spawnable = new Mock<ISpawnable>();
            Session session = new Session(spawnable.Object);
            session.Timeout  = 200;
            Assert.Equal(200, session.Timeout);
        }

        [Fact]
        public void SetGetTimeoutIncorrectValueTest()
        {
            var spawnable = new Mock<ISpawnable>();
            Session session = new Session(spawnable.Object);
            Exception exc = null;
            ArgumentException aoorexc = null;
            try
            {
                session.Timeout = -1;
            }
            catch (ArgumentException aoore)
            {
                aoorexc = aoore;
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.Null(exc);
            Assert.NotNull(aoorexc);
        }

        [Fact]
        public void BasicAsyncExpectTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.ReadAsync()).Returns(ReturnStringAfterDelayAsync("test expected string test", 10));
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            Task task = session.ExpectAsync("expected string", () => funcCalled = true);
            task.Wait();

            Assert.True(funcCalled);
        }

        [Fact]
        public void BasicExpectAsyncWithOutputTest()
        {
            var spawnable = new Mock<ISpawnable>();
            spawnable.Setup(p => p.ReadAsync()).Returns(ReturnStringAfterDelayAsync("test expected string test", 10));
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            string output = "";
            session.ExpectAsync("expected string", (s) => { funcCalled = true; output = s; }).Wait();

            Assert.True(funcCalled);
            Assert.Equal("test expected string test", output);
        }

        [Fact]
        public void SplitResultExpectAsyncTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelayAsync("test expected ", 100), 
                                     ReturnStringAfterDelayAsync("string test", 150)};
            spawnable.Setup(p => p.ReadAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;

            session.ExpectAsync("expected string", () => funcCalled = true).Wait();

            Assert.True(funcCalled);
            Assert.Equal(2, i);
        }

        [Fact]
        public void SplitResultExpectAsyncWitOutputTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelayAsync("test expected ", 100), 
                                     ReturnStringAfterDelayAsync("string test", 150)};
            spawnable.Setup(p => p.ReadAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            bool funcCalled = false;
            string output = "";

            session.ExpectAsync("expected string", (s) => { funcCalled = true; output = s; }).Wait();

            Assert.True(funcCalled);
            Assert.Equal(2, i);
            Assert.Equal("test expected string test", output);
        }

        [Fact]
        public void SendResetOutputAsyncTest()
        {
            var spawnable = new Mock<ISpawnable>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelayAsync("test expected ", 100), 
                                     ReturnStringAfterDelayAsync("string test", 150),
                                   ReturnStringAfterDelayAsync("next expected string", 100)};
            spawnable.Setup(p => p.ReadAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Session session = new Session(spawnable.Object);
            string output = "";

            session.ExpectAsync("expected string", (s) => { session.Send("test"); }).Wait();
            session.ExpectAsync("next expected", (s) => { output = s; }).Wait();
            Assert.Equal("next expected string", output);
        }
    }
    
}
