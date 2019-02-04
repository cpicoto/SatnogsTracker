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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDRSharp.Common;
using NDde;
using System.Threading;
using SDRSharp.Radio;
using SDRSharp.WavRecorder;
using System.IO;
using System.Net;
using System.ComponentModel;
using Zeptomoby.OrbitTools;
using SDRSharp.PanView;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Compression;

namespace SDRSharp.SatnogsTracker
{
    public class SatnogsTrackerPlugin : ISharpPlugin
    {
        private const string _displayName = "Satnogs Tracker";
        private Controlpanel _controlpanel;
        private ISharpControl control_;
        private SatPC32DDE satpc32Server_;
        public Boolean CalculateSatVisibilityRunning;
        private readonly RecordingIQProcessor _iqObserver = new RecordingIQProcessor();
        private readonly RecordingAudioProcessor _audioProcessor = new RecordingAudioProcessor();
        private readonly RecordingAudioProcessor _UDPaudioProcessor = new RecordingAudioProcessor();
        private SimpleRecorder _audioRecorder;
        private SimpleStreamer _UDPaudioStreamer;


        private SimpleRecorder _basebandRecorder;
        private readonly WavSampleFormat _wavSampleFormat = WavSampleFormat.PCM16;
        private string _SatElevation;
        public Action<String> UpdateStatus;
        String MyStationFilePath = "";
        String NoradMappingFilePath = "";

        List<_Transmitter> Transmitters = new List<_Transmitter>();
        List<_Satellite> Satellites = new List<_Satellite>();
        List<SpectrumColumn> ActiveFrequencies = new List<SpectrumColumn>();

        StreamWriter LogFile;
        Site siteHome;
        HamSite siteSettings;
        Settings dlg = new Settings();

        public void Initialize(ISharpControl control)
        {
            Console.WriteLine("Initialize Plugin\r\n");
            control_ = control;

            /// Setup Audio Recording
            _audioProcessor.Enabled = false;
            _UDPaudioProcessor.Enabled = false;
            _iqObserver.Enabled = false;
            control_.RegisterStreamHook(_iqObserver, ProcessorType.RawIQ);
            control_.RegisterStreamHook(_audioProcessor, ProcessorType.FilteredAudioOutput);
            control_.RegisterStreamHook(_UDPaudioProcessor, ProcessorType.FilteredAudioOutput);
            Console.WriteLine(_audioProcessor.SampleRate);
            _audioRecorder = new SimpleRecorder(_audioProcessor);
            _UDPaudioStreamer = new SimpleStreamer(_UDPaudioProcessor,"127.0.0.1",7355);
            _basebandRecorder = new SimpleRecorder(_iqObserver);

            //Instanciate all needed objects
            _controlpanel = new Controlpanel();
            
            dlg.HamSiteChanged += StationSiteChanged;
            dlg.HamSiteChanged += _controlpanel.ControlPanel_HomeSite_Changed;

            dlg.UpdateTle += UpdateTleData;

            _controlpanel.ShowSettings += ShowSettingsForm;
            NoradMappingFilePath = DataLocation() + "SatNOGsMapping.json";
            LoadNoradMappingList();


            MyStationFilePath = DataLocation() + "MyStation.json";
            //HERE
            if (!File.Exists(MyStationFilePath))
            {
                dlg.MySite = new HamSite("Your CallSign", "0.0", "0.0", "0.0", "SatPC32");
                dlg.Show();
                //SaveHomeStation();
            }
            else
            {
                LoadHomeSiteFromJson();
                dlg.MySite = siteSettings;
            }
            //Link the objects together
            SatPC32DDE satpc32Server = new SatPC32DDE(siteSettings.DDEApp);
            satpc32Server_ = satpc32Server;

            #region Setup event handlers
            //Setup Custom Paints
            control_.WaterfallCustomPaint += SDRSharp_WaterFallCustomPaint;
            control_.SpectrumAnalyzerBackgroundCustomPaint += SDRSharp_SpectrumAnalyzerBackgroundCustomPaint;
            control_.SpectrumAnalyzerCustomPaint += SDRSharp_SpectrumAnalyzerCustomPaint;

            _controlpanel.SatPC32ServerStart += satpc32Server.Start;
            _controlpanel.SatPC32ServerStop += satpc32Server.Stop;

            satpc32Server.Connected += _controlpanel.SatPC32Server_Connected_Changed;
            satpc32Server.Enabled += _controlpanel.SatPC32Server_Enabled_Changed;

            //satpc32Server.FrequencyInHzChanged += _controlpanel.SatPC32ServerReceivedFrequencyInHzChanged;
            satpc32Server.SatNameChanged += _controlpanel.SatPC32ServerNameChanged;
            satpc32Server.SatNameChanged += SDRSharp_SatNameChanged;

            satpc32Server.SatDownlinkFreqChanged += _controlpanel.SatPC32ServerDownlinkFreqChanged;
            satpc32Server.SatDownlinkFreqChanged += SDRSharp_DownlinkFreqChanged;

            //Display Only
            satpc32Server.SatAzimuthChanged += _controlpanel.SatPC32ServerAzimuthChanged;
            satpc32Server.SatElevationChanged += _controlpanel.SatPC32ServerElevationChanged;
            satpc32Server.SatElevationChanged += SDRSharp_ElevationChanged;

            satpc32Server.SatModulationChanged += _controlpanel.SatPC32ServerModulationChanged;
            satpc32Server.SatModulationChanged += SDRSharp_ModuladiontChanged;

            satpc32Server.SatBandwidthChanged += _controlpanel.SatPC32ServerBandwidthChanged;
            satpc32Server.SatBandwidthChanged += SDRSharp_BandwidthChanged;

            satpc32Server.SatSatNogsIDChanged += _controlpanel.SatPC32ServerSatNogsIDChanged;
            satpc32Server.SatSatNogsIDChanged += SDRSharp_SatIDChanged;

            satpc32Server.SatRecordBaseChanged += _controlpanel.SatPC32ServerRecordBaseChanged;
            satpc32Server.SatRecordBaseChanged += SDRSharp_BasebandRecorderChanged;

            satpc32Server.SatRecordAFChanged += _controlpanel.SatPC32ServerRecordAFChanged;
            satpc32Server.SatRecordAFChanged += SDRSharp_AFRecorderChanged;

            _controlpanel.StartRecordingAF += SDRSharp_AFRecorderChanged;
            #endregion

            #region Load and Setup Satellite and Transmitter data to Memory
            UpdateTleData(false);
            #endregion

            Console.WriteLine("Tracking Initiated");
            //Start Background Doppler Calculations
            ThreadPool.QueueUserWorkItem(CalculateSatVisibility);


        }

        private void  UpdateTleData(Boolean alwaysdownload)
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
                dlg.UpdateIP +=_UDPaudioStreamer.UpdateIP;
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


        #region Control Panel Methods
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
            CalculateSatVisibilityRunning = false;
            StopBaseRecorder();
            StopAFRecorder();
            //StopUDPStreamer();
            satpc32Server_?.Abort();
            LogFile.Close();
        }

        public bool HasGui
        {
            get { return true; }
        }
        #endregion

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
            } catch (Exception e)
            {
                Console.WriteLine("Failed to Close Logfile: {0}", e.Message);
                return false;
            }
            return true;
        }
        #region Satellite Methods
        public List<_Satellite> GetSatellites()
        {
            return Satellites;
        }

        private void LoadSatellitesAndTransmitters(bool alwaysdownload)
        {
            GetFile("https://db.satnogs.org/api/satellites/", "Satellites.json", alwaysdownload);
            LogFile.WriteLine("Got the Satellites.json, Loading them to Memory now....");
            LoadSatsfromJson("Satellites.json");

            GetFile("https://db.satnogs.org/api/transmitters/", "Transmitters.json", alwaysdownload);
            LoadTransmittersfromJson("Transmitters.json");
        }

        private void LoadAllKeps(Boolean alwaysdownload)
        {
            //
            //http://www.celestrak.com/NORAD/elements/amateur.txt
            // DK3WN has a better amateur.txt file
            //http://www.dk3wn.info/tle/amateur.txt
            GetFile("http://www.celestrak.com/NORAD/elements/amateur.txt", "amateur.txt", alwaysdownload);
            LoadTxtKeps("amateur.txt");

            GetFile("http://www.celestrak.com/NORAD/elements/active.txt", "active.txt", alwaysdownload);
            LoadTxtKeps("active.txt");

            GetFile("https://www.amsat.org/tle/current/nasabare.txt", "nasabare.txt", alwaysdownload);
            LoadTxtKeps("nasabare.txt");

            GetFile("https://www.amsat.org/tle/current/nasa.all", "nasa.all", alwaysdownload);
            LoadTxtKeps("nasa.all");

            GetFile("http://www.is0lnf.com/wp-content/uploads/2018/01/cubesatis0lnf.txt", "cubesatis0lnf.txt", alwaysdownload);
            LoadTxtKeps("cubesatis0lnf.txt");

            GetFile("http://www.celestrak.com/NORAD/elements/cubesat.txt", "cubesat.txt", alwaysdownload);
            LoadTxtKeps("cubesat.txt");

            GetFile("http://www.celestrak.com/NORAD/elements/tle-new.txt", "tle-new.txt", alwaysdownload);
            LoadTxtKeps("tle-new.txt");
        }


        public string SatelliteName { get; private set; }
        public string SatelliteID { get; private set; }
        public string SatElevation
        {
            get { return _SatElevation; }
            set
            {
                if (_SatElevation != value)
                {
                    _SatElevation = value;
                    if (float.Parse(value) <= 0)
                    {
                        if (_basebandRecorder.IsRecording) StopBaseRecorder();
                        if (_audioRecorder.IsRecording) StopAFRecorder();
                        if (_UDPaudioStreamer.IsStreaming) StopUDPStreamer();
                    }
                }
            }
        }

        #endregion

        private void SDRSharp_DownlinkFreqChanged(string Frequency)
        {
            control_.Frequency = long.Parse(Frequency);
            if (control_.IsPlaying)
                control_.CenterFrequency = control_.Frequency;
        }

        private void SDRSharp_SatNameChanged(string SatName)
        {
            if (SatelliteName != SatName)
            {
                SatelliteName = SatName;
                if (_basebandRecorder.IsRecording) _basebandRecorder.StopRecording();
                if (_audioRecorder.IsRecording) _audioRecorder.StopRecording();
            }
        }

        private void SDRSharp_SatIDChanged(string SatID)
        {
            SatelliteID = SatID;
        }

        private void SDRSharp_ElevationChanged(string Elevation)
        {
            SatElevation = Elevation;
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
                case "CW":
                    control_.DetectorType = DetectorType.CW;
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

        private void SDRSharp_BasebandRecorderChanged(Boolean RecordBase)
        {
            if (RecordBase && !_basebandRecorder.IsRecording)
            {
                PrepareBaseRecorder();

                try
                {
                    if (RecordBase)
                    {
                        _basebandRecorder.StartRecording();
                    }
                }
                catch
                {
                    _basebandRecorder.StopRecording();
                    MessageBox.Show("Unable to Start BaseBand Recording", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void SDRSharp_AFRecorderChanged(Boolean RecordAF)
        {
            if (RecordAF && !_audioRecorder.IsRecording)
            {


                try
                {
                    if (RecordAF)
                    {
                        PrepareAFRecorder();
                        _audioRecorder.StartRecording();

                        PrepareUDPStreamer();
                        _UDPaudioStreamer.StartStreaming();
                    }
                }
                catch
                {
                    _audioRecorder.StopRecording();
                    MessageBox.Show("Unable to Start AF Recording", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (!RecordAF && _audioRecorder.IsRecording)
            {
                _audioRecorder.StopRecording();
            }
        }

        private void PrepareBaseRecorder()
        {
            DateTime startTime = DateTime.UtcNow;
            _basebandRecorder.SampleRate = _iqObserver.SampleRate;
            String BaseRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_IQ.wav";
            _basebandRecorder.FileName = RecordingLocation() + "\\" + BaseRecordingName;
            _basebandRecorder.Format = _wavSampleFormat;
        }
        private void PrepareAFRecorder()
        {
            DateTime startTime = DateTime.UtcNow;
            String AudioRecordingName = "";
            //_audioProcessor.SampleRate = 48000;
            _audioRecorder.SampleRate = _audioProcessor.SampleRate; 
            if ((SatelliteName==null) || (SatelliteID==null))
            {
                AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "CURRENT_FREQ__AF.wav";
            } else
                AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_AF.wav";
            _audioRecorder.FileName = RecordingLocation() + "\\" + AudioRecordingName;
            _audioRecorder.Format = _wavSampleFormat;
        }

        private void PrepareUDPStreamer()
        {
            DateTime startTime = DateTime.UtcNow;
            _UDPaudioStreamer.SampleRate = _audioProcessor.SampleRate;
            String AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_STREAM.wav";
            _UDPaudioStreamer.FileName = RecordingLocation() + "\\" + AudioRecordingName;
            _UDPaudioStreamer.Format = _wavSampleFormat;
        }
        private void StopBaseRecorder()
        {
            if (_basebandRecorder.IsRecording) _basebandRecorder.StopRecording();
        }
        private void StopAFRecorder()
        {
            if (_audioRecorder.IsRecording) _audioRecorder.StopRecording();
        }

        private void StopUDPStreamer()
        {
            if (_UDPaudioStreamer.IsStreaming) _UDPaudioStreamer.StopStreaming();
        }

        private void SDRSharp_WaterFallCustomPaint(object sender, CustomPaintEventArgs e)
        {
            CustomPaintEventArgs CustomArgs = e;
            Waterfall _waterfall = (Waterfall)sender;
            Point Where = e.CursorPosition; //get
            float MinFreq = _waterfall.CenterFrequency - _waterfall.DisplayedBandwidth / 2;
            float MaxFreq = _waterfall.CenterFrequency + _waterfall.DisplayedBandwidth / 2;
            foreach (var sat in Satellites)
            {
                if (sat.InSpectrum(MinFreq, MaxFreq))
                {
                    foreach (var t in sat.Channel)
                    {
                        if (t.InSpectrum(MinFreq, MaxFreq))
                        {
                            long FreqX = _waterfall.PointToFrequency(e.CursorPosition.X);
                            long MinFreqX = (long)(float.Parse(t.DownlinkFreqWithDoppler) - (float.Parse(t.BandwidthHz.ToString()) / 2));
                            long MaxFreqX = (long)(float.Parse(t.DownlinkFreqWithDoppler) + (float.Parse(t.BandwidthHz.ToString()) / 2));

                            if ((FreqX >= MinFreqX - 10) && (FreqX <= MaxFreqX + 10))
                                e.CustomTitle += sat.Name + "(^" + t.CurrentEL.ToString("00.00") + "):" + t.FreqLine + ":" + Dotify(t.DownlinkFreqWithDoppler, 9) + "\r\n";
                        }
                    }
                }
            }
            String Title = e.CustomTitle; //get|set
        }
        private void SDRSharp_SpectrumAnalyzerBackgroundCustomPaint(object sender, CustomPaintEventArgs e)
        {
            SpectrumAnalyzer Spectrum = (SpectrumAnalyzer)sender;
            CustomPaintEventArgs LocalArgs = e;
            Point Start;
            Point Top;
            SolidBrush myBrush;
            Rectangle Output_Rectangle;
            SolidBrush myBrush2;
            Rectangle Output_Rectangle2;
            Spectrum.StatusText = "";
            float MinFreq = Spectrum.CenterFrequency - Spectrum.DisplayedBandwidth / 2;
            float MaxFreq = Spectrum.CenterFrequency + Spectrum.DisplayedBandwidth / 2;
            foreach (var sat in Satellites)
            {
                if (sat.InSpectrum(MinFreq, MaxFreq))
                {
                    Spectrum.StatusText += sat;
                    foreach (var t in sat.Channel)
                    {
                        if (t.InSpectrum(MinFreq, MaxFreq))
                        {
                            int Height = (int)(t.CurrentEL * (Spectrum.Height-30) / 90);
                            int MaxHeight = (int)(sat.NextMaxEl * (Spectrum.Height-30) / 90);
                            Start = new Point((int)Spectrum.FrequencyToPoint(double.Parse(t.DownlinkFreqWithDoppler) - t.BandwidthHz / 2),
                                               Spectrum.Height - Height-30);
                            Top = new Point((int)Spectrum.FrequencyToPoint(double.Parse(t.DownlinkFreqWithDoppler) - t.BandwidthHz / 2),
                                               Spectrum.Height - MaxHeight-30);
                            int Width = (int)Spectrum.FrequencyToPoint(double.Parse(t.DownlinkFreqWithDoppler) + t.BandwidthHz / 2) - Start.X;
                            Output_Rectangle = new Rectangle(Start, new Size(Width, Height));
                            myBrush = new SolidBrush(Color.Red);
                            e.Graphics.FillRectangle(myBrush, Output_Rectangle);
                            Output_Rectangle2 = new Rectangle(Top, new Size(Width, 5));
                            myBrush2 = new SolidBrush(Color.Green);
                            e.Graphics.FillRectangle(myBrush2, Output_Rectangle2);
                        }
                    }
                }
            }
        }

        private void SDRSharp_SpectrumAnalyzerCustomPaint(object sender, CustomPaintEventArgs e)
        {
            SpectrumAnalyzer Spectrum = (SpectrumAnalyzer)sender;
            e.CustomTitle = "";
            int Span = Spectrum.DisplayedBandwidth;
            int SpectrumWidth = Spectrum.SpectrumWidth;
            //Spectrum.StatusText = ((Spectrum.CenterFrequency - Spectrum.DisplayedBandwidth / 2)).ToString() + "--" +
            //   ((Spectrum.CenterFrequency + Spectrum.DisplayedBandwidth / 2));
            Spectrum.StatusText = "";
            float MinFreq = Spectrum.CenterFrequency - Spectrum.DisplayedBandwidth / 2;
            float MaxFreq = Spectrum.CenterFrequency + Spectrum.DisplayedBandwidth / 2;
            foreach (var sat in Satellites)
            {
                if (sat.InSpectrum(MinFreq, MaxFreq))
                {
                    Spectrum.StatusText += "[" + sat.Name + "]";
                    foreach (var t in sat.Channel)
                    {
                        if (t.InSpectrum(MinFreq, MaxFreq))
                        {
                            long FreqX = Spectrum.PointToFrequency(e.CursorPosition.X);
                            long MinFreqX = (long)(float.Parse(t.DownlinkFreqWithDoppler) - (float.Parse(t.BandwidthHz.ToString()) / 2));
                            long MaxFreqX = (long)(float.Parse(t.DownlinkFreqWithDoppler) + (float.Parse(t.BandwidthHz.ToString()) / 2));

                            if ((FreqX >= MinFreqX - 10) && (FreqX <= MaxFreqX + 10))
                                e.CustomTitle += sat.Name + "(^" + t.CurrentEL.ToString("00.00") + "):" + t.FreqLine + ":" + Dotify(t.DownlinkFreqWithDoppler, 9) + "\r\n";
                        }
                    }
                }
            }
            e.CustomTitle += "\r\n" + Dotify(Spectrum.PointToFrequency(e.CursorPosition.X).ToString(), 9);
        }

        #region SatNogs Satellite Help Methods

        public void CalculateSatVisibility(Object stateinfo)
        {
            //int Iterations = (int)stateinfo;
            CalculateSatVisibilityRunning = true;
            DateTime AOS = DateTime.UtcNow;
            Topo topoLook;
            Topo topoLook2;
            EciTime eciSDP4;
            DateTime PotentialAOS;
            DateTime PotentialEOS;
            Double Elevation;
            while (CalculateSatVisibilityRunning)
            {
                //Walk the List of Satellites for the ones that don't have NextAOS
                foreach (_Satellite sat in Satellites)
                {
                    //Just consider active satellites
                    if (sat.IsActive)
                    {   //Only Consider Sats with valid Transmitters
                        if (sat.IsActive && sat.Channel != null)
                        {
                            //Next AOS is no longer valid
                            if (!sat.HasNextAOS)
                            {
                                Console.WriteLine("Calculate AOS for {0} because it no longer has NextAOS", sat.Name);
                                Elevation = 0;
                                //
                                for (int minute = 0; (minute < 60 * 24) && !sat.HasNextAOS && sat.IsActive; minute++)
                                {
                                    PotentialAOS = DateTime.UtcNow.AddMinutes(minute);
                                    try
                                    {
                                        eciSDP4 = sat.satSDP4.PositionEci(PotentialAOS);
                                        topoLook = siteHome.GetLookAngle(eciSDP4);
                                        Elevation = topoLook.ElevationDeg;
                                    }
                                    catch (Zeptomoby.OrbitTools.DecayException e)
                                    {
                                        Console.WriteLine("Marking {0} Invalid due to DecayException:{1}", sat.Name, e.Message);
                                        sat.IsActive = false;
                                        sat.IsDecay = true;
                                        Elevation = -1;
                                    }

                                    if (sat.IsActive && (Elevation > 0))
                                    {
                                        sat.HasNextAOS = true;
                                        sat.NextAOS = PotentialAOS;
                                        sat.TimeToAOS = PotentialAOS.Subtract(DateTime.Now);
                                        //Console.WriteLine("{0} To to Aos {1}", sat.Name, sat.TimeToAOS);
                                        sat.NextMaxEl = Elevation;
                                        double WhileUp = Elevation;
                                        for (int secondstoeos = minute * 60; (secondstoeos < 60 * 60 * 24) && (WhileUp > 0); secondstoeos++)
                                        {
                                            PotentialEOS = DateTime.UtcNow.AddSeconds(secondstoeos);
                                            eciSDP4 = sat.satSDP4.PositionEci(PotentialEOS);
                                            topoLook2 = siteHome.GetLookAngle(eciSDP4);
                                            WhileUp = topoLook2.ElevationDeg;

                                            //Find Max Elev
                                            if (WhileUp > sat.NextMaxEl)
                                            {
                                                sat.NextMaxEl = WhileUp;
                                                sat.NextTCA = PotentialEOS;
                                            }

                                            //Find EOS
                                            if (WhileUp <= 0)
                                            {
                                                sat.NextEOS = PotentialEOS;
                                                sat.TimeToEOS = PotentialEOS.Subtract(DateTime.Now);
                                                /*
                                                LogFile.WriteLine("{0}:AOS:{1}/EOS:{2} MaxEl {3}", 
                                                    sat.Name,
                                                    sat.NextAOS,
                                                    sat.NextEOS,
                                                    sat.NextMaxEl);
                                                */
                                                Console.WriteLine("{0}:AOS:{1}/EOS:{2} MaxEl {3}",
                                                    sat.Name,
                                                    sat.NextAOS,
                                                    sat.NextEOS,
                                                    sat.NextMaxEl);

                                                break;
                                            }
                                        }
                                    }
                                }
                                if (Elevation < 0)
                                {
                                    sat.IsActive = false; //Can't find positive elevation in 24 Hours remove from the List 
                                    Console.WriteLine("Marking Satellite:{0} inactive because of no positive elevation in 24hours cycle", sat.Name);
                                }
                            }
                            else
                            {
                                //Recalculate time to next AOS/EOS
                                DateTime RightNow = DateTime.UtcNow;
                                if (sat.NextAOS < RightNow)
                                {
                                    //We are past AOS so satellie might be visible if we are still before EOS
                                    sat.TimeToAOS = sat.NextAOS.Subtract(RightNow);

                                    //If EOS is still in the future.
                                    if (sat.NextEOS >= RightNow)
                                    {
                                        //Calculate Doppler for Active Frequencies.
                                        try
                                        {
                                            eciSDP4 = sat.satSDP4.PositionEci(RightNow);
                                        }
                                        catch (Zeptomoby.OrbitTools.DecayException e)
                                        {
                                            Console.WriteLine("Marking {0} Invalid due to DecayException:{1}", sat.Name, e.Message);
                                            sat.IsActive = false;
                                            sat.IsDecay = true;
                                            break;
                                        }
                                        if (eciSDP4 != null)
                                        {
                                            GeoTime geo = new GeoTime(eciSDP4);
                                            topoLook = siteHome.GetLookAngle(eciSDP4);
                                            double doppler100 = -100.0e06 * ((topoLook.RangeRate * 1000.0) / 299792458.0);
                                            sat.CurrentEL = topoLook.ElevationDeg;
                                            if (sat.CurrentEL >= 0) //Positive Elevation calculate all doppler freqs
                                            {
                                                int counter = 0;
                                                String LastFreqCache = "";
                                                String LastDopplerFreqCache = "";
                                                foreach (var t in sat.Channel)
                                                {
                                                    t.CurrentEL = sat.CurrentEL;
                                                    //Console.WriteLine("Calculating Doppler for: {0} from {1}", t.DownlinkFreqWithDoppler, t.FreqLine);
                                                    if (t.DownlinkFreq != LastFreqCache)
                                                    {
                                                        double downlink = Convert.ToDouble(t.DownlinkFreq);
                                                        double dopp = 1.0e-08 * (doppler100 * downlink);
                                                        double newdownlink = dopp + downlink;
                                                        t.DownlinkFreqWithDoppler = newdownlink.ToString("f0");
                                                        LastFreqCache = t.DownlinkFreq;
                                                        LastDopplerFreqCache = t.DownlinkFreqWithDoppler;
                                                    }
                                                    else
                                                    {
                                                        t.DownlinkFreqWithDoppler = LastDopplerFreqCache;
                                                    }
                                                    counter++;
                                                    Console.WriteLine("[{0}]:Trx:[{1}]:{2} from {3}", counter, t.DownlinkFreqWithDoppler, t.FreqLine, sat.Name);
                                                    //LogFile.WriteLine("[{0}]:Trx:[{1}]:{2} from {3}", counter, t.DownlinkFreqWithDoppler, t.FreqLine, sat.Name);
                                                }
                                                //LogFile.Flush();
                                            }
                                        }
                                    }
                                }
                                else
                                {   //Not reached AOS yet
                                    sat.TimeToAOS = sat.NextAOS.Subtract(sat.NextAOS);
                                }

                                if (sat.NextEOS > RightNow)
                                {   //Not reached EOS yet
                                    sat.TimeToEOS = sat.NextEOS.Subtract(RightNow);

                                }

                                // Invalidate AOS if EOS time has passed
                                if (sat.HasNextAOS && (RightNow > sat.NextEOS))
                                {
                                    sat.HasNextAOS = false;
                                    sat.NextMaxEl = -1;
                                    //sat.CurrentEL = -1;
                                    //sat.TimeToEOS = sat.NextAOS.Subtract(sat.NextAOS);
                                    /*
                                    LogFile.WriteLine("EOS passed for {0} Aos:{1} Eos:{2} and now is {3}", 
                                        sat.Name,
                                        sat.NextAOS,
                                        sat.NextEOS,
                                        RightNow);
                                    LogFile.Flush();
                                    */
                                }
                            }
                        }
                    }
                }
                //Console.WriteLine("--Calculate AOS  End ---\n\n");
                Thread.Sleep(500);
            }
        }

        _Satellite FindSatelliteByNumber(string Number)
        {
            foreach (_Satellite s in Satellites)
            {
                if (s.Number.StartsWith(Number)) return s;
            }
            return null;
        }
        _Satellite FindSatelliteByName(string Name)
        {
            foreach (_Satellite s in Satellites)
            {
                if (s.Name.StartsWith(Name)) return s;
            }
            return null;
        }
        public void CreateSatRecordFromKep(string FileIn, string NameIn, string Keps1, string Keps2)
        {
            //Check if Satellite already exists
            string[] items = Keps2.Split(' ');
            string SatNumber = items[1].TrimStart('0');
            _Satellite sat = FindSatelliteByNumber(SatNumber);
            //Console.WriteLine("## CreateSatRecord:{0} /{1}", NameIn, SatNumber);
            if (sat == null)
            {
                Tle t = new Tle(NameIn, Keps1, Keps2);
                Satellites.Add(new _Satellite()
                {
                    Name = NameIn.Trim(),
                    Number = SatNumber,
                    KepSource = FileIn,
                    KepsLine1 = Keps1,
                    KepsLine2 = Keps2,
                    Tle1 = t,
                    satSDP4 = new Satellite(t),
                    HasKeps = true,
                    IsVisible = false,
                    IsActive = true,
                    IsDecay = false,
                    nChannels = 0,
                    Radius = 0,
                    Footprint = 0,
                }
                                 );
            }
            else
            {
                //Just Update Keps since record already exists
                sat.Tle1 = new Tle(NameIn, Keps1, Keps2);
                sat.KepsLine1 = Keps1;
                sat.KepsLine2 = Keps2;
                sat.satSDP4 = new Satellite(sat.Tle1);
                sat.HasKeps = true;
            }
        }

        public Boolean LoadTxtKeps(string file)
        {
            Boolean IsNasaAll;
            UpdateStatus?.Invoke(file);
            if (file == "nasa.all")
                IsNasaAll = true;
            else
                IsNasaAll = false;

            string Filename = DataLocation() + file;

            try
            {
                StreamReader reader = File.OpenText(Filename);
                string KepsLine1, KepsLine2;
                string line;
                if (IsNasaAll)
                {
                    for (int i = 1; i < 16; i++)
                    {
                        line = reader.ReadLine();
                    }
                }
                while (((line = reader.ReadLine()) != null))
                {
                    if ((line != "") && (line != "/EX"))
                    {
                        KepsLine1 = reader.ReadLine();
                        KepsLine2 = reader.ReadLine();
                        CreateSatRecordFromKep(file, line, KepsLine1, KepsLine2);
                        //Console.WriteLine("Added Keps from [{2}] for:[{0}] Id:{1}", line.Trim(), KepsLine2.Split(' ')[1], Filename);
                    }
                }
                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("File not found: {0}, possible no connection to download", Filename);
                return false;
            }
        }

        public const int DAYS_BEFORE_DOWNLOAD = 7;
        private StreamWriter BandPlanFile = null;
        public void ListSatellitesfromCollection()
        {
            try { BandPlanFile = new StreamWriter(ProgramLocation() + "BandPlan.xml"); }
            catch (Exception e)
            {
                LogFile.WriteLine($"Can't open file:{0} error:{1}", ProgramLocation() + "BandPlan.xml", e.Message);
            }
            BandPlanFile.WriteLine("<ArrayOfRangeEntry xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
            foreach (_Satellite sat in Satellites)
            {
                {
                    LogFile.WriteLine("---------------------------------------------------------------------------------------------------------");
                    //LogFile.WriteLine("           #:" + counter);
                    LogFile.WriteLine("        Name:[" + sat.Name + "]\t\tCallsign:" + sat.Callsign);
                    LogFile.WriteLine("   SatnogsID:" + sat.ReportingID);
                    LogFile.WriteLine("  TrackingID:" + sat.Number);
                    LogFile.WriteLine("       Keps1:" + sat.KepsLine1);
                    LogFile.WriteLine("       Keps2:" + sat.KepsLine2);
                    LogFile.WriteLine("   Keps from:" + sat.KepSource);
                    if (sat.Channel != null) LogFile.WriteLine(" N# Channels:" + sat.Channel.Count);
                    LogFile.WriteLine(".........................................................................................................");
                    if (sat.Channel != null)
                        foreach (_Transmitter trx in sat.Channel)
                        {
                            //LogFile.WriteLine("\t>>>>>Channel:" + i.ToString());
                            LogFile.WriteLine("\t    FreqLine:" + trx.FreqLine);
                            LogFile.WriteLine("\t    Downlink:[" + trx.DownlinkStart + "--" + trx.DownlinkEnd + "] Mode:" + trx.DownlinkMode);
                            LogFile.WriteLine("\t      Uplink:[" + trx.UplinkStart + "--" + trx.UplinkEnd + "] Mode:" + trx.UplinkMode + "BandWidth:" + trx.BandwidthHz);
                            LogFile.WriteLine("\tDownlinkFreq:" + trx.DownlinkFreq + "\t\tMode:" + trx.DownlinkMode);
                            LogFile.WriteLine("\tDLinkCorrect:" + trx.DownLinkCorrection);
                            LogFile.WriteLine("\t Mode String:" + trx.Modes);
                            LogFile.WriteLine();
                            if (trx.DownlinkFreq != null)
                            {
                                String MinFreq = (long.Parse(trx.DownlinkFreq) - 100).ToString();
                                String MaxFreq = (long.Parse(trx.DownlinkFreq) + 100).ToString();
                                BandPlanFile.WriteLine("<RangeEntry minFrequency = \"{0}\" maxFrequency=\"{1}\" mode=\"{2}\" step=\"12500\" color=\"red\">{3}</RangeEntry>",
                                    MinFreq,
                                    MaxFreq,
                                    "NFM",
                                    sat.Name + ":" + trx.FreqLine);
                            }

                        }

                    LogFile.WriteLine();
                }

            }
            //Console.WriteLine();
            BandPlanFile.WriteLine("</ArrayOfRangeEntry>");
            BandPlanFile.Flush();
            BandPlanFile.Close();
        }

        /// <summary>
        /// http get a file from URL and Cache Locally
        /// </summary>
        /// <param name="url">URL of the remote file</param>
        /// <param name="filepath">Where to store the file locally</param>
        public Boolean GetFile(string url, string filepath, Boolean alwaysdownload)
        {
            Boolean download = false;
            filepath = DataLocation() + filepath;
            if (File.Exists(filepath))
            {
                DateTime creation = File.GetCreationTime(filepath);
                DateTime last_write = File.GetLastWriteTime(filepath);
                DateTime now = DateTime.Now;
                TimeSpan span = now.Subtract(last_write);
                Console.WriteLine("File: {0:S} exists and it is {1:D} days old from URL {2}", filepath, span.Days, url);
                if ((span.Days > DAYS_BEFORE_DOWNLOAD) || alwaysdownload)
                {
                    download = true;
                    Console.WriteLine("File: {0:S} will download from URL {1}", filepath, url);
                }
                else return true; //We already have a recent file
            }
            else download = true;

            if (download)
            {
                HttpWebRequest httpRequest = (HttpWebRequest)
                WebRequest.Create(url);
                httpRequest.Method = WebRequestMethods.Http.Get;
                // Get back the HTTP response for web server
                HttpWebResponse httpResponse = null;
                try { httpResponse = (HttpWebResponse)httpRequest.GetResponse(); }
                catch (Exception e)
                {
                    Console.WriteLine("Failled to Execute  Get {0} with {1}:", url, e.Message);
                    if (File.Exists(filepath))
                        return false;
                    else
                    {
                        return false; //Not able to download file right now
                    }
                }

                Stream httpResponseStream = httpResponse.GetResponseStream();
                //Save the file to your local disk
                // Define buffer and buffer size
                int bufferSize = 1024;
                byte[] buffer = new byte[bufferSize];
                int bytesRead = 0;

                // Read from response and write to file
                FileStream fileStream = File.Create(filepath);
                while ((bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                } // end while
                fileStream.Close();
                return true;
            }
            return false;
        }

        public Boolean LoadSatsfromJson(string file)
        {
            _Satellite sat = null;
            List<JsonSatellite> Sats;
            file = DataLocation() + file;
            UpdateStatus?.Invoke(file);
            StreamReader reader = File.OpenText(file);
            try
            {
                Sats = (List<JsonSatellite>)Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonSatellite>>(reader.ReadLine());
            }
            catch (Exception e)
            {
                Console.WriteLine(">>>LoadSatsfromJson: Failed to load:{0}", e.Message);
                return false;
            }
            foreach (var s in Sats)
            {
                //Console.WriteLine("JSON Satellite: {0}-{1} with Status:{2}", s.norad_cat_id, s.name,  s.status);
                //LogFile.WriteLine("JSON Satellite: {0}-{1} with Status:{2}", s.norad_cat_id, s.name, s.status);
                String stat = (String)s.status;
                String noradID = (String)s.norad_cat_id.ToString();
                String Name = (String)s.name;

                if (noradID.StartsWith("999"))
                {
                    s.norad_cat_id = int.Parse(ReplaceTempID(noradID));
                    LogFile.WriteLine("Mapping Satelite {0} from {1} to {2} ", Name, noradID, s.norad_cat_id);
                }

                if ((stat.IndexOf("re-entered") == -1) && (stat.IndexOf("dead") == -1))
                {
                    sat = FindSatelliteByNumber(s.norad_cat_id.ToString());
                    sat = null;
                    if (sat == null)
                    {
                        String SatName = s.name.ToString().ToUpper();
                        String[] words = SatName.Split(' ');
                        for (int j = words.Length; ((j > 1) && (sat == null)); j--)
                        {
                            String lookfor = "";
                            for (int k = 0; k < j; k++)
                                lookfor += words[k] + " ";
                            sat = FindSatelliteByName(lookfor.Trim());
                        }

                        if (sat == null)
                        {
                            Console.WriteLine("##### Couldn't find Satellite:" + s.name + " ## (" + s.norad_cat_id + ") ################");
                        }
                    }
                    else
                    {
                        if (s.status == "alive")
                            sat.IsActive = true;
                        else
                            sat.IsActive = false;

                        //Use the Name from SatNogs for Temporary Sats
                        if (noradID.StartsWith("999"))
                        {
                            sat.Name = s.name.ToString().ToUpper();
                            sat.ReportingID = noradID;
                        }
                        else sat.ReportingID = s.norad_cat_id.ToString();

                        sat.NickNames = (string)s.names;
                        sat.URL = (string)s.image;
                        sat.Bands = "";
                        if (sat.URL != null) sat.URL = sat.URL.Trim();
                    }
                }
            }
            reader.Close();
            return true;
        }

        public void SatnogsCallsignChanged(String Call)
        {
            siteSettings.Callsign = Call;
        }
        public Boolean LoadHomeSiteFromJson()
        {
            if (!File.Exists(MyStationFilePath))
            {
                try
                {
                    siteSettings = new HamSite("HOME", "47.6458", "-122.2084", "0.065", "SatPC32");





                    using (StreamWriter fileHandle = File.CreateText(MyStationFilePath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(fileHandle, siteSettings);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to Open {0} with Exception:{1}", MyStationFilePath, e.Message);
                    return false;
                }
            }
            else
            {
                StreamReader reader = File.OpenText(MyStationFilePath);
                try
                {
                    siteSettings = (HamSite)Newtonsoft.Json.JsonConvert.DeserializeObject<HamSite>(reader.ReadLine());
                }
                catch (Exception e)
                {
                    Console.WriteLine(">>>LoadSatsfromJson: Failed to load:{0}", e.Message);
                    return false;
                }
            }
            try
            {
                siteHome = new Site(double.Parse(siteSettings.Latitude),
                    double.Parse(siteSettings.Longitude),
                    double.Parse(siteSettings.Altitude),
                    siteSettings.Callsign);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create siteHome with exception:{0}", e.Message);
                return false;
            }
            return true;
        }
        public void LoadTransmittersfromJson(string file)
        {
            _Satellite sat = null;
            StreamReader reader = File.OpenText(DataLocation() + file);
            string Descr;
            string TrxBand;
            UpdateStatus?.Invoke(file);
            List<JsonTransmitter> Transmitters = (List<JsonTransmitter>)Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonTransmitter>>(reader.ReadLine());

            foreach (JsonTransmitter t in Transmitters)
            {
                //LogFile.WriteLine("JSON Transmitter: uuid:{0} descr:{1} alive:{2} Uplink_low:{3} Uplink_high:{4}, Downlink_Low:{5}, Downlink_High:{6}, mode_id:{7}, invert:{8}, baud:{9}, norad_cat_id:{10}", t.uuid, t.description, t.alive, t.uplink_low, t.uplink_high,t.downlink_low, t.downlink_high, t.mode_id, t.invert, t.baud, t.norad_cat_id);
                //Console.WriteLine("JSON Transmitter: {0}-{1} or {2}", t.norad_cat_id.ToString(), t.description, t.downlink_low);

                String noradID = t.norad_cat_id.ToString();
                if (noradID.StartsWith("999"))
                {
                    t.norad_cat_id = int.Parse(ReplaceTempID(noradID));
                    LogFile.WriteLine("Mapping Satelite TRX from {0} to {1} \r\n", noradID, t.norad_cat_id);
                }
                sat = FindSatelliteByNumber(t.norad_cat_id.ToString());
                if (sat == null)
                {
                    LogFile.WriteLine("##### Couldn't find Satellite: {0} ################", t.norad_cat_id);
                }
                if (sat != null)
                {

#pragma warning disable IDE0017 // Simplify object initialization
                    _Transmitter trx = new _Transmitter();
#pragma warning restore IDE0017 // Simplify object initialization
                    trx.ID = t.uuid;
                    trx.SpaceID = t.norad_cat_id.ToString();
                    trx.IsActive = true;
                    trx.FreqLine = Descr = t.description;// + " " + t.baud;
                    trx.DownlinkStart = t.downlink_low;
                    trx.HasDownLinkDoppler = true;
                    trx.IsSplit = false;
                    trx.ModemPort = null;

                    if (t.downlink_high != null) trx.DownlinkEnd = t.downlink_high;
                    else trx.DownlinkEnd = trx.DownlinkStart;

                    if ((Descr.IndexOf("U/") != -1) | (Descr.IndexOf("V/") != -1) | (Descr.IndexOf("L/") != -1) |
                        (Descr.IndexOf("ransponder") != -1)) //transponder
                        trx.IsTransponder = true;
                    else
                        trx.IsTransponder = false;


                    if (trx.BandwidthHz == 0)
                    {
                        if ((trx.FreqLine.IndexOf("1200") != -1) || (trx.FreqLine.IndexOf("1k2") != -1) || (trx.FreqLine.IndexOf("1K2") != -1))
                            trx.BandwidthHz = 5000;
                        else if ((trx.FreqLine.IndexOf("4800") != -1) || (trx.FreqLine.IndexOf("4k8") != -1) || (trx.FreqLine.IndexOf("4K8") != -1))
                            trx.BandwidthHz = 6500;
                        else if ((trx.FreqLine.IndexOf("9600") != -1) || (trx.FreqLine.IndexOf("9k6") != -1) || (trx.FreqLine.IndexOf("9K6") != -1))
                            trx.BandwidthHz = 12500;
                        else if ((trx.FreqLine.IndexOf("19200") != -1) || (trx.FreqLine.IndexOf("19k2") != -1) || (trx.FreqLine.IndexOf("19K2") != -1) || (trx.FreqLine.IndexOf("40k") != -1))
                            trx.BandwidthHz = 30000;
                        else if ((trx.FreqLine.IndexOf("CW") != -1) || (trx.FreqLine.IndexOf("cw") != -1) || (trx.FreqLine.IndexOf("Cw") != -1))
                            trx.BandwidthHz = 300;
                        else trx.BandwidthHz = 12500;
                    }
                    trx.DownLinkChanged = false;

                    //Single Frequency
                    if (trx.DownlinkStart == trx.DownlinkEnd)
                    {
                        trx.DownlinkFreq = t.downlink_low;
                        if ((Descr.IndexOf("4k8") != -1) ||
                                 (Descr.IndexOf("4800") != -1))
                        {
                            trx.DownlinkMode = "FMD";
                            trx.ModemPort = "8048";
                        }
                        else
                        if ((Descr.IndexOf("9600") != -1) ||
                                 //(Descr.IndexOf("40k") != -1) ||
                                 //(Descr.IndexOf("GMSK") != -1) ||
                                 //(Descr.IndexOf("Telemetry") != -1) ||
                                 (Descr.IndexOf("9K6") != -1) ||
                                 (Descr.IndexOf("9k6") != -1))
                        {
                            if (Descr.IndexOf("BPSK") != -1)
                            {
                                trx.DownlinkMode = "USB";
                                trx.ModemPort = "8062";
                                //trx.DownLinkCorrection = -2700;
                            }
                            else
                            {
                                trx.DownlinkMode = "FMD";
                                trx.ModemPort = "8096"; //FSK 9600
                            }
                        }
                        else if ((Descr.IndexOf("19K2") != -1) || (Descr.IndexOf("19k2") != -1) || (Descr.IndexOf("19200") != -1))
                        {
                            trx.DownlinkMode = "FMD";
                            trx.ModemPort = "8019";
                        }
                        else if ((Descr.IndexOf("1K2") != -1) ||
                            (Descr.IndexOf("1k2") != -1) ||
                            (Descr.IndexOf("1200") != -1) ||
                            (Descr.IndexOf("12") != -1) ||
                            (Descr.IndexOf("AFSK Packet") != -1) ||
                            (Descr.IndexOf("APRS AFSK") != -1) ||
                            (Descr.IndexOf("AFSK") != -1) ||
                            (Descr.IndexOf("AFSK TLM") != -1))
                        {
                            if (Descr.IndexOf("BPSK") != -1)
                            {
                                trx.DownlinkMode = "USB";
                                trx.ModemPort = "8010";  //BPSK 1200
                                //trx.DownLinkCorrection = -2700;
                            }
                            else
                            {
                                trx.DownlinkMode = "FM";
                                trx.ModemPort = "8012"; //FSK 1200
                            }
                        }
                        else if (Descr.IndexOf("BPSK") != -1)
                        {
                            trx.DownlinkMode = "USB";
                            trx.ModemPort = "8010"; //BPSK 1200
                        }
                        else if ((Descr.IndexOf("SSB") != -1) ||
                                 (Descr.IndexOf("CW") != -1) ||
                                 (Descr.IndexOf("Beacon") != -1))
                        {
                            trx.DownlinkMode = "USB";

                        }
                        else
                        {
                            trx.DownlinkMode = "FM";

                        }

                    }
                    else
                    {
                        trx.DownlinkMode = "USB";
                        trx.DownlinkFreq = ((Convert.ToInt64(trx.DownlinkEnd) + Convert.ToInt64(trx.DownlinkStart)) / 2).ToString("F0");
                    }

                    if (trx.DownlinkFreq != null)
                    {
                        if (trx.DownlinkFreq.StartsWith("14")) TrxBand = "2m";
                        else if (trx.DownlinkFreq.StartsWith("24")) TrxBand = "13cm";
                        else if (trx.DownlinkFreq.StartsWith("23")) TrxBand = "13cm";
                        else if (trx.DownlinkFreq.StartsWith("22")) TrxBand = "S-Band";
                        else if (trx.DownlinkFreq.StartsWith("4")) TrxBand = "70cm";
                        else if (trx.DownlinkFreq.StartsWith("12")) TrxBand = "23cm";
                        else if (trx.DownlinkFreq.StartsWith("13")) TrxBand = "Weather";
                        else if (trx.DownlinkFreq.StartsWith("29")) TrxBand = "10m";
                        else TrxBand = "other";
                    }
                    else TrxBand = "other";
                    if (TrxBand == "10m") trx.DownlinkMode = "USB";

                    if ((Descr.IndexOf("Telemetry") != -1) | (Descr.IndexOf("TLM") != -1))
                        sat.Telemetry = true;

                    trx.HasTone = false;
                    trx.Tone = "67.0";

                    if (trx.DownlinkFreq != null) trx.HasDownlink = true;

                    if (trx.DownlinkEnd == null) trx.DownlinkEnd = trx.DownlinkStart;
                    if (trx.UplinkEnd == null) trx.UplinkEnd = trx.UplinkStart;
                    if (sat.Channel == null)
                        sat.Channel = new List<_Transmitter>();
                    if (sat.Bands != null)
                    {
                        if (sat.Bands.IndexOf(TrxBand) == -1)
                            sat.Bands = sat.Bands + " " + TrxBand;
                    }
                    else sat.Bands = "Unknown";
                    sat.Channel.Add(trx);
                    if (trx.IsTransponder) sat.IsTransponder = true;
                }
            }
            reader.Close();
        }
        public class NoradMappingLine
        {
            public String SatNOGsID { get; set; }
            public String TrackingID { get; set; }
            public NoradMappingLine(String sID, String nID)
            {
                SatNOGsID = sID;
                TrackingID = nID;
            }
        }
        public List<NoradMappingLine> NoradMappingList;
        public String ReplaceTempID(string ID)
        {
            if (NoradMappingList==null)
            {
                LoadNoradMappingList();
            }
            foreach (NoradMappingLine n in NoradMappingList)
            {
                if (ID == n.SatNOGsID)
                {
                    return n.TrackingID;
                }
            }
            return ID;
        }

        private void LoadNoradMappingList()
        {
            if (File.Exists(NoradMappingFilePath))
            {
                //Load file
                StreamReader reader = File.OpenText(NoradMappingFilePath);
                try
                {
                    NoradMappingList = (List<NoradMappingLine>)Newtonsoft.Json.JsonConvert.DeserializeObject<List<NoradMappingLine>>(reader.ReadLine());
                }
                catch (Exception e)
                {
                    Console.WriteLine(">>>LoadNoradMappingList>>: Failed to load:{0}", e.Message);
                    return;
                }
            }
            else
            {
                //Create Initial mappings as of 1/21/2019
                NoradMappingList = new List<NoradMappingLine>();
                NoradMappingList.Add(new NoradMappingLine("99911", "43770")); //FOX-1C 
                NoradMappingList.Add(new NoradMappingLine("99912", "43792")); //ESEO
                NoradMappingList.Add(new NoradMappingLine("99913", "43761")); //K2SAT
                NoradMappingList.Add(new NoradMappingLine("99914", "43770")); //ExseedSat 1 (VO-96)
                NoradMappingList.Add(new NoradMappingLine("99915", "43789")); //IRVINE 02
                NoradMappingList.Add(new NoradMappingLine("99916", "43798")); //Astrocast 0.1
                NoradMappingList.Add(new NoradMappingLine("99917", "43763")); //Centauri-1
                NoradMappingList.Add(new NoradMappingLine("99918", "43793")); //CSIM-FD
                NoradMappingList.Add(new NoradMappingLine("99919", "43786")); //ITASAT-1
                NoradMappingList.Add(new NoradMappingLine("99922", "43807")); //MinXSS-2
                NoradMappingList.Add(new NoradMappingLine("99924", "43798")); //RANGE-A
                NoradMappingList.Add(new NoradMappingLine("99925", "43773")); //RANGE-B
                NoradMappingList.Add(new NoradMappingLine("99927", "43786")); //SNUGLITE
                NoradMappingList.Add(new NoradMappingLine("99928", "43782")); //SNUSAT-2
                NoradMappingList.Add(new NoradMappingLine("99929", "43804")); //SUOMI100
                NoradMappingList.Add(new NoradMappingLine("99932", "43805")); //Al-Farabi-2
                NoradMappingList.Add(new NoradMappingLine("99950", "43879")); //D-START ONE
                NoradMappingList.Add(new NoradMappingLine("99951", "43880")); //UWE-4
                NoradMappingList.Add(new NoradMappingLine("99953", "43907")); //LUME-1
                                                                              //NoradMappingList.Add(new NoradMappingLine("99962", "???")); //MEMSAT
                                                                              //NoradMappingList.Add(new NoradMappingLine("99975", "???")); //AJJSYTA AUTCube1
                                                                              //NoradMappingList.Add(new NoradMappingLine("99981", "???")); //RSP-00
                                                                              //Save List to file
                using (StreamWriter fileHandle = File.CreateText(NoradMappingFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(fileHandle, NoradMappingList);
                    fileHandle.Close();
                }

            }               
        }

        private string Dotify(string Freq, int size)
        {
            string result = "";
            if (Freq == null) return "";
            Freq = Freq.Substring(0, Math.Min(size, Freq.Length));
            if (Freq.Length == 5) result = Freq.Substring(0, 2) + "." + Freq.Substring(2, 3);
            else if (Freq.Length == 6) result = Freq.Substring(0, 3) + "." + Freq.Substring(3, 3);
            else if (Freq.Length == 9) result = Freq.Substring(0, 3) + "." + Freq.Substring(3, 3) + "." + Freq.Substring(6, 3);
            else if (Freq.Length == 8) result = Freq.Substring(0, 2) + "." + Freq.Substring(2, 3) + "." + Freq.Substring(5, 3);
            else if (Freq.Length == 10) result = Freq.Substring(0, 1) + "." + Freq.Substring(1, 3) + "." + Freq.Substring(4, 3) + "." + Freq.Substring(7, 3);
            else result = Freq;

            return result;
        }

 
        #endregion
    }
    //End of Class SatnogsTrackerPlugin

    #region Satellite and Transmitters Data Classes
    public class _Satellite : INotifyPropertyChanged
    {
        private double _CurrentEL;
        private double _CurrentAZ;
        private double _NextMaxEl;
        private double _CurrentLat;
        private double _CurrentLong;
        private double _Range;
        private DateTime _NextAOS;
        private DateTime _NextEOS;
        private TimeSpan _TimeToAOS;
        private TimeSpan _TimeToEOS;
        private _Transmitter _SelectedChannel;
        private _Transmitter _PreviouslySelectedChannel;
        private Boolean _Transponder;

        public Boolean IsTransponder
        {
            get { return _Transponder; }
            set
            {
                _Transponder = value;
                NotifyPropertyChanged("Transponder");
            }
        }

        public  override String ToString()
        {
            return Name + " " + Number;
        }

        public string Name { get; set; }
        public string NickNames { get; set; }
        public string Number { get; set; }
        public string Split { get; set; }
        public string Usage { get; set; }
        public string Callsign { get; set; }
        public string Modes { get; set; }
        public string Bands { get; set; }
        public string URL { get; set; }
        public string KepSource { get; set; }
        public Boolean HasKeps { get; set; }

        private Boolean _IsVisibile;

        public Boolean IsVisible
        {
            get { return _IsVisibile; }
            set
            {
                _IsVisibile = value;
                NotifyPropertyChanged("IsVisible");
            }
        }

        public Boolean HasNextAOS { get; set; }
        public Boolean Announce { get; set; }
        private Boolean _IsActive;
        public Boolean IsActive
        {
            get { return _IsActive; }
            set
            {
                _IsActive = value;
                NotifyPropertyChanged("IsActive");
            }
        }
        public TimeSpan TimeToAOS
        {
            get { return _TimeToAOS; }
            set
            {
                _TimeToAOS = value;
                NotifyPropertyChanged("TimeToAOS");
            }
        }
        public TimeSpan TimeToEOS
        {
            get { return _TimeToEOS; }
            set
            {
                _TimeToEOS = value;
                NotifyPropertyChanged("TimeToEOS");
            }
        }
        public DateTime NextAOS
        {
            get { return _NextAOS; }
            set
            {
                _NextAOS = value;
                NotifyPropertyChanged("NextAOS");
            }
        }
        public DateTime NextEOS
        {
            get { return _NextEOS; }
            set
            {
                _NextEOS = value;
                NotifyPropertyChanged("NextEOS");
            }
        }
        public double Range
        {
            get { return _Range; }
            set
            {
                _Range = value;
                NotifyPropertyChanged("Range");
            }
        }
        public double NextMaxEl
        {
            get { return _NextMaxEl; }
            set
            {
                _NextMaxEl = value;
                NotifyPropertyChanged("NextMaxEl");
                NotifyPropertyChanged("SZNextMaxEL");
            }
        }
        public double CurrentEL
        {
            get { return _CurrentEL; }
            set
            {
                _CurrentEL = value;
                NotifyPropertyChanged("CurrentEL");
                NotifyPropertyChanged("SCurrentEL");
            }
        }
        public string SCurrentEL
        {
            get { return "EL:" + _CurrentEL.ToString("F1"); }
        }
        public string SZNextMaxEL
        {
            get { return "Max EL:" + _NextMaxEl.ToString("F1"); }
        }
        public double CurrentAZ
        {
            get { return _CurrentAZ; }
            set
            {
                _CurrentAZ = value;
                NotifyPropertyChanged("CurrentAZ");
                NotifyPropertyChanged("SCurrentAZ");
            }
        }
        public string SCurrentAZ
        {
            get { return "AZ:" + _CurrentAZ.ToString("F1"); }
        }
        public string KepsLine1 { get; set; }
        public string KepsLine2 { get; set; }
        public Tle Tle1 { get; set; }
        public Satellite satSDP4 { get; set; }// = new Satellite(tle2);
        public List<_Transmitter> Channel { get; set; }
        public _Transmitter SelectedChannel
        {
            get { return _SelectedChannel; }
            set
            {
                _PreviouslySelectedChannel = _SelectedChannel;
                _SelectedChannel = value;
                NotifyPropertyChanged("SelectedChannel");
            }
        }
        public _Transmitter PreviouslySelectedChannel
        {
            get { return _PreviouslySelectedChannel; }
            set
            {
                _PreviouslySelectedChannel = value;
                NotifyPropertyChanged("PreviouslySelectedChannel");
            }
        }
        public int nChannels { get; set; }
        public int Radius { get; internal set; }
        public double CurrentLat
        {
            get { return _CurrentLat; }
            set
            {
                _CurrentLat = value;
                NotifyPropertyChanged("CurrentLat");
            }
        }
        public double CurrentLong
        {
            get { return _CurrentLong; }
            set
            {
                _CurrentLong = value;
                NotifyPropertyChanged("CurrentLong");
            }
        }
        private double _CurrentX;

        public double CurrentX
        {
            get { return _CurrentX; }
            set { _CurrentX = value; NotifyPropertyChanged("CurrentX"); }
        }

        private double _CurrentY;

        public double CurrentY
        {
            get { return _CurrentY; }
            set { _CurrentY = value; NotifyPropertyChanged("CurrentY"); }
        }

        public double Footprint { get; internal set; }
        public bool Telemetry { get; internal set; }
        public bool IsDecay { get; internal set; }
        public string ReportingID { get; internal set; }
        public Boolean InSpectrum(float MinFreq, float MaxFreq)
        {
            if ((Channel == null)||(CurrentEL<=0)||!IsActive||IsDecay) return false;
            foreach (var t in Channel)
            {
                if (t.InSpectrum(MinFreq, MaxFreq)) return true;
            }
            return false;

        }
        public string ID_Mixed
        {
            get
            {
                if (Number.ToString() != ReportingID)
                    return Number.ToString() + "/" + ReportingID;
                else return Number.ToString();
            }
        }

        public DateTime NextTCA { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    //End of Class _Satellite
    public class JsonSatellite
    {
        // [{"norad_cat_id":965,"name":"TRANSIT 5B-5","names":"OPS 6582",
        //"image":"https://db.satnogs.org/media/satellites/transit-o__1.jpg","status":"alive"}
        public int norad_cat_id { get; set; }
        public String name { get; set; }
        public String names { get; set; }
        public String image { get; set; }
        public String status { get; set; }
    }
    //End of Class JsonSatellite
    public class _Transmitter : INotifyPropertyChanged
    {
        private double _dDownlinkFreq;
        private double _dUplinkFreq;
        private string _DownlinkFreq;
        private string _DownlinkFreqWithDoppler;
        private string _UplinkFreq;
        private string _UplinkFreqWithDoppler;
        private string _UplinkStart;
        private string _UplinkEnd;
        private string _DownlinkStart;
        private string _DownlinkEnd;
        public string ID { get; set; }                   //Satellite Name
        public string SpaceID { get; set; }              //Space ID from KEPS
        public String ModemPort { get; set; }
        public Boolean InSpectrum(float MinFreq, float MaxFreq)
        {
            if (DownlinkFreqWithDoppler == null) return false;
            if (this.CurrentEL < 0.5 ) return false;
            if ((float.Parse(DownlinkFreqWithDoppler) >= MinFreq) && (float.Parse(DownlinkFreqWithDoppler) <= MaxFreq))
                return true;
            else return false;
        }
        public string DownlinkStart   //Start Downlink Frequency
        {
            get { return _DownlinkStart; }
            set { _DownlinkStart = value; }
        }
        public _Transmitter()
        {
        }

        public _Transmitter(_Transmitter other)
        {
            ID = other.ID;
            SpaceID = other.SpaceID;
            IsTransponder = other.IsTransponder;
            DownlinkEnd = other.DownlinkEnd;
            DownlinkStart = other.DownlinkStart;
            DownlinkFreq = other.DownlinkFreq;
            DownlinkMode = other.DownlinkMode;
            HasDownlink = other.HasDownlink;
            HasUplink = other.HasUplink;
            HasDownLinkDoppler = other.HasDownLinkDoppler;
            HasUpLinkDoppler = other.HasUpLinkDoppler;
            UplinkStart = other.UplinkStart;
            UplinkEnd = other.UplinkEnd;
            UplinkFreq = other.UplinkFreq;
            UplinkMode = other.UplinkMode;
            IsBeacon = other.IsBeacon;
            IsSplit = other.IsSplit;
            FreqLine = other.FreqLine;
            IsActive = other.IsActive;
            Tone = other.Tone;
            Modes = other.Modes;
            HasTone = other.HasTone;
            Follow = other.Follow;
            IsDirect = other.IsDirect;
        }
        private Boolean _Transponder;

        public Boolean IsTransponder
        {
            get { return _Transponder; }
            set { _Transponder = value; }
        }
       
        public string DownlinkEnd  //End of Downlink Frequency
        {
            get { return _DownlinkEnd; }
            set { _DownlinkEnd = value; }
        }
        public string UplinkStart       //Start Uplink Fequency
        {
            get { return _UplinkStart; }
            set { _UplinkStart = value; }
        }
        public string UplinkEnd     //End Uplink Frequency
        {
            get { return _UplinkEnd; }
            set { _UplinkEnd = value; }
        }
        public Boolean IsBeacon { get; set; }            //Is this a Beacon frequency
        public Boolean IsSplit { get; set; }
        public double dDownlinkFreq
        {
            get { return _dDownlinkFreq; }
            set { _dDownlinkFreq = value; }
        }
        public double dUplinkFreq
        {
            get { return _dUplinkFreq; }
            set { _dUplinkFreq = value; }
        }      
        public string DownlinkFreq                      //Downlink Frequency to Start with
        {
            get { return _DownlinkFreq; }
            set
            {
                _DownlinkFreq = value;
                _dDownlinkFreq = Convert.ToDouble(value);
                _DownlinkFreqWithDoppler = value;
                NotifyPropertyChanged("DownlinkFreq");
            }
        }
        public string DownlinkFreqWithDoppler //Downlink Freq with Doppler;
        {
            get { return _DownlinkFreqWithDoppler; }
            set
            {
                _DownlinkFreqWithDoppler = value;
                NotifyPropertyChanged("DownlinkFreqWithDoppler");
            }
        }
        public string DownlinkMode { get; set; }         //Downlink Mode 
        public Boolean HasDownlink { get; set; }         //Boolean to Indicate it HasDownlink
        public string UplinkFreq
        {
            get { return _UplinkFreq; }
            set
            {
                _UplinkFreq = value;
                _UplinkFreqWithDoppler = value;
                NotifyPropertyChanged("UplinkFreq");
            }
        }           //Uplink Frequency to Start with
        public string UplinkFreqWithDoppler  //Uplink Frequency with Doppler
        {
            get { return _UplinkFreqWithDoppler; }
            set
            {
                _UplinkFreqWithDoppler = value;
                NotifyPropertyChanged("UplinkFreqWithDoppler");
            }
        }
        public string UplinkMode { get; set; }           //Uplink Frequency Mode
        public Boolean HasUplink { get; set; }           //Boolean to indicate it has Uplink
        public string ActiveState { get; set; }          //Is this Frequency Active According to file
        public string FreqLine { get; set; }             //Store the file line that was used to capture this record
        public Boolean IsActive { get; set; }            //Is this active
        public string Tone { get; set; }                 //Uplink Tone Frequency
        public string Modes { get; set; }                //Modes String
        public Boolean HasDownLinkDoppler { get; set; }
        public Boolean HasUpLinkDoppler { get; set; }
        public Boolean HasTone { get; set; }
        public Boolean Follow { get; set; }              //Follow this channel
        public bool IsDirect { get; internal set; }
        public double dSplitFreq { get; internal set; }
        public int DownLinkCorrection { get; internal set; }
        public int BandwidthHz { get; internal set; }
        public bool DownLinkChanged { get; internal set; }
        public double CurrentEL { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    //End of Class _Transmitter
    public class JsonTransmitter
    {
        public String uuid { get; set; }
        public String description { get; set; }
        public Boolean alive { get; set; }
        public String uplink_low { get; set; }
        public String uplink_high { get; set; }
        public String downlink_low { get; set; }
        public String downlink_high { get; set; }
        public String mode_id { get; set; }
        public Boolean invert { get; set; }
        //public int MyProperty { get; set; }
        public String baud { get; set; }
        public int norad_cat_id { get; set; }
    }
    //End of Class JsonTransmitter

    public class HamSite 
    {
        public HamSite(String inCallSign, String inLatitude, String inLongitude, String inAltitude, String inDDEApp)
            //double.parse(Latitude), double.parse(Latitude),Callsing))
        {
            Callsign = inCallSign;
            Latitude = inLatitude;
            Longitude = inLongitude;
            Altitude = inAltitude;
            DDEApp = inDDEApp;
            GRIP = "127.0.0.1";

            //_homeSite = new Site(double.Parse(Latitude), double.Parse(Longitude), double.Parse(Altitude), Callsign);          
        }
        public String Callsign { get; set; }
        public String Latitude { get; set; }
        public String Longitude { get; set; }
        public String Altitude { get; set; }
        public String DDEApp { get; set; }
        public string GRIP { get; set; }
        /*
* public Site HomeSite
{
get { return _homeSite; }
}
*/
    }

    //List<SpectrumColumn> ActiveFrequencies = new List<SpectrumColumn>();
    public class SpectrumColumn
    {
        public double CurrentFrequency { get; set; }
        public String SatName { get; set; }
        public String FreqLine { get; set; }

    }
    #endregion

    public enum RecordingMode
    {
        Baseband,
        Audio
    }

    public unsafe class SimpleStreamer : IDisposable
    {
        private const int DefaultAudioGain = 30;
        private const long MaxStreamLength = int.MaxValue;
        private static readonly int _bufferCount = Utils.GetIntSetting("RecordingBufferCount", 8);
        private readonly float _audioGain = (float)Math.Pow(DefaultAudioGain / 10.0, 10);

        private readonly SharpEvent _bufferEvent = new SharpEvent(false);

        private readonly UnsafeBuffer[] _circularBuffers = new UnsafeBuffer[_bufferCount];
        private readonly Complex*[] _complexCircularBufferPtrs = new Complex*[_bufferCount];
        private readonly float*[] _floatCircularBufferPtrs = new float*[_bufferCount];

        private int _circularBufferTail;
        private int _circularBufferHead;
        private int _circularBufferLength;
        private volatile int _circularBufferUsedCount;

        private long _skippedBuffersCount;
        private bool _diskWriterRunning;
        private string _fileName;
        private double _sampleRate;

        private WavSampleFormat _wavSampleFormat;
        private SimpleWavWriter _wavWriter;
        private Thread _diskWriter;

        private readonly RecordingMode _recordingMode;
        //private readonly RecordingIQObserver _iQObserver;
        private readonly RecordingAudioProcessor _audioProcessor;
        private byte[] msgBuffer = new byte[40000];
        private byte[] _outputBuffer = null;
        private BinaryWriter _outputStream;
        private long _length;
        private Boolean _isStreamFull;
        public bool IsStreaming
        {
            get { return _diskWriterRunning; }
        }

        public bool IsStreamFull
        {
            get { return _wavWriter == null ? false : _wavWriter.IsStreamFull; }
        }

        public long BytesWritten
        {
            get { return _wavWriter == null ? 0L : _wavWriter.Length; }
        }

        public long SkippedBuffers
        {
            get { return _wavWriter == null ? 0L : _skippedBuffersCount; }
        }

        public RecordingMode Mode
        {
            get { return _recordingMode; }
        }

        public WavSampleFormat Format
        {
            get { return _wavSampleFormat; }
            set
            {
                if (_diskWriterRunning)
                {
                    throw new ArgumentException("Format cannot be set while recording");
                }
                _wavSampleFormat = value;
            }
        }

        public double SampleRate
        {
            get { return _sampleRate; }
            set
            {
                if (_diskWriterRunning)
                {
                    throw new ArgumentException("SampleRate cannot be set while recording");
                }

                _sampleRate = value;
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (_diskWriterRunning)
                {
                    throw new ArgumentException("FileName cannot be set while recording");
                }
                _fileName = value;
            }
        }


    #region Initialization and Termination
    private UdpClient _udpClient;
        private IPEndPoint _udpEP;
        private int _port;

        public SimpleStreamer(RecordingAudioProcessor audioProcessor,String Host, int Port)
        {
            _audioProcessor = audioProcessor;
            _recordingMode = RecordingMode.Audio;
            _udpClient = new UdpClient();
            _port = Port;
            _udpEP = new IPEndPoint(IPAddress.Parse(Host), Port); // endpoint where server is listening
            //_udpClient.Connect(_udpEP);
        }

        ~SimpleStreamer()
        {
            Dispose();
        }

        public void Dispose()
        {
            FreeBuffers();
        }

        #endregion

        #region IQ Event Handler

        public void IQSamplesIn(Complex* buffer, int length)
        {

            #region Buffers

            if (_circularBufferLength != length)
            {
                FreeBuffers();
                CreateBuffers(length);

                _circularBufferTail = 0;
                _circularBufferHead = 0;
            }

            #endregion

            if (_circularBufferUsedCount == _bufferCount)
            {
                _skippedBuffersCount++;
                return;
            }

            Utils.Memcpy(_complexCircularBufferPtrs[_circularBufferHead], buffer, length * sizeof(Complex));
            _circularBufferHead++;
            _circularBufferHead &= (_bufferCount - 1);
            _circularBufferUsedCount++;
            _bufferEvent.Set();
        }

        #endregion

        #region Audio Event / Scaling

        public void AudioSamplesIn(float* audio, int length)
        {
            #region Buffers

            var sampleCount = length / 2;
            if (_circularBufferLength != sampleCount)
            {
                FreeBuffers();
                CreateBuffers(sampleCount);

                _circularBufferTail = 0;
                _circularBufferHead = 0;
            }

            #endregion

            if (_circularBufferUsedCount == _bufferCount)
            {
                _skippedBuffersCount++;
                return;
            }

            Utils.Memcpy(_floatCircularBufferPtrs[_circularBufferHead], audio, length * sizeof(float));
            _circularBufferHead++;
            _circularBufferHead &= (_bufferCount - 1);
            _circularBufferUsedCount++;
            _bufferEvent.Set();
        }

        public void ScaleAudio(float* audio, int length)
        {
            float Gain = _audioGain/200;
            for (var i = 0; i < length; i++)
            {
                audio[i] *= Gain; //_audioGain;
            }
        }

        #endregion

        #region Worker Thread
        public AcmStream resampleStream;
        private void DiskWriterThread()
        {
            if (_recordingMode == RecordingMode.Audio)
            {
                _audioProcessor.AudioReady += AudioSamplesIn;
                _audioProcessor.Enabled = true;
            }
            int input_rate = (int)_sampleRate;
            resampleStream = new AcmStream(new WaveFormat(input_rate, 16, 1), new WaveFormat(48000, 16, 1));
            
            while (_diskWriterRunning)// && !_wavWriter.IsStreamFull)
            {
                if (_circularBufferTail == _circularBufferHead)
                {
                    _bufferEvent.WaitOne();
                }

                if (_diskWriterRunning && _circularBufferTail != _circularBufferHead)
                {
                    if (_recordingMode == RecordingMode.Audio)
                    {
                       ScaleAudio(_floatCircularBufferPtrs[_circularBufferTail], _circularBuffers[_circularBufferTail].Length * 2);
                    }
                       
                     Write(_floatCircularBufferPtrs[_circularBufferTail], _circularBuffers[_circularBufferTail].Length * 2); //*2 ??? 

                    _circularBufferUsedCount--;
                    _circularBufferTail++;
                    _circularBufferTail &= (_bufferCount - 1);
                }
            }

            while (_circularBufferTail != _circularBufferHead)
            {
                if (_floatCircularBufferPtrs[_circularBufferTail] != null)
                {
                    Write(_floatCircularBufferPtrs[_circularBufferTail], _circularBuffers[_circularBufferTail].Length*2);
                }
                _circularBufferTail++;
                _circularBufferTail &= (_bufferCount - 1);
            }

            if (_recordingMode == RecordingMode.Audio)
            { 
                _audioProcessor.Enabled = false;
                _audioProcessor.AudioReady -= AudioSamplesIn;
            }

            _diskWriterRunning = false;
        }

        public void Write(float* data, int length)
        {

            if (_udpClient != null)
            {
                switch (_wavSampleFormat)
                {
                    case WavSampleFormat.PCM8:
                        WritePCM8(data, length);
                        break;

                    case WavSampleFormat.PCM16:
                        WritePCM16(data, length);
                        break;

                    case WavSampleFormat.Float32:
                        WriteFloat(data, length);
                        break;
                }

                return;
            }

            throw new InvalidOperationException("Stream not open");
        }
        private void WritePCM8(float* data, int length)
        {

            #region Buffer

            if (_outputBuffer == null || _outputBuffer.Length != length)
            {
                _outputBuffer = null;
                _outputBuffer = new byte[length];// * 2];
            }

            #endregion

            var ptr = data;

            for (var i = 0; i < length; i++)
            {
                _outputBuffer[i]=(byte)((*ptr++ * 127.0f) + 128);
                ptr++;
            }

            WriteStream(_outputBuffer);
        }

        private void WritePCM16(float* data, int length)
        {
            if (_outputBuffer == null || _outputBuffer.Length != (length * sizeof(Int16)))
            {
                _outputBuffer = null;
                _outputBuffer = new byte[length * sizeof(Int16)];
            }
            var ptr = data;           
            for (var i = 0; i < length; i++)
            {
                var leftChannel = (Int16)(*ptr++ * 32767.0f);
                var rightChannel = (Int16)(*ptr++ * 32767.0f);
                _outputBuffer[(i * 2)] = (byte)(leftChannel & 0x00ff);
                _outputBuffer[(i * 2) + 1] = (byte)(leftChannel >> 8);
            }
            int buffer_length = length * sizeof(Int16);
            System.Buffer.BlockCopy(_outputBuffer,0,resampleStream.SourceBuffer,0,buffer_length);
            int sourceBytesConverted = 0;
            
            var convertedBytes = resampleStream.Convert(buffer_length, out sourceBytesConverted);
            if (sourceBytesConverted != buffer_length)
            {
                Console.WriteLine("We didn't convert everything {0} bytes in, {1} bytes converted");
            }
            var converted = new byte[convertedBytes];
            System.Buffer.BlockCopy(resampleStream.DestBuffer, 0, converted, 0, convertedBytes);
            int counter = 0;
            const int MAX_PAYLOAD = 1472;
            byte[] packet = new byte[MAX_PAYLOAD];
            Boolean AllZero=true;
            for (var i=0; i< convertedBytes; i++)
            {                
                if (counter == MAX_PAYLOAD)
                {
                    if (!AllZero)
                        _udpClient.Send(packet, counter, _udpEP);
                    //Thread.Sleep(50);
                    //packet = null;
                    packet = new byte[MAX_PAYLOAD];
                    counter = 0;
                    AllZero = true;
                }
                packet[counter] = converted[i];
                if (packet[counter] != 0) AllZero = false;
                counter++;
            }
            if (counter > 1)
            {
                if (!AllZero)
                    _udpClient.Send(packet, counter-1, _udpEP);
            }
        }

        private void WriteFloat(float* data, int length)
        {

            #region Buffer

            if (_outputBuffer == null || _outputBuffer.Length != (length * sizeof(float) * 2))
            {
                _outputBuffer = null;
                _outputBuffer = new byte[length * sizeof(float) * 2];
            }

            #endregion

            Marshal.Copy((IntPtr)data, _outputBuffer, 0, _outputBuffer.Length);

            WriteStream(_outputBuffer);
        }

        private void WriteStream(byte[] data)
        {
            _udpClient.Send(data, data.Length, _udpEP);
            /*
            if (_outputStream != null)
            {
                var toWrite = (int)Math.Min(MaxStreamLength - _outputStream.BaseStream.Length, data.Length);

                _outputStream.Write(data, 0, toWrite);

                _length += toWrite;
                UpdateLength();

                _isStreamFull = _outputStream.BaseStream.Length >= MaxStreamLength;
            }
            */
        }
        public void InsertDataIntoCircularBuffer(byte[] data)
        {

        }
        public void SendingPackets()
        {

        }
        private void UpdateLength()
        {
            if (_outputStream != null)
            {
                /*
                _outputStream.Seek((int)_fileSizeOffs, SeekOrigin.Begin);
                _outputStream.Write((UInt32)(_outputStream.BaseStream.Length - 8));
                _outputStream.Seek((int)_dataSizeOffs, SeekOrigin.Begin);
                _outputStream.Write((UInt32)(_length));
                _outputStream.BaseStream.Seek(0, SeekOrigin.End);
                */
            }
        }
        private void Flush()
        {
            if (_wavWriter != null)
            {
                //_wavWriter.Close();
            }
        }

        private void CreateBuffers(int size)
        {
            for (var i = 0; i < _bufferCount; i++)
            {
                _circularBuffers[i] = UnsafeBuffer.Create(size, sizeof(Complex));
                _complexCircularBufferPtrs[i] = (Complex*)_circularBuffers[i];
                _floatCircularBufferPtrs[i] = (float*)_circularBuffers[i];
            }

            _circularBufferLength = size;
        }

        private void FreeBuffers()
        {
            _circularBufferLength = 0;
            for (var i = 0; i < _bufferCount; i++)
            {
                if (_circularBuffers[i] != null)
                {
                    _circularBuffers[i].Dispose();
                    _circularBuffers[i] = null;
                    _complexCircularBufferPtrs[i] = null;
                    _floatCircularBufferPtrs[i] = null;
                }
            }
        }

        #endregion

        #region Public Methods

        public void StartStreaming()
        {
            if (_diskWriter == null)
            {
                _circularBufferHead = 0;
                _circularBufferTail = 0;

                _skippedBuffersCount = 0;

                _bufferEvent.Reset();

                //_wavWriter = new SimpleWavWriter(_fileName, _wavSampleFormat, (uint)_sampleRate);
                //_wavWriter.Open();

                _diskWriter = new Thread(DiskWriterThread);

                _diskWriterRunning = true;
                _diskWriter.Start();
            }
        }

        public void StopStreaming()
        {
            _diskWriterRunning = false;

            if (_diskWriter != null)
            {
                _bufferEvent.Set();
                _diskWriter.Join();
            }

            Flush();
            FreeBuffers();

            _diskWriter = null;
            _wavWriter = null;
        }

        internal void UpdateIP(string NewIP)
        {
            _udpEP = new IPEndPoint(IPAddress.Parse(NewIP), _port);
        }

        #endregion
    }
}
