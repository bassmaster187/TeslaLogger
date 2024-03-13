using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestGrafana
    {
        [TestMethod]
        public void CheckDataSource()
        {
            var files = System.IO.Directory.GetFiles("../../../TeslaLogger/Grafana", "*.json");
            foreach (var f in files)
            {
                var ff = new FileInfo(f);
                var name = ff.Name;
                var content = File.ReadAllText(ff.FullName);

                if (content.Contains(":8888"))
                    Assert.Fail("Contains :8888 - " + name);
                else if (content.Contains("192.168.1.195"))
                    Assert.Fail("Contains 192.168.1.195 - " + name);
                else if (content.Contains("192.168.1.195"))
                    Assert.Fail("Contains 192.168.1.195 - " + name);
                else if (content.IndexOf("teslarossa", StringComparison.OrdinalIgnoreCase) >= 0)
                    Assert.Fail("Contains teslarossa - " + name);
                else if (content.IndexOf("tessi", StringComparison.OrdinalIgnoreCase) >= 0)
                    Assert.Fail("Contains tessi - " + name);


            }
        }
    }
}
