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
    public class DXT : IImageFormat
    {
        public enum Version
        {
            DXT1,
            DXT3,
            DXT5
        }

        public int bitDepth { get; set; }
        public int blockBitDepth { get; set; }

        Version version;
        bool standard;

        ByteOrder byteOrder;

        public DXT(Version version, bool standard = false, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            bitDepth = (version == Version.DXT1) ? 4 : 8;
            blockBitDepth = (version == Version.DXT1) ? 64 : 128;

            this.version = version;
            this.standard = standard;

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                Enum.TryParse<Support.DXT.Formats>(version.ToString(), false, out var dxtFormat);
                var dxtdecoder = new Support.DXT.Decoder(dxtFormat);

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    yield return dxtdecoder.Get(() =>
                    {
                        //if (br.BaseStream.Position == br.BaseStream.Length) return (0, 0);
                        var dxt5Alpha = version == Version.DXT3 || version == Version.DXT5 ? br.ReadUInt64() : 0;
                        return (dxt5Alpha, br.ReadUInt64());
                    });
                }
            }
        }

        public void Save(Color color, Stream output)
        {
        }
    }
}
