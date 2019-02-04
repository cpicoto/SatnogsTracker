# SDRSharp.SatnogsTracker
* Plugin to connect SatPC32 and other DDE apps to SDRSharp.
	* Connects to db.satgnogs.org to obtain Satellites and Transmitter information.
	* Connects to multiple sources for TLE files
	* Displays visibile Satellite frequencies in range of the current Spectrum Bandwidth.
	* Dinamic bar will increase based on elevation for current satellite until it reaches max elevation for the path, marked by green bar.
	* Waterfall is centered on currently tuned Satellite from the external tracking application. 


## Install ##
* Download SDR Sharp from https://airspy.com/?ddownload=3130
* Add the Plugin Key to the Plugins.xml file
	* ```<add key="SatnogsTracker" value="SDRSharp.SatnogsTracker.SatnogsTrackerPlugin,SDRSharp.SatnogsTracker" />``` 
	* Copy the Binaries to the sdrsharp folder
		* SDRSharp.SatnogsTracker.dll
		* Zeptomoby.OrbitTools.Core.dll
		* Zeptomoby.OrbitTools.Orbit.dll
		* Newtonsoft.Json.dll

## Folders and Files Created ##
* DriveLetter:\ProgramData\SDRSharp\SDR#\SatnogsTracker
	* TLE Files
		* active.txt		from http://www.celestrak.com/NORAD/elements/active.txt
		* amateur.txt		from http://www.dk3wn.info/tle/amateur.txt
		* cubesat.txt		from http://www.celestrak.com/NORAD/elements/cubesat.txt
		* cubesatis0lnf.txt	from http://www.is0lnf.com/wp-content/uploads/2018/01/cubesatis0lnf.txt
		* nasa.all			from https://www.amsat.org/tle/current/nasa.all		
		* nasabare.txt		from https://www.amsat.org/tle/current/nasabare.txt
		* tle-new.txt		from http://www.celestrak.com/NORAD/elements/tle-new.txt

	* SatNOGs Data
		* Satellites.json	from https://db.satnogs.org/api/satellites/
		* Transmitters.json	from https://db.satnogs.org/api/transmitters/

	* Configuration and Logs
		* MyStation.json created on first run or anytime settings are changed, can be edited manually or from the plugin
			* ```{"Callsign":"your callsign","Latitude":"Value between -90.000 and 90.0000","Longitude":"Value Between -180.0000a and 180.0000","Altitude":" (inKms) Value between 0.000 and 0.300","DDEApp":"Value is either Orbitron or SatPC32 or WxTrack"}```
		* SatNOGsMapping.json: File with temporary mappings from SatNOG IDS 999?? to norad tracking IDs, can be edited manually.
			* ```[{"SatNOGsID":"99911","TrackingID":"43770"},{"SatNOGsID":"99912","TrackingID":"43792"}]```
		* SatnogsTracker_logfile.txt created every time SDR launchs for debug purposes

* DriveLetter:sdrharp-exe-location\SatRecordings
	* UTC DATE_Sat Name_norad ID_[AF|IQ].wav
		* Examples
			* 2019-01-03T21-50.202238_JAS-2-(FO-29)_24278_AF.wav
			* 2019-01-03T22-21.016427_JAS-2-(FO-29)_24278_AF.wav


## How to setup gr-satellites to decode telemetry from SatnogsTracker using Windows Subsystem for Linux (WSL)
* On Windows 10, go the Windows Store and download the Debian GNU/Linux for WSL
* Configure WSL following these instructions https://docs.microsoft.com/en-us/windows/wsl/install-win10
* Launch Debian after configuring WSL
	* Edit /etc/apt/sources.list
	* Insert this new line at the top ```deb http://ftp.us.debian.org/debian sid main```
	* ```
		sudo apt update
		sudo apt upgrade  --> This will take sometime
	    sudo apt install make cmake git xterm python-pip synaptic swig doxygen direwolf
		pip install construct requests
	    sudo synaptic```
	* Select gnuradio and gnuradio-dev >= 3.7.13.4 and apply changes to Install
	```
	cd ~
	git clone https://github.com/daniestevez/gr-satellites
	git clone https://github.com/daniestevez/gr-kiss
	git clone https://github.com/daniestevez/libfec
	cd libfec
	./configure
	make
	sudo make install
	cd ..
	cd gr-kiss
	mkdir build
	cd build
	cmake ..
	make
	sudo make install
	cd ../..
	cd gr-satellites
	mkdir build
	cd build
	cmake ..
	make
	sudo make install
	sudo ldconfig
	cd ..
	./compile_hierarchical.sh
	```

## Credits and References
* OrbitTools Library - Public Edition - Copyright © 2003-2017 Michael F. Henry - http://zeptomoby.com/satellites/index.htm
* Doppler calculations based on Predict formulas from KD2BD https://www.qsl.net/kd2bd/index.html
* Started from GpredictConnector by Alex Wahl, Copyright (c) 2018 Alex Wahl
* Mike Rupprecht, DK3WN and his Satblog http://www.dk3wn.info/p/
* Satnogs.Tracker can interface with Orbitron - Satellite Tracking System (C) 2001-2005 by Sebastian Stoff  http://www.stoff.pl/
* Satnogs.Tracker can interface with SatPC32 by Erich Eichmann, DK1TB http://www.dk1tb.de/indexeng.htm
* Satnogs.Tracker can interface with Wxtrack by David Taylor http://www.satsignal.eu/software/wxtrack.htm
* gr-satellites by Dani Estevez available at https://github.com/daniestevez/gr-satellites
