using HexUtils;
using PowerArgs;
using ProgLib.DebugInterface.Debug;
using ProgLib.DebugInterface.Memory.Edbg;
using ProgLib.FlashLoader;

namespace ProgLib
{
    // A class that describes the command line arguments for this program
    public class MyArgs
    {
        [ArgRequired(PromptIfMissing = true)]
        public string FlmPath { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string FwPath { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<MyArgs>(args);

                var mem = new Edbg();
                var debug = new ARMV6M(mem);


                var l = new FlashLoader.CortexLoader.FlashLoader(mem, debug, parsed.FlmPath, 0x20000000, 0x2000);
                var flashRange = l.ValidFlashRange;

                HexLoader hexLoader = new HexLoader(parsed.FwPath, flashRange.Item1, flashRange.Item2, l.EmplyVal);

                l.PrepareLoad();
                Console.WriteLine($"Flashing");
                l.Load(flashRange.Item1, hexLoader.Dump, (p) =>
                {
                    Console.Write($"Flashing: {(int)(p * 100)}%");
                    Console.CursorLeft = 0;
                });
                Console.WriteLine("Flashing done");

                Console.WriteLine("Verify");

                var readBack = mem.Read8(flashRange.Item1, hexLoader.Dump.Length);

                for (int i = 0; i < readBack.Length; i++)
                {
                    if (hexLoader.dump[i] != readBack[i])
                    {
                        Console.WriteLine($"First data mismatch @{i.ToString("X8")}");
                        Console.WriteLine("ERROR");
                        return;
                    }
                }

                Console.WriteLine("OK");
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<MyArgs>());
            }            
        }

    }
}