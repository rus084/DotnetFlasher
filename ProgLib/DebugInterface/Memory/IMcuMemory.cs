using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.DebugInterface.Memory
{
    interface IMcuMemory
    {
        byte[] Read8(uint address, int count);
        ushort[] Read16(uint address, int count);
        uint[] Read32(uint address, int count);
        void Write8(uint address, params byte[] data);
        void Write16(uint address, params ushort[] data);
        void Write32(uint address, params uint[] data);
    }

    static class IMcuMemoryExtension
    {
        public static void Write8(this IMcuMemory memory, uint address, Span<byte> data)
        {
            memory.Write8(address, data.ToArray());
        }

        public static byte Read8(this IMcuMemory memory, uint address)
        {
            return memory.Read8(address, 1)[0];
        }
        public static ushort Read16(this IMcuMemory memory, uint address)
        {
            return memory.Read16(address, 1)[0];
        }
        public static uint Read32(this IMcuMemory memory, uint address)
        {
            return memory.Read32(address, 1)[0];
        }
    }
}
