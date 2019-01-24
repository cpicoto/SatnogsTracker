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
using System.Windows.Forms;
using System.Diagnostics;

using SDRSharp.Common;

namespace SDRSharp.SatnogsTracker
{
    
    public partial class Controlpanel : UserControl
    {
        private bool enabled_ = false;
        private bool connected_ = false;
        public event Action SatPC32ServerStart;
        public event Action SatPC32ServerStop;
        public event Action ShowSettings;
        //public Settings dlg = new Settings();
        public Controlpanel()
        {
            InitializeComponent();
            this.checkBoxEnable.CheckedChanged += CheckBoxEnable_CheckedChanged;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.labelVersion.Text = "v"+fvi.FileMajorPart+"."+fvi.FileMinorPart;
        }

        public void SatPC32ServerReceivedFrequencyInHzChanged(String frequency_in_hz)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerReceivedFrequencyInHzChanged), new object[] { frequency_in_hz });
                return;
            }
            else
            {
                this.labelDownlink.Text = frequency_in_hz;
            }
        }

        public void SatPC32ServerNameChanged(String SatName)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerNameChanged), new object[] { SatName});
                return;
            }
            else
            {
                this.labelName.Text = SatName;
            }
        }

        public void SatPC32DDEAppChanged(String DDEApp)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32DDEAppChanged), new object[] { DDEApp });
                return;
            }
            else
            {
                this.labelDescSatPC32.Text = DDEApp;
            }
        }
        public void SatPC32ServerDownlinkFreqChanged(String DownlinkFreq)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerDownlinkFreqChanged), new object[] { DownlinkFreq });
                return;
            }
            else
            {
                this.labelDownlink.Text = DownlinkFreq;
            }
        }

        public void SatPC32ServerAzimuthChanged(String Azimuth)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerAzimuthChanged), new object[] { Azimuth });
                return;
            }
            else
            {
                this.labelAzimuth.Text = Azimuth;
            }
        }

        public void SatPC32ServerElevationChanged(String Elevation)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerElevationChanged), new object[] { Elevation });
                return;
            }
            else
            {
                this.labelElevation.Text = Elevation;
            }
        }

        public void SatPC32ServerModulationChanged(String Modulation)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerModulationChanged), new object[] { Modulation });
                return;
            }
            else
            {
                this.labelModulation.Text = Modulation;
            }
        }

        public void SatPC32ServerBandwidthChanged(String Bandwidth)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerBandwidthChanged), new object[] { Bandwidth });
                return;
            }
            else
            {
                this.labelBandwidth.Text = Bandwidth;
            }
        }

        public void SatPC32ServerRecordBaseChanged(Boolean RecordBase)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<Boolean>(SatPC32ServerRecordBaseChanged), new object[] { RecordBase });
                return;
            }
            else
            {
                this.checkBoxRecordBase.Checked = RecordBase;

            }
        }

        public void SatPC32ServerRecordAFChanged(Boolean RecordAF)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<Boolean>(SatPC32ServerRecordAFChanged), new object[] { RecordAF });
                return;
            }
            else
            {
                this.checkBoxRecordAF.Checked = RecordAF;
 

            }
        }
        public void SatPC32ServerSatNogsIDChanged(String SatNogsID)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<String>(SatPC32ServerSatNogsIDChanged), new object[] { SatNogsID });
                return;
            }
            else
            {
                this.labelSatNogsID.Text = SatNogsID;
            }
        }

        public void SatPC32Server_Enabled_Changed(bool enabled)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<bool>(SatPC32Server_Enabled_Changed), new object[] { enabled });
                return;
            }
            else
            {
                enabled_ = enabled;
                if (enabled) this.labelSatPC32Status.Text = "Enabled";
                else this.labelSatPC32Status.Text = "Disabled";
                //SetDescriptionOfServerState();
            }
        }

        public void SatPC32Server_Connected_Changed(bool connected)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<bool>(SatPC32Server_Connected_Changed), new object[] { connected });
                return;
            }
            else
            {
                connected_ = connected;
                if (connected) this.labelSatPC32Status.Text = "Connected";
                else this.labelSatPC32Status.Text = "Disconnected";
            }
        }
        public void ControlPanel_HomeSite_Changed(HamSite site)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<HamSite>(ControlPanel_HomeSite_Changed), new object[] { site});
                return;
            }
            else
            {
                this.labelGrid.Text=LatLonToGridSquare(double.Parse(site.Latitude), double.Parse(site.Longitude));
                this.labelDescSatPC32.Text = site.DDEApp;
                //this.labelGrid.Text = "Grid:"+site.Latitude+"/"+site.Longitude;
            }
        }
        public String LatLonToGridSquare(double lat, double lon)
        {
            double adjLat, adjLon;
            char GLat, GLon;
            String nLat, nLon;
            char gLat, gLon;
            double rLat, rLon;
            String U = "ABCDEFGHIJKLMNOPQRSTUVWX";
            String L = U.ToLower();

            if (double.IsNaN(lat)) throw new Exception("lat is NaN");
            if (double.IsNaN(lon)) throw new Exception("lon is NaN");
            if (Math.Abs(lat) == 90.0) throw new Exception("grid squares invalid at N/S poles");
            if (Math.Abs(lat) > 90) throw new Exception("invalid latitude: " + lat);
            if (Math.Abs(lon) > 180) throw new Exception("invalid longitude: " + lon);

            adjLat = lat + 90;
            adjLon = lon + 180;
            GLat = U[(int)(adjLat / 10)];
            GLon = U[(int)(adjLon / 20)];
            nLat = "" + (int)(adjLat % 10);
            nLon = "" + (int)((adjLon / 2) % 10);
            rLat = (adjLat - (int)(adjLat)) * 60;
            rLon = (adjLon - 2 * (int)(adjLon / 2)) * 60;
            gLat = L[(int)(rLat / 2.5)];
            gLon = L[(int)(rLon / 5)];
            String locator = "" + GLon + GLat + nLon + nLat + gLon + gLat;
            return locator;
        }
        private void CheckBoxEnable_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            if (checkbox != null)
            {
                if (checkbox.Checked)
                {
                    enabled_=true;
                    Console.WriteLine("Checkbox Changed");
                    SatPC32ServerStart?.Invoke();
                    
                } else
                {
                    enabled_ = false;
                    SatPC32ServerStop?.Invoke();     
                }
            }
        }

        private void Controlpanel_Load(object sender, EventArgs e)
        {
            //
        }

        private void btSettings_Click(object sender, EventArgs e)
        {
            ShowSettings?.Invoke();
        }

    }
}
