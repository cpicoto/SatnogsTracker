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
using NAudio.Wave;
using SDRSharp.WavRecorder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDRSharp.Common;
using NDde;
using System.Threading;
using SDRSharp.Radio;
using System.Net;
using System.ComponentModel;
using Zeptomoby.OrbitTools;
using SDRSharp.PanView;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NAudio.Wave.Compression;

namespace SDRSharp.SatnogsTracker
{
    public partial class SatnogsTrackerPlugin : ISharpPlugin
    {
        private readonly RecordingIQProcessor _iqObserver = new RecordingIQProcessor();
        private readonly RecordingAudioProcessor _audioProcessor = new RecordingAudioProcessor();
        private readonly RecordingAudioProcessor _AFProcessor = new RecordingAudioProcessor();
        private readonly RecordingAudioProcessor _UDPaudioProcessor = new RecordingAudioProcessor();
        private SimpleRecorder _audioRecorder;
        private SatnogsWavRecorder _AFRecorder;
        private SimpleStreamer _UDPaudioStreamer;
        private SimpleRecorder _basebandRecorder;
        private readonly WavSampleFormat _wavSampleFormat = WavSampleFormat.PCM16;

        private void PrepareAFRecorder()
        {
            DateTime startTime = DateTime.UtcNow;
            String AudioRecordingName = "";
            //_audioProcessor.SampleRate = 48000;
            _audioRecorder.SampleRate = _audioProcessor.SampleRate;
            if ((SatelliteName == null) || (SatelliteID == null))
            {
                AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH:mm:ss.ffffff") + "_CURRENT_FREQ__AF.wav";
            }
            else
                AudioRecordingName = startTime.ToString(@"yyyy-MM-dd HH:mm:ss.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_AF.wav";
            _audioRecorder.FileName = RecordingLocation() + "\\" + AudioRecordingName;
            _audioRecorder.Format = _wavSampleFormat;
        }

        private void StopAFRecorder()
        {
            if (_audioRecorder.IsRecording) _audioRecorder.StopRecording();
        }

        private void PrepareBaseRecorder()
        {
            String BaseRecordingName;
            DateTime startTime = DateTime.UtcNow;
            _basebandRecorder.SampleRate = _iqObserver.SampleRate;
            if ((SatelliteName == null) || (SatelliteID == null))
            {
                BaseRecordingName = startTime.ToString(@"yyyy-MM-ddTHH:mm:ss.ffffff") + "_CURRENT_FREQ__IQ.wav";
            }
            else
                BaseRecordingName = startTime.ToString(@"yyyy-MM-ddTHH:mm:ss.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_IQ.wav";

            _basebandRecorder.FileName = RecordingLocation() + "\\" + BaseRecordingName;
            _basebandRecorder.Format = _wavSampleFormat;
        }

        private void StopBaseRecorder()
        {
            if (_basebandRecorder.IsRecording) _basebandRecorder.StopRecording();
        }
    }
    class SatnogsWavRecorder
    {
        public WaveIn TrxwaveSource = null;
        public WaveFileWriter TrxwaveFile = null;
        public String TrxRecordingFile;
        private RecordingAudioProcessor _audioProcessor;
        private RecordingMode _recordingMode;

        public SatnogsWavRecorder(RecordingAudioProcessor audioProcessor)
        {
            _audioProcessor = audioProcessor;
            _recordingMode = RecordingMode.Audio;
        }

        private void StopWAVRecording()
        {
            Console.WriteLine(">>>Satellite.StopWAVRecording>>> into:{0}", TrxRecordingFile);
            if (TrxwaveSource != null) TrxwaveSource.StopRecording();
        }

        private void StartWAVRecording()
        {
            /*
            string roamingPath = Environment.GetFolderPath(Environment.Spe);
            string fullPath = roaming
            //string recPath = $"{fullPath}\\recordings";
            string recPath = "MainWindow.mySettingsViewModel.MyRecordingLocation";
            if (!Directory.Exists(recPath)) System.IO.Directory.CreateDirectory(recPath);
            DateTime now = DateTime.UtcNow;
            String date_path = now.ToString(@"yyyy-MM-dd_HH-mm");
            String Name;
            if (MainWindow.CurrentlySelectedSatellite == null)
                Name = "Empty";
            else
                Name = MainWindow.CurrentlySelectedSatellite.Name;
            TrxRecordingFile = recPath + "\\" + date_path + "_" + MainWindow.mySettingsViewModel._Settings.CallSign + "_" + Name + ".wav";
            //"_"+ CurrentlySelectedSatellite.SelectedChannel.FreqLine+".wav";
            int DuplicateCounter = 1;
            while (File.Exists(TrxRecordingFile) && (DuplicateCounter < 20))
            {
                TrxRecordingFile = recPath + "\\" + date_path + "_" + MainWindow.mySettingsViewModel._Settings.CallSign + "_" + Name + "_" + DuplicateCounter.ToString() + ".wav";
                DuplicateCounter++;
            }
            if (DuplicateCounter == 20)
            {
                Console.WriteLine(" >>>Satellite.StartWAVRecording>>> Too many duplicates abandon recording");
                return;
            }
            Console.WriteLine(">>>Satellite.StartWAVRecording>>> into:{0}", TrxRecordingFile);

          
            TrxwaveSource = new WaveIn();
            TrxwaveSource.DeviceNumber = mySettingsViewModel._Settings.Radio1.AudioIn;
            TrxwaveSource.WaveFormat = new WaveFormat(44100, 1);

            TrxwaveSource.DataAvailable += new EventHandler<WaveInEventArgs>(TrxwaveSource_DataAvailable);

            TrxwaveSource.RecordingStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(TrxwaveSource_RecordingStopped);

            //RecordingFile=
            TrxwaveFile = new WaveFileWriter(TrxRecordingFile, TrxwaveSource.WaveFormat);
            */
            TrxwaveSource.StartRecording();
 
        }

        void TrxwaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (TrxwaveFile != null)
            {
                TrxwaveFile.Write(e.Buffer, 0, e.BytesRecorded);
                TrxwaveFile.Flush();
            }
        }

        void TrxwaveSource_RecordingStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            if (TrxwaveSource != null)
            {
                TrxwaveSource.Dispose();
                TrxwaveSource = null;
            }

            if (TrxwaveFile != null)
            {
                TrxwaveFile.Dispose();
                TrxwaveFile = null;
            }
        }
    }
}
