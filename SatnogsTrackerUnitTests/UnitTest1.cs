/* 
    Copyright(c) Carlos Picoto (AD7NP), Inc. All rights reserved. 

    The MIT License(MIT) 

    Permission is hereby granted, free of charge, to any person obtaining a copy 
    of this software and associated documentation files(the "Software"), to deal 
    in the Software without restriction, including without limitation the rights 
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell 
    copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions : 
    The above copyright notice and this permission notice shall be included in 
    all copies or substantial portions of the Software. 

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE 
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
    THE SOFTWARE. 
*/
using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDRSharp.SatnogsTracker;

namespace SatnogsTrackerUnitTests
{
    [TestClass]
    public class LoadSatnogsFiles
    {
        [TestMethod]
        public void LoadKep_amateur_txt()
        {
            SatnogsTrackerPlugin MyControl = new SatnogsTrackerPlugin();
            Assert.AreNotEqual(MyControl, null);
            MyControl.StartLogFile();
            Boolean Result = false;            
            //http://www.celestrak.com/NORAD/elements/amateur.txt
            Result=MyControl.GetFile("http://www.dk3wn.info/tle/amateur.txt", "amateur.txt", false);
            Assert.AreEqual(Result, true);
            Result=MyControl.LoadTxtKeps("amateur.txt");
            Assert.AreEqual(Result, true);
            MyControl.StopLogFile();
        }
        [TestMethod]
        public void LoadKep_cubesat_txt()
        {
            Boolean Result;
            SatnogsTrackerPlugin MyControl = new SatnogsTrackerPlugin();
            Assert.AreNotEqual(MyControl, null);
            MyControl.StartLogFile();
            MyControl.GetFile("http://www.celestrak.com/NORAD/elements/cubesat.txt", "cubesat.txt", false);
            Result= MyControl.LoadTxtKeps("cubesat.txt");
            Assert.AreEqual(Result, true);
            MyControl.StopLogFile();

        }
        [TestMethod]
        public void LoadKep_tle_new_txt()
        {
            Boolean Result;
            SatnogsTrackerPlugin MyControl = new SatnogsTrackerPlugin();
            Assert.AreNotEqual(MyControl, null);
            Result = MyControl.StartLogFile();
            Assert.AreEqual(Result, true);
            Result = MyControl.GetFile("http://www.celestrak.com/NORAD/elements/tle-new.txt", "tle-new.txt", false);
            Assert.AreEqual(Result, true);
            Result = MyControl.LoadTxtKeps("tle-new.txt");
            Assert.AreEqual(Result, true);
            Result = MyControl.StopLogFile();
            Assert.AreEqual(Result, true);
        }
        [TestMethod]
        public void LoadSatellites()
        {
            Boolean Result;
            SatnogsTrackerPlugin MyControl = new SatnogsTrackerPlugin();
            Assert.AreNotEqual(MyControl, null);
            Result=MyControl.StartLogFile();
            Assert.AreEqual(Result, true);
            Result = MyControl.GetFile("https://db.satnogs.org/api/satellites/", "Satellites.json", false);
            Assert.AreEqual(Result, true);
            Result = MyControl.LoadSatsfromJson("Satellites.json");
            Assert.AreEqual(Result, true);
            Result=MyControl.StopLogFile();
            Assert.AreEqual(Result, true);
        }


        [TestMethod]

        public void StartTracking()
        {
            Boolean Result;
            SatnogsTrackerPlugin MyControl = new SatnogsTrackerPlugin();
            Assert.AreNotEqual(MyControl,null);
            Result=MyControl.StartLogFile();
            Assert.AreEqual(Result, true);
            LoadKeps_Only(MyControl);
            LoadSatellites_Only(MyControl);
            Result= MyControl.LoadHomeSiteFromJson();
            Assert.AreEqual(Result, true);
            Console.WriteLine("Tracking Initiated");
            int iterations = 1;
            MyControl.CalculateSatVisibility(iterations);
            Result = MyControl.StopLogFile();
            Assert.AreEqual(Result, true);

        }
        public void LoadSatellites_Only(SatnogsTrackerPlugin MyControl)
        {
            Boolean Result;
            Assert.AreNotEqual(MyControl, null);
            Result = MyControl.GetFile("https://db.satnogs.org/api/satellites/", "Satellites.json", false);
            Assert.AreEqual(Result, true);
            Result = MyControl.LoadSatsfromJson("Satellites.json");
            Assert.AreEqual(Result, true);
        }
        public void LoadKeps_Only(SatnogsTrackerPlugin MyControl)
        {
            Boolean Result;
            Assert.AreNotEqual(MyControl, null);
            Result = MyControl.GetFile("http://www.dk3wn.info/tle/amateur.txt", "amateur.txt", false);
            Assert.AreEqual(Result, true);
            Result = MyControl.LoadTxtKeps("amateur.txt");
            Assert.AreEqual(Result, true);
        }

    }
    [TestClass]
    public class ActiveTracking
    {
        [TestMethod]
        public void Validate()
        {
            Assert.AreEqual(true, true);
        }
    }
}
