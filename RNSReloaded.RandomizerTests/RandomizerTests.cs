using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RNSReloaded.Interfaces;
using RNSReloaded.Randomizer;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer.Tests
{
    [TestClass()]
    public class RandomizerTests {
        [TestMethod()]
        public void LoadLanguageMapTest() {
            //arrange
            Randomizer randomizer = new Randomizer();
            var mockRns = new Mock<IRNSReloaded>();
            mockRns.Setup(rns => rns.GetString())
            var rns = mockRns.Object;

            //act
            randomizer.LoadLanguageMap(rns);

            //assert
            Dictionary<string, string> expected = new Dictionary<string, string> {
                ["oneKey"] = "oneVal",
                ["twoKey"] = "twoVal",
            };
            Assert.AreEqual(expected.ToFrozenDictionary(), randomizer.LanguageMap);
        }
    }
}
