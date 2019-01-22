# SDRSharp.SatnogsTracker
* Plugin to connect SatPC32 and other DDE apps to SDRSharp.
	* Connects to db.satgnogs.org to obtain Satellites and Transmitter information.
	* Connects to multiple sources for TLE files
	* Displays visibile Satellite frequencies in range of the current Spectrum Bandwidth.
	* Dinamic bar will increase based on elevation for current satellite until it reaches max elevation for the path, marked by green bar.
	* Waterfall is centered on currently tuned Satellite from the external tracking application. 


## Install ##
* Add the Plugin Key to the Plugins.xml file
	* ```<add key="SatnogsTracker" value="SDRSharp.SatnogsTracker.SatnogsTrackerPlugin,SDRSharp.SatnogsTracker" />``` 
	* Copy the Binary to the sdrsharp folder

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
			* {"Callsign":"your callsign>","Latitude":"Value between -90.000 and 90.0000","Longitude":"Value Between -180.0000a and 180.0000","Altitude":" (inKms) Value between 0.000 and 0.300","DDEApp":"Value is either Orbitron or SatPC32 or WxTrack"}
		* SatNOGsMapping.json: File with temporary mappings from SatNOG IDS 999?? to norad tracking IDs, can be edited manually.
			* [{"SatNOGsID":"99911","TrackingID":"43770"},{"SatNOGsID":"99912","TrackingID":"43792"}]
		* SatnogsTracker_logfile.txt created every time SDR launchs for debug purposes

* DriveLetter:sdrharp-exe-location\SatRecordings
	* UTC DATE_Sat Name_norad ID_[AF|IQ].wav
		* Examples
			* 2019-01-03T21-50.202238_JAS-2-(FO-29)_24278_AF.wav
			* 2019-01-03T22-21.016427_JAS-2-(FO-29)_24278_AF.wav

## Credits and References
* OrbitTools Library - Public Edition - Copyright © 2003-2017 Michael F. Henry - http://zeptomoby.com/satellites/index.htm
* Doppler calculations based on Predict formulas from KD2BD https://www.qsl.net/kd2bd/index.html
* Started from GpredictConnector by Alex Wahl, Copyright (c) 2018 Alex Wahl
* Mike Rupprecht, DK3WN and his Satblog http://www.dk3wn.info/p/
* Satnogs.Tracker can interface with Orbitron - Satellite Tracking System (C) 2001-2005 by Sebastian Stoff  http://www.stoff.pl/
* Satnogs.Tracker can interface with SatPC32 by Erich Eichmann, DK1TB http://www.dk1tb.de/indexeng.htm
* Satnogs.Tracker can interface with Wxtrack by David Taylor http://www.satsignal.eu/software/wxtrack.htm
 
