using System;
using TheResistorNetwork.VfdDriver;

namespace DeskClock
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("VFD Desk Clock");

			var display = new VdfDisplay ("/dev/ttyUSB1");
			display.Reset ();
		}
	}
}
