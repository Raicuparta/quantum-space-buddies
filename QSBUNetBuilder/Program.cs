using System;
using System.IO;

namespace QSBUNetBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"C:\Users\Henry\source\repos\quantum-space-buddies2\QSB\bin\Debug\QSB.dll";
            File.Delete(@"C:\Users\Henry\source\repos\quantum-space-buddies2\QSBUNetBuilder\Assembly-QSB.dll");
            File.Copy(file, @"C:\Users\Henry\source\repos\quantum-space-buddies2\QSBUNetBuilder\Assembly-QSB.dll");


            Unity.UNetWeaver.Program.Process
            (
                @"C:\Program Files\Unity\Hub\Editor\2017.4.33f1\Editor\Data\Managed\UnityEngine.dll",
                @"C:\Program Files\Unity\Hub\Editor\2017.4.33f1\Editor\Data\UnityExtensions\Unity\Networking\UnityEngine.Networking.dll",
                @"C:\Users\Henry\source\repos\quantum-space-buddies2\QSBUNetBuilder\", //Output directory for the new .dll file
                new string[]
                {
                    @"C:\Users\Henry\source\repos\quantum-space-buddies2\QSBUNetBuilder\Assembly-QSB.dll" //Your custom dll file. Remember to add "Assembly-" in front of the name
                },
                new string[]
                {

                },
                null,
                (str) => Console.WriteLine("Warning: " + str),
                (str) => Console.WriteLine("Error: " + str)
            );

            File.Delete(@"C:\Users\Henry\AppData\Roaming\OuterWildsModManager\OWML\Mods\QSB\QSB.dll");
            File.Copy(@"C:\Users\Henry\source\repos\quantum-space-buddies2\QSBUNetBuilder\Assembly-QSB.dll", @"C:\Users\Henry\AppData\Roaming\OuterWildsModManager\OWML\Mods\QSB\QSB.dll");
        }
    }
}
