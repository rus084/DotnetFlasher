using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.MCU
{
    public interface IMCU
    {
        ProtectedState protectedState { get; set; }

        void ProgrammFlash(UInt32 address, byte[] data);
        byte[] ReadMem(UInt32 address);
    }
}
