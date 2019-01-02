using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace SDRSharp.SatnogsTracker
{
    public partial class Controlpanel : UserControl
    {
        

        public Controlpanel()
        {
            InitializeComponent();
            this.checkBoxEnable.CheckedChanged += CheckBoxEnable_CheckedChanged;
            TcpServer_Connected_Changed(false); // init state is disconnected.
            //SatPC32Server_Connected_Changed(true);
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.labelVersion.Text = "v"+fvi.FileMajorPart+"."+fvi.FileMinorPart;
            //satpc32_ = new SatPC32DDE();
        }

        public  void ReceivedFrequencyInHzChanged(long frequency_in_hz)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<long>(ReceivedFrequencyInHzChanged), new object[] { frequency_in_hz });
                return;
            }
            else
            {
                this.labelFrequency.Text = frequency_in_hz.ToString();
            }
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
        public void TcpServer_Enabled_Changed(bool enabled)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<bool>(TcpServer_Enabled_Changed), new object[] { enabled });
                return;
            }
            else
            {
                enabled_ = enabled;
                SetDescriptionOfServerState();
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

        public void TcpServer_Connected_Changed(bool connected)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<bool>(TcpServer_Connected_Changed), new object[] { connected });
                return;
            }
            else
            {
                connected_ = connected;
                SetDescriptionOfServerState();
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

        public void SetDescriptionOfServerState()
        {
            if (enabled_)
            {
                if (connected_)
                {
                    this.labelStatus.Text = "connected";
                    labelStatus.ForeColor = Color.Green;
                }
                else
                {
                    this.labelStatus.Text = "listening on port 4532";
                    labelStatus.ForeColor = Color.Blue;
                }
            }
            else
            {
                this.labelStatus.Text = "disabled";
                labelStatus.ForeColor = Color.DarkRed;
            }
        }

        private void CheckBoxEnable_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            if (checkbox != null)
            {
                if (checkbox.Checked)
                {
                    Console.WriteLine("Checkbox Changed");
                    //ServerStart?.Invoke();
                    SatPC32ServerStart?.Invoke();
                    
                } else
                {
                    //ServerStop?.Invoke();
                    SatPC32ServerStop?.Invoke();
                    
                }
            }
        }

        private bool enabled_ = false;
        private bool connected_ = false;
        public event Action SatPC32ServerStart;
        public event Action SatPC32ServerStop;

        private void Controlpanel_Load(object sender, EventArgs e)
        {

        }
    }
}
