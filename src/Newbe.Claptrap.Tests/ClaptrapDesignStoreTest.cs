using Autofac.Extras.Moq;
using FluentAssertions;
using Newbe.Claptrap.Preview;
using Newbe.Claptrap.Preview.Abstractions.Exceptions;
using Newbe.Claptrap.Preview.Abstractions.Metadata;
using Newbe.Claptrap.Preview.Impl;
using Xunit;
using Xunit.Abstractions;

namespace Newbe.Claptrap.Tests
{
    public class ClaptrapDesignStoreTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ClaptrapDesignStoreTest(
            ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }


        [Theory]
        [InlineData("123", "testCode")]
        [InlineData(null, "testCode")]
        public void Add(string id, string typeCode)
        {
            using var mocker = AutoMock.GetStrict(builder => { builder.AddLogging(_testOutputHelper); });
            mocker.VerifyAll = true;
            var claptrapDesignStore = mocker.Create<ClaptrapDesignStore>();
            var actorIdentity = new ClaptrapIdentity(id, typeCode);
            var design = new ClaptrapDesign
            {
                Identity = actorIdentity
            };
            claptrapDesignStore.AddOrReplace(design);
            var claptrapDesign = claptrapDesignStore.FindDesign(actorIdentity);
            claptrapDesign.Should().Be(design);
        }

        [Theory]
        [InlineData("123", "testCode")]
        [InlineData(null, "testCode")]
        public void Replace(string id, string typeCode)
        {
            using var mocker = AutoMock.GetStrict(builder => { builder.AddLogging(_testOutputHelper); });
            mocker.VerifyAll = true;
            var claptrapDesignStore = mocker.Create<ClaptrapDesignStore>();
            var actorIdentity = new ClaptrapIdentity(id, typeCode);
            var design = new ClaptrapDesign
            {
                Identity = actorIdentity
            };
            claptrapDesignStore.AddOrReplace(design);
            var newDesign = new ClaptrapDesign
            {
                Identity = actorIdentity,
            };
            claptrapDesignStore.AddOrReplace(newDesign);
            var claptrapDesign = claptrapDesignStore.FindDesign(actorIdentity);
            claptrapDesign.Should().Be(newDesign);
        }

        [Fact]
        public void NotFound()
        {
            using var mocker = AutoMock.GetStrict(builder => { builder.AddLogging(_testOutputHelper); });
            mocker.VerifyAll = true;
            var claptrapDesignStore = mocker.Create<ClaptrapDesignStore>();
            var actorIdentity = new ClaptrapIdentity("123", "testCode");
            Assert.Throws<ClaptrapDesignNotFoundException>(() =>
                claptrapDesignStore.FindDesign(actorIdentity));
        }

        [Fact]
        public void IdSpecificFirstFound()
        {
            using var mocker = AutoMock.GetStrict(builder => { builder.AddLogging(_testOutputHelper); });
            mocker.VerifyAll = true;
            var claptrapDesignStore = mocker.Create<ClaptrapDesignStore>();
            const string typeCode = "testCode";
            var actorIdentity = new ClaptrapIdentity("123", typeCode);
            var idDesign = new ClaptrapDesign
            {
                Identity = actorIdentity
            };
            claptrapDesignStore.AddOrReplace(idDesign);
            var globalDesign = new ClaptrapDesign
            {
                Identity = new ClaptrapIdentity(string.Empty, typeCode),
            };
            claptrapDesignStore.AddOrReplace(globalDesign);
            var claptrapDesign = claptrapDesignStore.FindDesign(actorIdentity);
            claptrapDesign.Should().Be(idDesign);
        }

        [Fact]
        public void GlobalDesignFound()
        {
            using var mocker = AutoMock.GetStrict(builder => { builder.AddLogging(_testOutputHelper); });
            mocker.VerifyAll = true;
            var claptrapDesignStore = mocker.Create<ClaptrapDesignStore>();
            const string typeCode = "testCode";
            var actorIdentity = new ClaptrapIdentity("123", typeCode);
            var idDesign = new ClaptrapDesign
            {
                Identity = actorIdentity
            };
            claptrapDesignStore.AddOrReplace(idDesign);
            var globalDesign = new ClaptrapDesign
            {
                Identity = new ClaptrapIdentity(string.Empty, typeCode),
            };
            claptrapDesignStore.AddOrReplace(globalDesign);
            var claptrapDesign = claptrapDesignStore.FindDesign(new ClaptrapIdentity("456", typeCode));
            claptrapDesign.Should().Be(globalDesign);
        }
    }
}