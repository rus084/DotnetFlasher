using ProgLib.DebugInterface.Debug;
using ProgLib.DebugInterface.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.FlashLoader.CortexLoader
{
    internal class FlashLoader
    {
        struct SegmentInfo
        {
            public enum SegmentType
            {
                empty,
                loader,
                stack,
                buffer,
            }

            public SegmentType type;
            public uint addr;
            public int size;
        }


        FlashLoaderModel model;
        IMcuMemory memory;
        ICortexDebug debug;
        uint ramStart;
        uint ramSize;

        uint bufferStart;

        uint breakAddr;
        uint stackInit;

        public FlashLoader(IMcuMemory memory, ICortexDebug debug, string filePath, uint ramStart, uint ramSize)
        {
            this.ramStart = ramStart;
            this.ramSize = ramSize;

            this.memory = memory;
            this.debug = debug;
            model = new FlashLoaderModel(filePath);

            var lastSegment = model.LoadableSections.MaxBy(x => x.addr);

            var emptyRegionStart = lastSegment.addr + lastSegment.data.Length;
            var emptyRegionSize = ramSize - emptyRegionStart;

            var maxSectorSize = model.FlashDeviceDescriptor.Sectors.Max(x => x.SzSector);

            if (emptyRegionSize < maxSectorSize)
            {
                throw new Exception("Not enought ram for use loader");
            }

            bufferStart = ramSize - maxSectorSize;
            Console.WriteLine($"Allocated writebuf @{bufferStart.ToString("X8")}");

            emptyRegionSize -= maxSectorSize;

            if (emptyRegionStart % 8 > 0)
            {
                emptyRegionStart += 8 - (emptyRegionStart % 8);
            }

            breakAddr = (uint)emptyRegionStart;

            emptyRegionStart += 8;
            emptyRegionSize -= 8;

            stackInit = (uint)(emptyRegionStart + emptyRegionSize - 8);
            Console.WriteLine($"Allocated stack @{emptyRegionStart.ToString("X8")} with {emptyRegionSize.ToString("X8")} size");
        }

        public uint RemoteCall(uint addr, uint timeoutMs, params uint[] args)
        {
            var dataSectionAddr = model
                .LoadableSections
                .Where(x => x.type == FlashLoaderModel.LoadableSection.SectionType.data)
                .Single().addr + ramStart;

            var registers = new Dictionary<ICortexDebug.Register, uint>
            {
                { ICortexDebug.Register.PC, addr + ramStart},
                { ICortexDebug.Register.LR, (breakAddr + ramStart) | 1},
                { ICortexDebug.Register.R9, dataSectionAddr },
            };

            var argsMap = new ICortexDebug.Register[]
            {
                ICortexDebug.Register.R0,
                ICortexDebug.Register.R1,
                ICortexDebug.Register.R2,
            };

            if (args.Length > argsMap.Length)
            {
                throw new NotSupportedException($"Args count > {argsMap.Length} is not supported");
            }

            for (int i=0;i<args.Length;i++)
            {
                registers[argsMap[i]] = args[i];
            }

            debug.SetRegisters(registers);

            debug.Run();

            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeoutMs && debug.IsRunning());

            if (debug.IsRunning())
            {
                throw new Exception("Execution was timed out");
            }

            return debug.GetRegister(ICortexDebug.Register.R0);
        }

        void AssertRemoteCallSuccess(uint addr, uint timeoutMs, params uint[] args)
        {
            uint result = RemoteCall(addr, timeoutMs, args);

            if (result != 0)
            {
                throw new Exception("Failed to remote call");
            }
        }

        uint InitFunc(uint addr, uint clk, uint fnc)
        {
            return RemoteCall(model.InitAddr, 500, addr, clk, fnc);
        }
        uint UnInitFunc(uint fnc)
        {
            return RemoteCall(model.UninitAddr, 500, fnc);
        }
        uint EraseSectorFunc(uint addr)
        {
            return RemoteCall(model.EraseSectorAddr, model.FlashDeviceDescriptor.ToErase, addr);
        }
        uint ProgramPageFunc(uint addr, uint sz, uint buf)
        {
            return RemoteCall(model.ProgramPageAddr, model.FlashDeviceDescriptor.ToProg, addr, sz, buf);
        }

        public void PrepareLoad()
        {
            for (int i = 0; i < 3;i++)
            {
                if (!debug.IsRunning())
                    break;
                debug.Halt();
            }

            if (debug.IsRunning())
            {
                throw new Exception("Cannot stop target");
            }

            foreach (var segment in model.LoadableSections)
            {
                memory.Write8(segment.addr + ramStart, segment.data);
                byte[] readBack = memory.Read8(segment.addr + ramStart, segment.data.Length);

                if (segment.data.Length != readBack.Length)
                {
                    Console.WriteLine("Read back different size");
                }

                for (int i=0;i<readBack.Length;i++)
                {
                    if (segment.data[i] != readBack[i])
                    {
                        Console.WriteLine($"Readback mismatch @{(segment.addr + ramStart + i).ToString("X8")}");
                        break;
                    }
                }
            }

            memory.Write16(breakAddr + ramStart, 0xBEAB);

            debug.SetRegister(ICortexDebug.Register.SP, stackInit + ramStart);
        }

        public void Load(uint addr, byte[] data)
        {
            for (uint i = 0; i < data.Length;)
            {
                uint size = model.FlashDeviceDescriptor.SzPage;

                InitFunc(addr, 10000000, 1);
                EraseSectorFunc(addr + i);
                UnInitFunc(1);

                memory.Write8(bufferStart + ramStart, data.AsSpan().Slice((int)i, (int)size));

                InitFunc(addr, 0, 2);
                ProgramPageFunc(addr + i, size, bufferStart + ramStart);
                UnInitFunc(2);

                Console.WriteLine($"Loaded: {i.ToString("X8")}");

                i += size;
            }
        }
    }
}
