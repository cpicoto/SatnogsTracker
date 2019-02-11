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
using SDRSharp.Radio;
using SDRSharp.WavRecorder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDRSharp.Common;
using NDde;
using System.ComponentModel;
using Zeptomoby.OrbitTools;
using SDRSharp.PanView;
using System.Drawing;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Compression;

namespace SDRSharp.SatnogsTracker
{
    public partial class SatnogsTrackerPlugin : ISharpPlugin
    {
        private void SDRSharp_StreamerChanged(Boolean StreamAF)
        {
            if (StreamAF && !_UDPaudioStreamer.IsStreaming)
            {
                PrepareUDPStreamer();
                _UDPaudioStreamer.StartStreaming();
            }
            if (!StreamAF && _UDPaudioStreamer.IsStreaming)
            {
                _UDPaudioStreamer.StopStreaming();
            }
        }
        private void PrepareUDPStreamer()
        {
            DateTime startTime = DateTime.UtcNow;
            _UDPaudioStreamer.SampleRate = _audioProcessor.SampleRate;
            String AudioRecordingName = startTime.ToString(@"yyyy-MM-ddTHH-mm.ffffff") + "_" + SatelliteName + "_" + SatelliteID + "_STREAM.wav";
            //_UDPaudioStreamer.FileName = RecordingLocation() + "\\" + AudioRecordingName;
            _UDPaudioStreamer.Format = _wavSampleFormat;
        }
        private void StopUDPStreamer()
        {
            if (_UDPaudioStreamer.IsStreaming) _UDPaudioStreamer.StopStreaming();
        }
    }

    public enum RecordingMode
    {
        Baseband,
        Audio
    }

    public unsafe class SimpleStreamer : IDisposable
    {
        private const int DefaultAudioGain = 19;
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

        public SimpleStreamer(RecordingAudioProcessor audioProcessor, String Host, int Port)
        {
            _audioProcessor = audioProcessor;
            _recordingMode = RecordingMode.Audio;
            _udpClient = new UdpClient();
            _port = Port;
            _udpEP = new IPEndPoint(IPAddress.Parse(Host), Port); 
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
            for (var i = 0; i < length; i++)
            {
                audio[i] *= _audioGain;
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
            CurrentCounter = 0;
            LastCheck = DateTime.Now;
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
                    Write(_floatCircularBufferPtrs[_circularBufferTail], _circularBuffers[_circularBufferTail].Length * 2);
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

                    case WavSampleFormat.PCM16: //This is the only supported one
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
                _outputBuffer[i] = (byte)((*ptr++ * 127.0f) + 128);
                ptr++;
            }

            WriteStream(_outputBuffer);
        }
        const int MAX_PAYLOAD = 1472;
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
                var average = (Int16)(((*ptr++ + *ptr++) / 2) * 32767.0f);
                //var leftChannel =(Int16) (*ptr++ * 32767.0f);
                //var rightChannel =(Int16) (*ptr++ * 32767.0f);
                //var average = (Int16)(((*ptr++ * 32767.0f) + (*ptr++ * 32767.0f)) / 2);
                _outputBuffer[(i * 2)] = (byte)(average & 0x00ff);
                _outputBuffer[(i * 2) + 1] = (byte)(average >> 8);
            }
            int buffer_length = length * sizeof(Int16);
            System.Buffer.BlockCopy(_outputBuffer, 0, resampleStream.SourceBuffer, 0, buffer_length);
            int sourceBytesConverted = 0;

            var convertedBytes = resampleStream.Convert(buffer_length, out sourceBytesConverted);
            if (sourceBytesConverted != buffer_length)
            {
                Console.WriteLine("We didn't convert everything {0} bytes in, {1} bytes converted");
            }
            var converted = new byte[convertedBytes];
            System.Buffer.BlockCopy(resampleStream.DestBuffer, 0, converted, 0, convertedBytes);
            int counter = 0;

            byte[] packet = new byte[MAX_PAYLOAD];
            var TxStream = new WaveFormat(48000, 16, 1);

            for (var i = 0; i < convertedBytes; i++)
            {
                if (counter == MAX_PAYLOAD)
                {

                    _udpClient.Send(packet, counter, _udpEP);
                    MarkAndWait(counter, TxStream.AverageBytesPerSecond);
                    packet = null;
                    packet = new byte[MAX_PAYLOAD];
                    counter = 0;
                }
                packet[counter] = converted[i];
                counter++;
            }
            if (counter > 0)
            {
                counter--;
                _udpClient.Send(packet, counter, _udpEP);
                MarkAndWait(counter, TxStream.AverageBytesPerSecond);
            }
        }
        private DateTime LastCheck;
        private double CurrentCounter;

        private void MarkAndWait(int counter, int average)
        {
            CurrentCounter += counter;
            DateTime Iteration = DateTime.Now;
            TimeSpan elapsed = Iteration - LastCheck;
            double averageMilli = average / 1000;

            double bytesPerMiliSec = CurrentCounter / elapsed.TotalMilliseconds;
            if (bytesPerMiliSec > averageMilli)
            {
                Boolean WaitMore = true;
                while (WaitMore)
                {
                    //_udpClient.Client.Poll(100, SelectMode.SelectError);
                    Thread.Sleep(0);
                    elapsed = DateTime.Now - LastCheck;
                    bytesPerMiliSec = CurrentCounter / elapsed.TotalMilliseconds;
                    if (bytesPerMiliSec <= averageMilli)
                        WaitMore = false;
                }
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
