# SDRSharp.SatnogsTracker Changelog
## 1.4
* New Visualization shows highest elevation per Sat
## 1.3
*Added Satnogs JSON parsers
*Loads KEPs from multiple sources
*Calculates Doppler for Visible Sats/Transmitters
*Still working on best way to display
## v1.2
*Fixed Exit and Disconnect
*Stop Recordings if SatElevation reaches 0
## v1.1
*Added IQ and AF Recording 
*DDE String parses the following fields from tracking software
	SN<Satellite Name>
	AZ<Azimuth>
	EL<Elevation>
	UP<Uplink Frequency(Not Used)>
	UM<Uplink Mode(Not Used)>
	DN<Downlink Frequency in Hz>
	DM<Downlink Mode>
	BW<Filter Bandwidth in Hz>
	ID<Satnogs ID>
	RB<yes|no> Record IQ
	RA<yes|no> Record AF
*File Name Conventions
	*<Date>_SatName_SatID_<AF|IQ>.wav
	*2019-01-03T03-57.095984_NUDTSAT_42787_AF.wav
	*2019-01-03T03-59.397224_NUDTSAT_42787_IQ
## v1.0
* First running program 
* tested with 
  * SDRSharp v1.0.0.1700
