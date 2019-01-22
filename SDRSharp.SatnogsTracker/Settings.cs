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
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Forms;

namespace SDRSharp.SatnogsTracker
{
    public partial class Settings : Form
    {
        public Action UpdateSite;
        private HamSite _site;
        private readonly string MyStationFilePath;
        public Settings()
        {
            InitializeComponent();
            MyStationFilePath = DataLocation() + "MyStation.json";
            if (File.Exists(MyStationFilePath))
            {
                LoadHomeSiteFromJson();
            }
        }

        private string DataLocation()
        {
            string Filefolder = Path.GetDirectoryName(Application.CommonAppDataPath);
            Filefolder += "\\SatnogsTracker";
            if (!Directory.Exists(Filefolder))
            {
                Directory.CreateDirectory(Filefolder);
            }

            return Filefolder + "\\";
        }

        public bool SaveHomeSiteToJson()
        {
            using (StreamWriter fileHandle = File.CreateText(MyStationFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fileHandle, MySite);
            }
            return true;
        }
        public bool LoadHomeSiteFromJson()
        {
            StreamReader reader = File.OpenText(MyStationFilePath);
            try
            {
                MySite = Newtonsoft.Json.JsonConvert.DeserializeObject<HamSite>(reader.ReadLine());
            }
            catch (Exception e)
            {
                Console.WriteLine(">>>LoadSatsfromJson: Failed to load:{0}", e.Message);
                return false;
            }
            reader.Close();

            return true;
        }
        public HamSite MySite
        {
            get => _site;
            set
            {
                _site = value;
                textBox1.Text = _site.Callsign;
                textBox2.Text = _site.Latitude;
                textBox3.Text = _site.Longitude;
                textBox4.Text = _site.Altitude;
                comboBox1.Text = _site.DDEApp;
                HamSiteChanged?.Invoke(_site);

            }
        }

        public Action<HamSite> HamSiteChanged;

        private void button1_Click(object sender, EventArgs e)
        {
            //Save Fields to Settings File
            _site.Callsign = textBox1.Text;
            _site.Latitude = textBox2.Text;
            _site.Longitude = textBox3.Text;
            _site.Altitude = textBox4.Text;
            _site.DDEApp = comboBox1.Text;
            HamSiteChanged?.Invoke(_site);
            SaveHomeSiteToJson();
            Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Ignore Changes
            Hide();
        }
    }
}
