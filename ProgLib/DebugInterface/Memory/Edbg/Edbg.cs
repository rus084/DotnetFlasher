using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProgLib.DebugInterface.Memory;

namespace ProgLib.DebugInterface.Memory.Edbg
{
    class Edbg : IMcuMemory, IDisposable
    {

        [DllImport("edbg.dll")]
        public static extern int edbgLibStart(string target);

        [DllImport("edbg.dll")]
        public static extern int edbgLibClose();
        [DllImport("edbg.dll")]
        public static extern IntPtr edbgLibGetLastError();

        [DllImport("edbg.dll")]
        public static extern int dap_reset_pin(int state);
        [DllImport("edbg.dll")]
        public static extern int dap_reset_target_hw(int state);

        [DllImport("edbg.dll")]
        public static extern int dap_read_block(uint addr, byte[] data, int size);
        [DllImport("edbg.dll")]
        public static extern int dap_write_block(uint addr, byte[] data, int size);


        private void ThrowInternalException()
        {
            var errPtr = edbgLibGetLastError();
            var err = Marshal.PtrToStringAnsi(errPtr);
            throw new Exception(err);
        }

        public Edbg(string type = "genericM0")
        {
            if (edbgLibStart(type) != 0)
                ThrowInternalException();
        }

        public byte[] Read8(uint address, int count)
        {
            byte[] result = new byte[count];

            if (dap_read_block(address, result, count) != 0)
                ThrowInternalException();

            return result;
        }

        public ushort[] Read16(uint address, int count)
        {
            ushort[] result = new ushort[count];

            byte[] tmp = new byte[count * 2];
            if (dap_read_block(address, tmp, tmp.Length) != 0)
                ThrowInternalException();

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (ushort)(tmp[2 * i] | tmp[2 * i + 1] << 8);
            }

            return result;
        }

        public uint[] Read32(uint address, int count)
        {
            uint[] result = new uint[count];

            byte[] tmp = new byte[count * 4];
            if (dap_read_block(address, tmp, tmp.Length) != 0)
                ThrowInternalException();

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (uint)(tmp[4 * i] | tmp[4 * i + 1] << 8 | tmp[4 * i + 2] << 16 | tmp[4 * i + 3] << 24);
            }

            return result;
        }

        public void Write8(uint address, params byte[] data)
        {
            if (dap_write_block(address, data, data.Length) != 0)
                ThrowInternalException();
        }

        public void Write16(uint address, params ushort[] data)
        {
            byte[] tmp = new byte[data.Length * 2];

            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)(data[i / 2] >> i % 2 * 8);
            }

            if (dap_write_block(address, tmp, tmp.Length) != 0)
                ThrowInternalException();
        }

        public void Write32(uint address, params uint[] data)
        {
            byte[] tmp = new byte[data.Length * 4];

            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)(data[i / 4] >> i % 4 * 8);
            }

            if (dap_write_block(address, tmp, tmp.Length) != 0)
                ThrowInternalException();
        }

        public void ResetMCU()
        {
            //dap_reset_pin(0);
            //Thread.Sleep(10);
            //dap_reset_pin(1);
        }

        public void Dispose()
        {
            edbgLibClose();
        }
    }
}
