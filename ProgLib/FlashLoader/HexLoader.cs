using HexIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexUtils
{
    public class HexLoader
    {
        public byte[] Dump => dump;

        public byte[] dump;

        public HexLoader(string file, uint flashStart, uint flashSize, byte filler)
        {
            dump = GetDump(file, flashStart, flashSize, filler);
        }


        public byte[] GetDump(string file, uint flashStart, uint flashSize, byte filler)
        {
            try
            {
                uint address;
                IList<byte> data;

                byte[] array = new byte[flashSize];
                for (int i = 0; i < flashSize; i++)
                    array[i] = filler;

                using (var fileStream = new FileStream(file, FileMode.Open))
                {
                    using (IntelHexReader hexReader = new IntelHexReader(fileStream))
                    {
                        while (hexReader.Read(out address, out data))
                        {
                            for (int i = 0; i < data.Count; i++)
                            {
                                if (address + i < flashStart)
                                {
                                    continue;
                                }

                                if (address + i >= flashStart + flashSize)
                                {
                                    continue;
                                }

                                array[address - flashStart + i] = data[i];
                            }
                        }
                    }
                }


                return array;
            } catch (Exception e)
            {
                throw new Exception("Cannot open hex file");
            }

        }
    }
}
