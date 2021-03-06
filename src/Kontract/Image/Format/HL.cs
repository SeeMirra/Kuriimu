﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class HL : IImageFormat
    {
        public int bitDepth { get; set; }

        int rDepth;
        int gDepth;

        ByteOrder byteOrder;

        public HL(int r, int g, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            bitDepth = r + g;
            if (bitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {bitDepth}");
            if (bitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {bitDepth}");
            if (bitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {bitDepth}");
            if (r < 4 || g < 4) throw new Exception($"Red and Green value can't be smaller than 4.\nGiven Red: {r}; Given Green: {g}");

            rDepth = r;
            gDepth = g;

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                var rShift = gDepth;

                var gBitMask = (int)Math.Pow(2, gDepth) - 1;
                var rBitMask = (int)Math.Pow(2, rDepth) - 1;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long value = 0;

                    switch (bitDepth)
                    {
                        case 4:
                            value = br.ReadNibble();
                            break;
                        case 8:
                            value = br.ReadByte();
                            break;
                        case 16:
                            value = br.ReadUInt16();
                            break;
                        default:
                            throw new Exception($"BitDepth {bitDepth} not supported!");
                    }

                    yield return Color.FromArgb(
                        (gDepth == 0) ? 255 : CorrectValue((int)(value & gBitMask), gDepth),
                        (rDepth == 0) ? 255 : CorrectValue((int)(value >> rShift & rBitMask), rDepth),
                        (rDepth == 0) ? 255 : CorrectValue((int)(value >> rShift & rBitMask), rDepth),
                        (rDepth == 0) ? 255 : CorrectValue((int)(value >> rShift & rBitMask), rDepth));
                }
            }
        }

        int CorrectValue(int value, int depth)
        {
            switch (depth)
            {
                case 4:
                    return value * 15;
                case 5:
                    return value * 33 / 4;
                case 6:
                    return value * 65 / 16;
                case 7:
                    return value * 129 / 64;
                case 9:
                    return value / 2;
                case 10:
                    return value / 4;
                default:
                    return value;
            }
        }

        public void Save(Color color, Stream output)
        {
        }
    }
}
