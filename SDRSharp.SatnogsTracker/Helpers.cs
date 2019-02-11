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
using SDRSharp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Zeptomoby.OrbitTools;

namespace SDRSharp.SatnogsTracker
{
    public partial class SatnogsTrackerPlugin : ISharpPlugin
    {

        private void UpdateTleData(Boolean alwaysdownload)
        {
            StartLogFile();
            //Boolean alwaysdownload = false;
            LoadAllKeps(alwaysdownload);
            LoadSatellitesAndTransmitters(alwaysdownload);
            ListSatellitesfromCollection(); //List everything to Logfile
            UpdateStatus?.Invoke("TLE Updated");
            LogFile.Flush();
            StopLogFile();
        }

        private void ShowSettingsForm()
        {
            if (dlg.Created) dlg.Show();
            else
            {
                dlg = new Settings();
                dlg.HamSiteChanged += StationSiteChanged;
                dlg.HamSiteChanged += _controlpanel.ControlPanel_HomeSite_Changed;
                dlg.UpdateTle += UpdateTleData;
                UpdateStatus += dlg.UpdateStatus;
                dlg.UpdateIP += _UDPaudioStreamer.UpdateIP;
                dlg.Show();
            }
        }

        private void StationSiteChanged(HamSite obj)
        {
            siteSettings = obj;
            siteHome = new Site(double.Parse(siteSettings.Latitude),
                    double.Parse(siteSettings.Longitude),
                    double.Parse(siteSettings.Altitude),
                    siteSettings.Callsign);
            if (satpc32Server_ != null)
            {
                if (satpc32Server_.DDEServerApp != siteSettings.DDEApp)
                {
                    satpc32Server_ = new SatPC32DDE(siteSettings.DDEApp);
                }
            }
        }

        private String RecordingLocation()
        {
            String Filefolder = Path.GetDirectoryName(Application.ExecutablePath);
            Filefolder += "\\SatRecordings";
            if (!Directory.Exists(Filefolder))
                Directory.CreateDirectory(Filefolder);
            return Filefolder;
        }

        private String DataLocation()
        {
            String Filefolder = Path.GetDirectoryName(Application.CommonAppDataPath);
            Filefolder += "\\SatnogsTracker";
            if (!Directory.Exists(Filefolder))
                Directory.CreateDirectory(Filefolder);
            return Filefolder + "\\";
        }

        private String ProgramLocation()
        {
            String Filefolder = Path.GetDirectoryName(Application.ExecutablePath);
            return Filefolder + "\\";
        }

        public Boolean StartLogFile()
        {
            String LogFileName = DataLocation() + "SatnogsTracker_logfile.txt";
            if (File.Exists(LogFileName))
                File.Delete(LogFileName);
            try
            {
                LogFile = new StreamWriter(LogFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't open file:{0} error:{1}", LogFileName, e.Message);
                return false;
            }
            return true;
        }

        public Boolean StopLogFile()
        {
            try
            {
                LogFile.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Close Logfile: {0}", e.Message);
                return false;
            }
            return true;
        }
    }
}
