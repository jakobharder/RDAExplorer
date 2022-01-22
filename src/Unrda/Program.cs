using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using RDAExplorer;

namespace Unrda
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var fileName = @"C:\Program Files (x86)\Ubisoft\Related Designs\ANNO 2070 DEMO\maindata\Data0.rda";
            RDAReader reader = new RDAReader();
            reader.FileName = fileName;
            reader.ReadRDAFile();
            RDAExplorer.RDAFileExtension.ExtractAll(reader.rdaFolder.GetAllFiles(), @"C:\Users\User\Desktop\dst");
        }
    }
}