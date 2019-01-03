using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SDRSharp.SatnogsTracker
{

    class SatPC32DDE
    {
        private NDde.Client.DdeClient ddeclient_;

        private String _SatName;
        private String _SatDownlinkFreq;
        private String _SatAzimuth;
        private String _SatElevation;
        private String _SatModulation;
        private String _SatBandwidth;
        private Boolean _SatRecordBase;
        private Boolean _SatRecordAF;
        private String _SatNogsID;
        private String _ApplicationName;
        private String _LinkTopic;
        private String _LinkItem;
        private String _DDEServerApp="SatPC32";



        public event Action<String> SatNameChanged;
        public event Action<String> SatDownlinkFreqChanged;
        public event Action<String> SatAzimuthChanged;
        public event Action<String> SatElevationChanged;
        public event Action<String> SatModulationChanged;
        public event Action<String> SatBandwidthChanged;
        public event Action<Boolean> SatRecordBaseChanged;
        public event Action<Boolean> SatRecordAFChanged;
        public event Action<String> SatSatNogsIDChanged;


        public String DDEServerApp
        {
            get { return _DDEServerApp; }
            set
            {
                _DDEServerApp = value;
                switch (value)
                {
                    case "SatPC32":
                        _ApplicationName = "SatPC32";
                        _LinkTopic = "SatPcDdeConv";
                        _LinkItem = "SatPcDdeItem";
                        break;
                    case "WxTrack":
                        _ApplicationName = "WxTrack";
                        _LinkTopic = "Tracking";
                        _LinkItem = "Tracking";
                        break;
                    case "Orbitron":
                        _ApplicationName = "Orbitron";
                        _LinkTopic = "Tracking";
                        _LinkItem = "TrackingData";
                        break;
                    default:
                        _ApplicationName = "SatPC32";
                        _LinkTopic = "SatPcDdeConv";
                        _LinkItem = "SatPcDdeItem";
                        _DDEServerApp = "SatPC32";
                        break;
                }
            }
        }

        public Boolean SatRecordBase
        {
            get { return _SatRecordBase; }
            set
            {
                if (_SatRecordBase != value)
                {
                    _SatRecordBase = value;
                    SatRecordBaseChanged?.Invoke(_SatRecordBase);
                }
                
            }
        }

        public Boolean SatRecordAF
        {
            get { return _SatRecordAF; }
            set
            {
                if (_SatRecordAF != value)
                {
                    _SatRecordAF = value;
                    SatRecordAFChanged?.Invoke(_SatRecordAF);
                }
            }
        }
        public String SatNogsID
        {
            get { return _SatNogsID; }
            set
            {
                if (_SatNogsID != value)
                    _SatNogsID = value;
                SatSatNogsIDChanged?.Invoke(_SatNogsID);
            }
        }
        public string SatName
        {
            get
            {
                return _SatName;
            }
            set
            {
                
                if (_SatName != value)
                {
                    _SatName = value;
                    SatNameChanged?.Invoke(_SatName);
                }
                
            }
        }

        public string SatDownlinkFreq
        {
            get { return _SatDownlinkFreq; }
            set
            {
                if (_SatDownlinkFreq != value)
                {
                    _SatDownlinkFreq = value;
                    SatDownlinkFreqChanged?.Invoke(_SatDownlinkFreq);
                }
            }
        }
        public string SatAzimuth
        {
            get { return _SatAzimuth; }

            set
            {
                if (_SatAzimuth != value)
                {
                    _SatAzimuth = value;
                    SatAzimuthChanged?.Invoke(_SatAzimuth);
                }
            }
        }
        public string SatElevation
        {
            get { return _SatElevation; }
            set
            {
                if (_SatElevation != value)
                {
                    _SatElevation = value;
                    SatElevationChanged?.Invoke(_SatElevation);
                }
            }
        }
        public string SatModulation
        {
            get { return _SatModulation; }
            set
            {
                if (_SatModulation != value)
                {
                    _SatModulation = value;
                    SatModulationChanged?.Invoke(_SatModulation);
                }
            }
        }
        public string SatBandwidth
        {
            get { return _SatBandwidth; }
            set
            {
                if (_SatBandwidth != value)
                {
                    long bw = long.Parse(value);
                    if ((bw > 500) && (bw < 32001))
                    {
                        _SatBandwidth = value;
                        SatBandwidthChanged?.Invoke(_SatBandwidth);
                    }
                }

            }
        }



        //Constructor
        public SatPC32DDE()
        {
            //
            Console.WriteLine("Creating DDEClient");
            DDEServerApp = "SatPC32";
            ddeclient_ = new NDde.Client.DdeClient(_ApplicationName, _LinkTopic);
            ddeclient_.Advise += OnAdvise;
            ddeclient_.Disconnected += OnDisconnected;
        }

        public void OnAdvise(Object sender, NDde.Client.DdeAdviseEventArgs e)
        {
            Console.WriteLine("DDE CLient OnAdvise  Got this:{0}", e.Text);
            ParseDde(e.Text);
        }

        public void OnDisconnected(Object sender, NDde.Client.DdeDisconnectedEventArgs e)
        {
            Console.WriteLine("DDE CLient OnDisconnected  Got this:{0}", e.ToString());
            Connected?.Invoke(false);
        }
        public void ParseDde(string Input)
        {
            string[] words = Input.Split(' ');
            String SatMA;
            foreach (string word in words)
            {
                if (word.StartsWith("SN"))
                {
                    SatName = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("AZ"))
                {
                    SatAzimuth = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("EL"))
                {
                    SatElevation = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("DM"))
                {
                    SatModulation = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("DN"))
                {
                    SatDownlinkFreq = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("BW"))
                {
                    SatBandwidth = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("ID"))
                {
                    SatNogsID = word.Substring(2, word.Length - 2);
                }
                else if (word.StartsWith("RB"))
                {
                    if (word.Substring(2, word.Length - 2).ToLower().StartsWith("yes"))
                        SatRecordBase = true;
                    else
                        SatRecordBase = false;
                }
                else if (word.StartsWith("RA"))
                {
                    if (word.Substring(2, word.Length - 2).ToLower().StartsWith("yes"))
                        SatRecordAF = true;
                    else
                        SatRecordAF = false;
                }
                else if (word.StartsWith("MA"))
                {
                    SatMA = word.Remove(0, 2);
                }
                else if (word.StartsWith("** NO SATELLITE **"))
                {
                    SatName = "NONE";
                    SatDownlinkFreq = "145.900";
                    SatAzimuth = "180.0";
                    SatElevation = "0.0";
                    SatModulation = "FM";
                    SatBandwidth = "435.000";
                    //SatUplinkMode = "FM";
                    //CheckSatAction(SatName);
                }
            }
        }

        public void Start()
        {

            Console.WriteLine("Created DDEClient, about to Connect");

            try
            {
                ddeclient_.Connect();
                Console.WriteLine("DDEClient Connected, start Advise");
                
                ddeclient_.StartAdvise(_LinkItem, 1, true, 60000);
                //                SatTextBox.Text="[Sat:"+SatName+",EL:"+SatElevation+",AZ:"+SatAzimuth+"], DFreq:"+SatDownLinkFrequency+"]";
                Enabled.Invoke(true);
                Connected?.Invoke(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e.Message);
            }



        }

        public void Stop()
        {
            try { ddeclient_.Disconnect(); }
            catch (Exception e)
            {
                Console.WriteLine("Exception trying to DDE Disconnect:" + e.Message);
            }
            Enabled.Invoke(false);
            Connected?.Invoke(false);
        }

        private void CancelComm()
        {
            this.Stop();
            Enabled?.Invoke(false);
        }


        private void Connection_established()
        {

            Connected?.Invoke(true);
        }

        private void connection_lost()
        {

            Connected?.Invoke(false);
        }
        //Parse the string received from SatPC32 on the DDE Channel
      
        public event Action<bool> Connected;
        public event Action<bool> Enabled;

    }
}
