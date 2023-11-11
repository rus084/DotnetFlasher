﻿using ProgLib.DebugInterface.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProgLib.DebugInterface.Debug.ICortexDebug;

namespace ProgLib.DebugInterface.Debug
{
    internal interface ICortexDebug
    {
        enum Register
        {
            R0, R1, R2, R3, 
            R4, R5, R6, R7, 
            R8, R9, R10, R11,
            R12, SP, LR, PC,
            MSP, PSP, xPSR
        }

        void Halt();
        void Run();
        bool IsRunning();

        Dictionary<Register, uint> GetRegisters(params Register[] registers);
        void SetRegisters(Dictionary<Register, uint> registers);

        void SetBP(uint addr);
        List<uint> GetBPs();
    }

    static class ICortexDebugExtensions
    {
        public static uint GetRegister(this ICortexDebug debug, Register register)
        {
            return debug.GetRegisters(register).First().Value;
        }

        public static void SetRegister(this ICortexDebug debug, Register register, uint value)
        {
            debug.SetRegisters(new Dictionary<Register, uint> { { register, value } });
        }
    }
}
