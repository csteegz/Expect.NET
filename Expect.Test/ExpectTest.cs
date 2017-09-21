using System;
using ExpectNet;
using Moq;
using Xunit;

namespace ExpectNet.Test
{
   
    public class ExpectTest
    {
        [Fact]
        public void SpawnInitSpawnableTest()
        {
            var spawnable = new Mock<ISpawnable>();

            Expect.Spawn(spawnable.Object);

            spawnable.Verify(s => s.Init());
        }

        [Fact]
        public void SpawnCreateSessionTest()
        {
            var spawnable = new Mock<ISpawnable>();

            var session = Expect.Spawn(spawnable.Object);

            Assert.IsType<Session>( session);
            Assert.NotNull(session);
        }
    }
}
