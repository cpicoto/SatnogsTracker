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
using SDRSharp.PanView;
using SDRSharp.Radio;
using SDRSharp.WavRecorder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Zeptomoby.OrbitTools;

namespace SDRSharp.SatnogsTracker
{
    public partial class SatnogsTrackerPlugin : ISharpPlugin
    {
        private const string _displayName = "Satnogs Tracker";
        private Controlpanel _controlpanel;
        private ISharpControl control_;
        private SatPC32DDE satpc32Server_;
        public Boolean CalculateSatVisibilityRunning;

        private string _SatElevation;
        public Action<String> UpdateStatus;
        String MyStationFilePath = "";
        String NoradMappingFilePath = "";

        List<_Transmitter> Transmitters = new List<_Transmitter>();
        List<_Satellite> Satellites = new List<_Satellite>();

        StreamWriter LogFile;
        Site siteHome;
        HamSite siteSettings;
        Settings dlg = new Settings();

        public void Initialize(ISharpControl control)
        {
            Console.WriteLine("Initialize Plugin\r\n");
            control_ = control;

            /// Setup Audio Recording
            _AFProcessor.Enabled = false;
            _UDPaudioProcessor.Enabled = false;
            _iqObserver.Enabled = false;
            control_.RegisterStreamHook(_iqObserver, ProcessorType.RawIQ);
            control_.RegisterStreamHook(_AFProcessor, ProcessorType.FilteredAudioOutput);
            control_.RegisterStreamHook(_UDPaudioProcessor, ProcessorType.FilteredAudioOutput);
            Console.WriteLine(_AFProcessor.SampleRate);
            //_audioRecorder = new SimpleRecorder(_audioProcessor);
            _AFRecorder = new SimpleSatNogsWAVRecorder(_AFProcessor);
            _UDPaudioStreamer = new SimpleStreamer(_UDPaudioProcessor, "127.0.0.1", 7355);
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

            if (!File.Exists(MyStationFilePath))
            {
                dlg.MySite = new HamSite("Your CallSign", "0.0", "0.0", "0.0", "SatPC32");
                dlg.Show();
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
            _controlpanel.StartRecordingBaseband += SDRSharp_BasebandRecorderChanged;


            satpc32Server.SatRecordAFChanged += _controlpanel.SatPC32ServerRecordAFChanged;
            satpc32Server.SatRecordAFChanged += SDRSharp_AFRecorderChanged;

            _controlpanel.StartRecordingAF += SDRSharp_AFRecorderChanged;

            satpc32Server.SatStreamAFChanged += _controlpanel.SatPC32ServerStreamAFChanged;
            satpc32Server.SatStreamAFChanged += SDRSharp_StreamerChanged;
            _controlpanel.StartStreamingAF += SDRSharp_StreamerChanged;

            //_controlpanel.StartStreamingAF += S ;


            //_controlpanel.StartStreamingAF+= SDRSharp_StreamerChanged;

            #endregion

            UpdateTleData(false);

            Console.WriteLine("Tracking Initiated");

            //Start Background Doppler Calculations
            ThreadPool.QueueUserWorkItem(CalculateSatVisibility);


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
            StopUDPStreamer();
            satpc32Server_?.Abort();
            LogFile.Close();
        }

        public bool HasGui
        {
            get { return true; }
        }
        #endregion 
        private void SDRSharp_DownlinkFreqChanged(string Frequency)
        {
            
            if (control_.IsPlaying && control_.SourceIsTunable)
            {
                    control_.Frequency = long.Parse(Frequency);
                    control_.CenterFrequency = control_.Frequency;
            }
            else if (control_.IsPlaying)
            {
                //IQ Source used for IC-9700
                control_.Frequency = (long)11939;
            }               
        }
        private void SDRSharp_SatNameChanged(string SatName)
        {
            if (SatelliteName != SatName)
            {
                SatelliteName = SatName;
                if (_basebandRecorder.IsRecording) _basebandRecorder.StopRecording();
                if (_AFRecorder.IsRecording) _AFRecorder.StopRecording();
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
                    Console.WriteLine("Unable to Start BaseBand Recording");
                    return;
                }
            }
            if (!RecordBase && _basebandRecorder.IsRecording)
            {
                _basebandRecorder.StopRecording();
            }
        }
        private void SDRSharp_AFRecorderChanged(Boolean RecordAF)
        {
            if (RecordAF && !_AFRecorder.IsRecording)
            {


                try
                {
                    if (RecordAF)
                    {
                        PrepareAFRecorder();
                        _AFRecorder.StartRecording();
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to Start AF Recording");
                    return;
                }
            }

            if (!RecordAF && _AFRecorder.IsRecording)
            {
                _AFRecorder.StopRecording();
            }
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
                            int Height = (int)(t.CurrentEL * (Spectrum.Height - 30) / 90);
                            int MaxHeight = (int)(sat.NextMaxEl * (Spectrum.Height - 30) / 90);
                            Start = new Point((int)Spectrum.FrequencyToPoint(double.Parse(t.DownlinkFreqWithDoppler) - t.BandwidthHz / 2),
                                               Spectrum.Height - Height - 30);
                            Top = new Point((int)Spectrum.FrequencyToPoint(double.Parse(t.DownlinkFreqWithDoppler) - t.BandwidthHz / 2),
                                               Spectrum.Height - MaxHeight - 30);
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
    }
    //End of Class SatnogsTrackerPlugin
}
