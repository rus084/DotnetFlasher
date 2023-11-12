using HexUtils;
using ProgLib.DebugInterface.Debug;
using ProgLib.DebugInterface.Memory.Edbg;
using ProgLib.FlashLoader;

namespace ProgLib
{
    public class Class1
    {
        static void Main()
        {
            var mem = new Edbg();
            var debug = new ARMV6M(mem);


            for (int i = 0; i < 3; i++)
            {
                if (!debug.IsRunning())
                    break;
                debug.Halt();
            }

            if (debug.IsRunning())
            {
                throw new Exception("Cannot stop target");
            }

            Console.WriteLine("PC: " + debug.GetRegister(IArmDebug.Register.PC));

            //return;

            var flmPath = "PASTE FLM PATH HERE";
            var hexPath = "PASTE HEX PATH HERE";


            var l = new FlashLoader.CortexLoader.FlashLoader(mem, debug, flmPath, 0x20000000, 0x2000);
            var flashRange = l.ValidFlashRange;

            HexLoader hexLoader = new HexLoader(hexPath, flashRange.Item1, flashRange.Item2 - flashRange.Item1, l.EmplyVal);


            l.PrepareLoad();
            l.Load(flashRange.Item1, hexLoader.Dump);

            var readBack = mem.Read8(flashRange.Item1, hexLoader.Dump.Length);

            for (int i = 0; i < readBack.Length; i++)
            {
                if (hexLoader.dump[i] != readBack[i])
                {
                    Console.WriteLine($"Data mismatch @{i.ToString("X8")}");
                }
            }
            
        }

    }
}