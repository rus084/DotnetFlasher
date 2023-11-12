using ProgLib.DebugInterface.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.DebugInterface.Debug
{
    public class ARMV6M : IArmDebug
    {
        const uint DFSR = 0xE000ED30;

        const uint DHCSR = 0xe000edf0;
        const uint DHCSR_DEBUGEN = (1 << 0);
        const uint DHCSR_HALT = (1 << 1);
        const uint DHCSR_SHALT = (1 << 17);
        const uint DHCSR_S_REGRDY = (1 << 16);
        const uint DHCSR_DBGKEY = (0xa05fu << 16);

        const uint DEMCR = 0xe000edfc;
        const uint DEMCR_VC_CORERESET = (1 << 0);

        const uint AIRCR = 0xe000ed0c;
        const uint AIRCR_VECTKEY = (0x05fa << 16);
        const uint AIRCR_SYSRESETREQ = (1 << 2);

        const uint DCRSR = 0xE000EDF4;
        const uint DCRSR_REGWnR = 1 << 16;

        const uint DCRDR = 0xE000EDF8;

        Dictionary<IArmDebug.Register, uint> RegisterId = new Dictionary<IArmDebug.Register, uint>
        {
            { IArmDebug.Register.R0, 0 },
            { IArmDebug.Register.R1, 1 },
            { IArmDebug.Register.R2, 2 },
            { IArmDebug.Register.R3, 3 },
            { IArmDebug.Register.R4, 4 },
            { IArmDebug.Register.R5, 5 },
            { IArmDebug.Register.R6, 6 },
            { IArmDebug.Register.R7, 7 },
            { IArmDebug.Register.R8, 8 },
            { IArmDebug.Register.R9, 9 },
            { IArmDebug.Register.R10, 10 },
            { IArmDebug.Register.R11, 11 },
            { IArmDebug.Register.R12, 12 },
            { IArmDebug.Register.SP, 13 },
            { IArmDebug.Register.LR, 14 },
            { IArmDebug.Register.MSP, 17 },
            { IArmDebug.Register.PSP, 18 },
            { IArmDebug.Register.PC, 15 },
        };


        IMcuMemory memory;

        public ARMV6M(IMcuMemory memory)
        {
            this.memory = memory;
        }

        public List<uint> GetBPs()
        {
            throw new NotImplementedException();
        }

        public void Halt()
        {
            memory.Write32(DHCSR, DHCSR_DBGKEY | DHCSR_DEBUGEN | DHCSR_HALT);
        }

        public bool IsRunning()
        {
            var reg = memory.Read32(DHCSR);

            return (reg & DHCSR_SHALT) == 0;
        }

        public void Run()
        {
            memory.Write32(DHCSR, DHCSR_DBGKEY | DHCSR_DEBUGEN);
        }

        public void SetBP(uint addr)
        {
            throw new NotImplementedException();
        }

        void SetRegisterRequest(IArmDebug.Register register, bool write)
        {
            uint value = RegisterId[register];

            if (write)
            {
                value |= DCRSR_REGWnR;
            }

            memory.Write32(DCRSR, value);
        }

        void WaitForRegisterLoaded()
        {
            Stopwatch sw = Stopwatch.StartNew();
            uint reg = 0;

            do
            {
                if (sw.ElapsedMilliseconds > 500)
                {
                    throw new TimeoutException("Cannot access core register");
                }
                reg = memory.Read32(DHCSR);
            } while ((reg & DHCSR_S_REGRDY) == 0);

        }

        public void SetRegisters(Dictionary<IArmDebug.Register, uint> registers)
        {
            foreach (var reg in registers)
            {
                memory.Write32(DCRDR, reg.Value);
                SetRegisterRequest(reg.Key, true);
                WaitForRegisterLoaded();
            }
        }

        public Dictionary<IArmDebug.Register, uint> GetRegisters(params IArmDebug.Register[] registers)
        {
            Dictionary<IArmDebug.Register, uint> result = new();

            foreach (var reg in registers)
            {
                SetRegisterRequest(reg, false);
                WaitForRegisterLoaded();
                var value = memory.Read32(DCRDR);

                result.Add(reg, value);
            }

            return result;
        }
    }
}
