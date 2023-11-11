﻿using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
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
            var debug = new CortexM0(mem);


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

            Console.WriteLine("PC: " + debug.GetRegister(ICortexDebug.Register.PC));

            //var flmPath = "PASTE FLM PATH HERE";

            //var l = new FlashLoader.CortexLoader.FlashLoader(mem, debug, flmPath, 0x20000000, 0x2000);


            /*
            l.PrepareLoad();
            l.Load(address, firmware);

            var readBack = mem.Read8(hexLoader.imageParam.imageOffset, hexLoader.dump.Length);

            for (int i = 0; i < readBack.Length; i++)
            {
                if (hexLoader.dump[i] != readBack[i])
                {
                    Console.WriteLine($"Data mismatch @{i.ToString("X8")}");
                }
            }
            */
        }

    }
}