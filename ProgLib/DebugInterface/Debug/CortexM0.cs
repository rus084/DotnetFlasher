using ProgLib.DebugInterface.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.DebugInterface.Debug
{
    internal class CortexM0 : ICortexDebug
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

        Dictionary<ICortexDebug.Register, uint> RegisterId = new Dictionary<ICortexDebug.Register, uint>
        {
            { ICortexDebug.Register.R0, 0 },
            { ICortexDebug.Register.R1, 1 },
            { ICortexDebug.Register.R2, 2 },
            { ICortexDebug.Register.R3, 3 },
            { ICortexDebug.Register.R4, 4 },
            { ICortexDebug.Register.R5, 5 },
            { ICortexDebug.Register.R6, 6 },
            { ICortexDebug.Register.R7, 7 },
            { ICortexDebug.Register.R8, 8 },
            { ICortexDebug.Register.R9, 9 },
            { ICortexDebug.Register.R10, 10 },
            { ICortexDebug.Register.R11, 11 },
            { ICortexDebug.Register.R12, 12 },
            { ICortexDebug.Register.SP, 13 },
            { ICortexDebug.Register.LR, 14 },
            { ICortexDebug.Register.MSP, 17 },
            { ICortexDebug.Register.PSP, 18 },
            { ICortexDebug.Register.PC, 15 },
        };


        IMcuMemory memory;

        public CortexM0(IMcuMemory memory)
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

        void SetRegisterRequest(ICortexDebug.Register register, bool write)
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

        public void SetRegisters(Dictionary<ICortexDebug.Register, uint> registers)
        {
            foreach (var reg in registers)
            {
                memory.Write32(DCRDR, reg.Value);
                SetRegisterRequest(reg.Key, true);
                WaitForRegisterLoaded();
            }
        }

        public Dictionary<ICortexDebug.Register, uint> GetRegisters(params ICortexDebug.Register[] registers)
        {
            Dictionary<ICortexDebug.Register, uint> result = new();

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
