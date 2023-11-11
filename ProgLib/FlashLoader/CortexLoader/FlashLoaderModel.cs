using ELFSharp.ELF.Sections;
using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLib.FlashLoader.CortexLoader
{
    public struct FlashDevice
    {
        public enum DeviceType
        {
            Unknown = 0,
            OnChip = 1,
            Ext8Bit = 2,
            Ext16Bit = 3,
            Ext32Bit = 4,
            ExtSpi = 5,
        }

        public struct FlashSector : IEquatable<FlashSector>
        {
            public uint SzSector;
            public uint AddrSector;

            public static FlashSector SectorEnd = new FlashSector { AddrSector = 0xFFFFFFFF, SzSector = 0xFFFFFFFF };
            public static FlashSector FromArr(byte[] data, ref uint offset)
            {
                FlashSector result = new FlashSector();

                result.SzSector = 0;
                result.SzSector |= data[offset++];
                result.SzSector |= (uint)(data[offset++] << 8);
                result.SzSector |= (uint)(data[offset++] << 16);
                result.SzSector |= (uint)(data[offset++] << 24);

                result.AddrSector = 0;
                result.AddrSector |= data[offset++];
                result.AddrSector |= (uint)(data[offset++] << 8);
                result.AddrSector |= (uint)(data[offset++] << 16);
                result.AddrSector |= (uint)(data[offset++] << 24);

                return result;
            }

            public bool Equals(FlashSector obj)
            {
                if (ReferenceEquals(this, obj)) return true;
                if (obj.SzSector != SzSector) return false;
                if (obj.AddrSector != AddrSector) return false;
                return true;
            }
        };

        public ushort Vers;
        public string DevName;
        public DeviceType DevType;
        public uint DevAdr;
        public uint SzDev;
        public uint SzPage;
        public uint Res;
        public byte ValEmpty;
        public uint ToProg;
        public uint ToErase;
        public List<FlashSector> Sectors;

        public static FlashDevice FromArr(byte[] data, ref uint offset)
        {
            FlashDevice result = new FlashDevice();

            result.Vers = 0;
            result.Vers |= data[offset++];
            result.Vers |= (ushort)(data[offset++] << 8);

            result.DevName = string.Empty;
            for (int i = 0; i < 128; i++)
            {
                var b = data[offset + i];
                if (b == 0)
                {
                    break;
                }
                result.DevName += (char)b;
            }
            offset += 128;

            result.DevType = 0;
            result.DevType = (DeviceType)data[offset++];
            if (data[offset] != 0)
            {
                throw new InvalidDataException("Device type is not parsable");
            }
            offset++;

            result.DevAdr = 0;
            result.DevAdr |= data[offset++];
            result.DevAdr |= (uint)(data[offset++] << 8);
            result.DevAdr |= (uint)(data[offset++] << 16);
            result.DevAdr |= (uint)(data[offset++] << 24);

            result.SzDev = 0;
            result.SzDev |= data[offset++];
            result.SzDev |= (uint)(data[offset++] << 8);
            result.SzDev |= (uint)(data[offset++] << 16);
            result.SzDev |= (uint)(data[offset++] << 24);

            result.SzPage = 0;
            result.SzPage |= data[offset++];
            result.SzPage |= (uint)(data[offset++] << 8);
            result.SzPage |= (uint)(data[offset++] << 16);
            result.SzPage |= (uint)(data[offset++] << 24);

            result.Res = 0;
            result.Res |= data[offset++];
            result.Res |= (uint)(data[offset++] << 8);
            result.Res |= (uint)(data[offset++] << 16);
            result.Res |= (uint)(data[offset++] << 24);

            result.ValEmpty = data[offset++];
            offset += 3;

            result.ToProg = 0;
            result.ToProg |= data[offset++];
            result.ToProg |= (uint)(data[offset++] << 8);
            result.ToProg |= (uint)(data[offset++] << 16);
            result.ToProg |= (uint)(data[offset++] << 24);

            result.ToErase = 0;
            result.ToErase |= data[offset++];
            result.ToErase |= (uint)(data[offset++] << 8);
            result.ToErase |= (uint)(data[offset++] << 16);
            result.ToErase |= (uint)(data[offset++] << 24);

            result.Sectors = new List<FlashSector>();
            for (FlashSector sector = FlashSector.FromArr(data, ref offset); sector.Equals(FlashSector.SectorEnd); sector = FlashSector.FromArr(data, ref offset))
            {
                result.Sectors.Add(sector);
            }

            if (result.Sectors.Count == 0)
            {
                result.Sectors.Add(new FlashSector { AddrSector = 0, SzSector = result.SzPage });
            }

            return result;
        }

    }

    public class FlashLoaderModel
    {
        public struct LoadableSection
        {
            public enum SectionType
            {
                prg,
                data
            };

            public SectionType type;
            public uint addr;
            public byte[] data;
        }
        public uint InitAddr { get; }
        public uint UninitAddr { get; }
        public uint EraseSectorAddr { get; }
        public uint ProgramPageAddr { get; }
        public FlashDevice FlashDeviceDescriptor { get; }
        public List<LoadableSection> LoadableSections { get; }

        public FlashLoaderModel(string filePath)
        {
            Console.WriteLine($"Loading flash loader: {filePath}");
            var elf = ELFReader.Load<uint>(filePath);

            {
                elf.TryGetSection("PrgCode", out var prgCode);
                elf.TryGetSection("PrgData", out var prgData);
                LoadableSections = new List<LoadableSection>();
                if (prgCode != null)
                {
                    LoadableSection section = new LoadableSection();
                    section.data = prgCode.GetContents();
                    section.addr = prgCode.LoadAddress;
                    section.type = LoadableSection.SectionType.prg;
                    Console.WriteLine($"Found code section @{prgCode.LoadAddress.ToString("X8")}:{section.data.Length.ToString("X8")}");
                    LoadableSections.Add(section);
                }
                if (prgData != null)
                {
                    LoadableSection section = new LoadableSection();
                    section.data = prgData.GetContents();
                    section.addr = prgData.LoadAddress;
                    section.type = LoadableSection.SectionType.data;
                    Console.WriteLine($"Found data section @{prgData.LoadAddress.ToString("X8")}:{section.data.Length.ToString("X8")}");
                    LoadableSections.Add(section);
                }
            }

            {
                var functions = ((ISymbolTable)elf.GetSection(".symtab")).Entries.Where(x => x.Type == SymbolType.Function);

                var initFunc = (SymbolEntry<uint>)functions.Single(f => f.Name == "Init");
                var unInitFunc = (SymbolEntry<uint>)functions.Single(f => f.Name == "UnInit");
                var eraseSectorFunc = (SymbolEntry<uint>)functions.Single(f => f.Name == "EraseSector");
                var programPageFunc = (SymbolEntry<uint>)functions.Single(f => f.Name == "ProgramPage");

                InitAddr = initFunc.Value;
                UninitAddr = unInitFunc.Value;
                EraseSectorAddr = eraseSectorFunc.Value;
                ProgramPageAddr = programPageFunc.Value;

                Console.WriteLine($"Neccesary functions detected");
            }

            {
                var descEntry = (SymbolEntry<uint>)((ISymbolTable)elf.GetSection(".symtab")).Entries.Single(x => x.Name == "FlashDevice");

                var content = descEntry.PointedSection.GetContents();

                uint startAddr = descEntry.Value - descEntry.PointedSection.LoadAddress;
                if (startAddr > descEntry.PointedSection.Size)
                {
                    throw new ArgumentException("");
                }

                FlashDeviceDescriptor = FlashDevice.FromArr(content, ref startAddr);

                Console.WriteLine($"Descriptor parsed succesfully, name: {FlashDeviceDescriptor.DevName}");
            }
        }
    }
}
