
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDRSharp.Common;
using NDde;
using System.Threading;
using SDRSharp.Radio;

namespace SDRSharp.SatnogsTracker
{
    public class SatnogsTrackerPlugin : ISharpPlugin
    {
        private const string _displayName = "Satnogs Tracker";
        private Controlpanel _controlpanel;
        private ISharpControl control_;
        private SatPC32DDE satpc32Server_;




        public UserControl GuiControl
        {
            get { return _controlpanel; }
        }
        public UserControl Gui
        {
            get
            {
                return _controlpanel;
            }
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public void Close()
        {
            satpc32Server_?.Stop();
        }

        public bool HasGui
        {
            get { return true; }
        }


        public void Initialize(ISharpControl control)
        {
            Console.WriteLine("Initialize Plugin\r\n");
            control_ = control;

            //Instanciate all needed objects
            _controlpanel = new Controlpanel();          

            //Link the objects together
            SatPC32DDE satpc32Server = new SatPC32DDE();
            satpc32Server_ = satpc32Server;

            _controlpanel.SatPC32ServerStart += satpc32Server.Start;
            _controlpanel.SatPC32ServerStop += satpc32Server.Stop;

            satpc32Server.Connected += _controlpanel.SatPC32Server_Connected_Changed;
            satpc32Server.Enabled += _controlpanel.SatPC32Server_Enabled_Changed;

            //satpc32Server.FrequencyInHzChanged += _controlpanel.SatPC32ServerReceivedFrequencyInHzChanged;
            satpc32Server.SatNameChanged += _controlpanel.SatPC32ServerNameChanged;

            satpc32Server.SatDownlinkFreqChanged += _controlpanel.SatPC32ServerDownlinkFreqChanged;
            satpc32Server.SatDownlinkFreqChanged += SDRSharp_DownlinkFreqChanged;

            //Display Only
            satpc32Server.SatAzimuthChanged += _controlpanel.SatPC32ServerAzimuthChanged;
            satpc32Server.SatElevationChanged += _controlpanel.SatPC32ServerElevationChanged;

            satpc32Server.SatModulationChanged += _controlpanel.SatPC32ServerModulationChanged;
            satpc32Server.SatModulationChanged += SDRSharp_ModuladiontChanged;

            satpc32Server.SatBandwidthChanged += _controlpanel.SatPC32ServerBandwidthChanged;
            satpc32Server.SatBandwidthChanged += SDRSharp_BandwidthChanged;

            satpc32Server.SatSatNogsIDChanged += _controlpanel.SatPC32ServerSatNogsIDChanged;
            satpc32Server.SatRecordBaseChanged += _controlpanel.SatPC32ServerRecordBaseChanged;



        }


        private void SDRSharp_DownlinkFreqChanged(string Frequency)
        {
            control_.Frequency = long.Parse(Frequency);
        }

        private void SDRSharp_ModuladiontChanged(string Modulation)
        {
            switch (Modulation.ToUpper())
            {
                case "AM":
                    control_.DetectorType = DetectorType.AM;
                    break;
                case "FM":
                case "FMD":
                case "NFM":
                    control_.DetectorType = DetectorType.NFM;
                    break;
                case "USB":
                    control_.DetectorType = DetectorType.USB;
                    break;
                case "LSB":
                    control_.DetectorType = DetectorType.LSB;
                    break;
                default:
                    control_.DetectorType = DetectorType.NFM;
                    break;
            }
        }

        private void SDRSharp_BandwidthChanged(string Bandwidth)
        {
            control_.FilterBandwidth = int.Parse(Bandwidth);
        }
    }
}
