using System;

namespace DeskClock
{
	public class WeatherData
	{
		public Coordinates coord { get; set; }
		public SunriseSunset sys { get; set; }
		public AtmosphericConditions[] weather { get; set; }
		public string @base { get; set; }
		public TemperatureConditions main { get; set; }
		public WindConditions wind { get; set; }
		public CloudConditions clouds { get; set; }
		public double dt { get; set; }
		public double id { get; set; }
		public string name { get; set; }
		public string cod { get; set; }
	}
}

