
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

namespace SDRSharp.SatnogsTracker
{
    public class SatnogsTrackerPlugin : ISharpPlugin
    {
        private const string _displayName = "Satnogs Tracker";
        private Controlpanel _controlpanel;
        private ISharpControl control_;
        private SatPC32DDE satpc32Server_;



        private readonly RecordingIQProcessor _iqObserver = new RecordingIQProcessor();
        private readonly RecordingAudioProcessor _audioProcessor = new RecordingAudioProcessor();

        private  SimpleRecorder _audioRecorder;
        private  SimpleRecorder _basebandRecorder;

        private WavSampleFormat _wavSampleFormat;
        private string _SatElevation;

        public void Initialize(ISharpControl control)
        {
            Console.WriteLine("Initialize Plugin\r\n");
            control_ = control;

            _audioProcessor.Enabled = false;
            _iqObserver.Enabled = false;
            control_.RegisterStreamHook(_iqObserver, ProcessorType.RawIQ);
            control_.RegisterStreamHook(_audioProcessor, ProcessorType.DemodulatorOutput);

            _audioRecorder = new SimpleRecorder(_audioProcessor);
            _basebandRecorder = new SimpleRecorder(_iqObserver);

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
            satpc32Server.SatNameChanged += SDRSharp_SatNameChanged;

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
            satpc32Server.SatSatNogsIDChanged += SDRSharp_SatIDChanged;

            satpc32Server.SatRecordBaseChanged += _controlpanel.SatPC32ServerRecordBaseChanged;
            satpc32Server.SatRecordBaseChanged += SDRSharp_BasebandRecorderChanged;

            satpc32Server.SatRecordAFChanged += _controlpanel.SatPC32ServerRecordAFChanged;
            satpc32Server.SatRecordAFChanged += SDRSharp_AFRecorderChanged;

        }

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
            StopBaseRecorder();
            StopAFRecorder();
            satpc32Server_?.Abort();
        }

        public bool HasGui
        {
            get { return true; }
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
                    if (int.Parse(value) <= 0)
                    {
                        if (_basebandRecorder.IsRecording) StopBaseRecorder();
                        if (_audioRecorder.IsRecording) StopAFRecorder();
                    }
                }
            }
        }

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
                    }
                }
                catch
                {
                    _audioRecorder.StopRecording();
                    MessageBox.Show("Unable to Start AF Recording", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
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

        private void PrepareBaseRecorder()
        {
            DateTime startTime = DateTime.UtcNow;
            _basebandRecorder.SampleRate = _iqObserver.SampleRate;
            String BaseRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_IQ.wav";
            _basebandRecorder.FileName = RecordingLocation()+"\\" + BaseRecordingName;
            _basebandRecorder.Format = _wavSampleFormat;
        }

        private void PrepareAFRecorder()
        {
            DateTime startTime = DateTime.UtcNow;
            _audioRecorder.SampleRate = _audioProcessor.SampleRate;
            String AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_AF.wav";
            _audioRecorder.FileName = RecordingLocation() + "\\" + AudioRecordingName;
            _audioRecorder.Format = _wavSampleFormat;
        }
        private void StopBaseRecorder()
        {
            if (_basebandRecorder.IsRecording) _basebandRecorder.StopRecording();
        }

        private void StopAFRecorder()
        {
            if (_audioRecorder.IsRecording) _audioRecorder.StopRecording();
        }

    }
}
