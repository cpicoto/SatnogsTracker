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
using System.Threading;

namespace SDRSharp.SatnogsTracker
{
    class SatPC32DDE : IDisposable
    {
        private readonly NDde.Client.DdeClient ddeclient_;
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
        private String _DDEServerApp = "SatPC32";

        public event Action<bool> Connected;
        public event Action<bool> Enabled;
        public event Action<String> SatNameChanged;
        public event Action<String> SatDownlinkFreqChanged;
        public event Action<String> SatAzimuthChanged;
        public event Action<String> SatElevationChanged;
        public event Action<String> SatModulationChanged;
        public event Action<String> SatBandwidthChanged;
        public event Action<Boolean> SatRecordBaseChanged;
        public event Action<Boolean> SatRecordAFChanged;
        public event Action<Boolean> SatStreamAFChanged;
        public event Action<String> SatSatNogsIDChanged;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (ddeclient_ != null) ddeclient_.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
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
                    if (_ApplicationName == "Orbitron")
                    {
                        switch (value)
                        {
                            case "FM":
                                SatBandwidth = "6250";
                                break;
                            case "FM-W":
                                SatBandwidth = "12500";
                                break;
                            case "FM-N":
                                SatBandwidth = "6250";
                                break;
                            case "CW-N":
                                SatBandwidth = "300";
                                break;
                            case "CW-W":
                                SatBandwidth = "2000";
                                break;
                            case "USB":
                            case "LSB":
                                SatBandwidth = "14000";
                                break;
                            default:
                                SatBandwidth = "12500";
                                break;
                        }
                    }
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

        private Boolean _SatStreamAF;
        public bool SatStreamAF
        {
            get
            {
                return _SatStreamAF;
            }
            set
            {
                if (_SatStreamAF != value)
                {
                    _SatStreamAF = value;
                    SatStreamAFChanged?.Invoke(_SatStreamAF);
                }
            }
        }

        //Constructor
        public SatPC32DDE(String DDEApp)
        {
            //
            Console.WriteLine("Creating DDEClient");
            DDEServerApp = DDEApp;
            ddeclient_ = new NDde.Client.DdeClient(_ApplicationName, _LinkTopic);
            ddeclient_.Advise += OnAdvise;
            ddeclient_.Disconnected += OnDisconnected;
        }

        public void OnAdvise(Object sender, NDde.Client.DdeAdviseEventArgs e)
        {
            //Console.WriteLine("DDE CLient OnAdvise  Got this:{0}", e.Text);
            ParseDde(e.Text);
        }

        public void OnDisconnected(Object sender, NDde.Client.DdeDisconnectedEventArgs e)
        {
            Console.WriteLine("DDE CLient OnDisconnected  Got this:{0}", e.ToString());
            SatRecordBaseChanged?.Invoke(false);
            SatRecordAFChanged?.Invoke(false);
            Connected?.Invoke(false);
            //Start Threadpool to attempt connecting every 30 seconds
            ThreadPool.QueueUserWorkItem(RetryConnection);
        }

        public void RetryConnection(Object stateinfo)
        {
            while (!ddeclient_.IsConnected)
            {
                Thread.Sleep(3000);
                try { ddeclient_.Connect(); }
                catch
                {
                    Console.WriteLine("Failed to Connect to DDE Server, waiting...");
                }
            }
            ddeclient_.StartAdvise(_LinkItem, 1, true, 60000);
            Connected?.Invoke(true);
        }
        public void ParseDde(string Input)
        {
            string[] words = Input.Split(' ');
            String SatMA;
            Boolean _recording = false;
            Boolean _streaming = false;
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
                    {
                        SatRecordBase = true;
                        _recording = true;
                    }
                    else
                        SatRecordBase = false;
                }
                else if (word.StartsWith("RA"))
                {
                    if (word.Substring(2, word.Length - 2).ToLower().StartsWith("yes"))
                    {
                        SatRecordAF = true;
                        _recording = true;
                    }
                    else
                        SatRecordAF = false;
                }
                else if (word.StartsWith("RS"))
                {
                    if (word.Substring(2, word.Length - 2).ToLower().StartsWith("yes"))
                    {
                        SatStreamAF = true;
                        _streaming = true;
                    }
                    else
                        SatStreamAF = false;
                }
                else if (word.StartsWith("MA"))
                {
                    SatMA = word.Remove(0, 2);
                    if (!_recording)
                    {
                        SatRecordAF = false;
                        SatRecordBase = false;
                    }
                    if (!_streaming) SatStreamAF = false;

                }
                else if (word.StartsWith("** NO SATELLITE **"))
                {
                    SatName = "NONE";
                    SatDownlinkFreq = "145.900";
                    SatAzimuth = "180.0";
                    SatElevation = "0.0";
                    SatModulation = "FM";
                    SatBandwidth = "435.000";
                }
            }
        }

        public void Start()
        {
            Console.WriteLine("Created DDEClient, about to Connect");
            try
            {
                if (ddeclient_.IsConnected)
                {
                    ddeclient_.StartAdvise(_LinkItem, 1, true, 60000);
                }
                else
                {
                    try
                    {
                        ddeclient_.Connect();
                        Console.WriteLine("DDEClient Connected, start Advise");
                        ddeclient_.StartAdvise(_LinkItem, 1, true, 60000);
                        Connected?.Invoke(true);
                    }
                    catch
                    {
                        Console.WriteLine("Failed to Connect to DDE Server");
                        ThreadPool.QueueUserWorkItem(RetryConnection);
                    }
                }
                Enabled.Invoke(true);

            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e.Message);
            }
        }

        public void Stop()
        {
            Enabled.Invoke(false);
            Connected?.Invoke(false);
        }

        public void Abort()
        {
            Console.WriteLine("Abort DDE Client");
        }
    }
}
