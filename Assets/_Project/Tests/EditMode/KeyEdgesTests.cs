using NUnit.Framework;
using VampireSpawn.Game.Screens;

namespace VampireSpawn.Tests
{
    public class KeyEdgesTests
    {
        [Test]
        public void AKeyHeldSinceBeforeTheFirstReadIsNotAPress()
        {
            var keys = new KeyEdges();
            Assert.IsFalse(keys.Pressed("enter", true), "held over from the screen before");
            Assert.IsFalse(keys.Pressed("enter", true));
            Assert.IsFalse(keys.Pressed("enter", false));
            Assert.IsTrue(keys.Pressed("enter", true), "let go and pressed again");
        }

        [Test]
        public void APressAfterAnIdleReadCounts()
        {
            var keys = new KeyEdges();
            Assert.IsFalse(keys.Pressed("z", false));
            Assert.IsTrue(keys.Pressed("z", true));
            Assert.IsFalse(keys.Pressed("z", true), "still held");
        }
    }
}
