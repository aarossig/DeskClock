using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace GifPrepper
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var filename = "/home/andrew/Projects/DeskClock/Images/envelope-cropped.gif";

			var image = Image.FromFile (filename);
			var dimension = new FrameDimension (image.FrameDimensionsList [0]);

			Console.Write ("var chars = new byte[,][] {");

			for (int i = 0; i < image.GetFrameCount (dimension); i++) {
				image.SelectActiveFrame (dimension, i);

				var charDefinitions = new byte[15,5];
				int cnt = 0;

				for(int y = 0; y < image.Height; y += 8) {
					for (int x = 0; x < image.Width; x += 5) {

						for (int charY = 0; charY < 8; charY++) {
							for (int charX = 0; charX < 5; charX++) {
								var pixel = ((Bitmap)image).GetPixel (x + charX, y + charY);

								var value = 0;

								if (pixel.R == 0xFF
									&& pixel.G == 0xFF
									&& pixel.B == 0xFF) {
									value = 1;
								}

								charDefinitions [cnt, ((charY * 5) + charX) / 8] |= (byte)(value << (((charY * 5) + charX) % 8));
							}
						}

						cnt++;
					}
				}

				Console.WriteLine ("{");
				for(int g = 0; g < 15; g++)
				{
					Console.WriteLine("new byte[] {{ {0} }},", string.Join(", ", charDefinitions[g,0], charDefinitions[g,1], charDefinitions[g,2], charDefinitions[g,3], charDefinitions[g,4] ));
				}
				Console.WriteLine ("}, ");
			}
		}
	}
}
