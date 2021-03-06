﻿/* 
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
using Newtonsoft.Json;
using SDRSharp.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using Zeptomoby.OrbitTools;

namespace SDRSharp.SatnogsTracker
{
    public partial class SatnogsTrackerPlugin : ISharpPlugin
    {
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
                        if (_AFRecorder.IsRecording) StopAFRecorder();
                        if (_UDPaudioStreamer.IsStreaming) StopUDPStreamer();
                    }
                }
            }
        }
        #endregion

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

        public void ListSatellitesfromCollection()
        {
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
                        }

                    LogFile.WriteLine();
                }

            }
            //Console.WriteLine();
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
            if (NoradMappingList == null)
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

        public override String ToString()
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
            if ((Channel == null) || (CurrentEL <= 0) || !IsActive || IsDecay) return false;
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
            if (this.CurrentEL < 0.5) return false;
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
        {
            Callsign = inCallSign;
            Latitude = inLatitude;
            Longitude = inLongitude;
            Altitude = inAltitude;
            DDEApp = inDDEApp;
            GRIP = "127.0.0.1";
        }
        public String Callsign { get; set; }
        public String Latitude { get; set; }
        public String Longitude { get; set; }
        public String Altitude { get; set; }
        public String DDEApp { get; set; }
        public string GRIP { get; set; }
    }
}
