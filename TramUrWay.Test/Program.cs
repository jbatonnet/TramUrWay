using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TramUrWay.Android;

namespace Android.Content
{
    public class AssetManager
    {
        public string[] List(string value)
        {
            return Directory.GetFiles(@"..\..\..\Data\Hiver 2015\").Select(p => Path.GetFileName(p)).ToArray();
        }
        public Stream Open(string value)
        {
            return File.OpenRead(@"..\..\..\Data\Hiver 2015\" + value);
        }
    }

    public class Context
    {
        public AssetManager Assets { get; } = new AssetManager();
    }
}

namespace TramUrWay.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Database.Initialize(new Context());
            Database.GetLiveTimeSteps();
        }
    }
}
