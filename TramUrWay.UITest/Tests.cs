using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace TramUrWay.UITest
{
    [TestFixture(Platform.Android)]
    public class Tests
    {
        IApp app;
        Platform platform;

        public Tests(Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            app = AppInitializer.StartApp(platform);
        }

        [Test]
        public void AppLaunches()
        {
            app.Screenshot("First screen");
        }

        [Test]
        public void TramwayScreen()
        {
            app.Tap("Tramway ligne 1");
            app.WaitFor(TimeSpan.FromSeconds(5));
            app.Screenshot("Tram screen");
        }
    }

    public static class AppExtensions
    {
        public static void WaitFor(this IApp me, TimeSpan timeSpan)
        {
            DateTime now = DateTime.Now;
            me.WaitFor(() => DateTime.Now - now > timeSpan);
        }
    }
}