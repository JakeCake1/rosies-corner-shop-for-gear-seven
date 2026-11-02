using Game.Features.Home;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Home
{
    public sealed class SignLinesTests
    {
        [TestCase("Rosie's Corner Shop", "Rosie's\nCorner Shop")]
        [TestCase("Bob's Shop", "Bob's\nShop")]
        [TestCase("The Little Green Grocer", "The Little\nGreen Grocer")]
        [TestCase("Rosie", "Rosie")]
        [TestCase("  Corner  Shop ", "Corner\nShop")]
        public void Split_BreaksAtTheMostBalancedSpace(string name, string expected)
        {
            Assert.AreEqual(expected, SignLines.Split(name));
        }

        [TestCase("Rosie's\nCorner Shop", 11)]
        [TestCase("Rosie", 5)]
        public void LongestLine_CountsTheLongerLine(string lines, int expected)
        {
            Assert.AreEqual(expected, SignLines.LongestLine(lines));
        }
    }
}
