using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Interface;
using Kontract.IO;

namespace archive_level5.PCK
{
    public sealed class PCK
    {
        public List<PckFileInfo> Files;
        Stream _stream = null;

        List<string> legitNames = new List<string>();

        public PCK(Stream input, string filename)
        {
            _stream = input;
            using (var br = new BinaryReaderX(input, true))
            {
                var entries = br.ReadMultiple<Entry>(br.ReadInt32()).ToList();

                var count = 0;
                Files = entries.Select(entry =>
                {
                    br.BaseStream.Position = entry.fileOffset;
                    var hashes = (br.ReadInt16() == 0x64) ? br.ReadMultiple<uint>(br.ReadInt16()).ToList() : null;
                    int blockOffset = hashes?.Count + 1 ?? 0;

                    return new PckFileInfo
                    {
                        FileData = new SubStream(
                            input,
                            entry.fileOffset + blockOffset * 4,
                            entry.fileLength - blockOffset * 4),
                        FileName = $"0x{entry.hash:X8}.bin",
                        //FileName = $"0x{count++:X8}.bin",
                        State = ArchiveFileState.Archived,
                        Entry = entry,
                        Hashes = hashes
                    };
                }).ToList();
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                bw.Write(Files.Count);

                // entryList
                int dataPos = 4 + Files.Count * 0xc;
                foreach (var afi in Files)
                {
                    var entry = new Entry
                    {
                        hash = afi.Entry.hash,
                        fileOffset = dataPos,
                        fileLength = 4 * (afi.Hashes?.Count + 1 ?? 0) + (int)afi.FileSize
                    };
                    dataPos += entry.fileLength;
                    bw.WriteStruct(entry);
                }

                // data
                foreach (var afi in Files)
                {
                    if (afi.Hashes != null)
                    {
                        bw.Write((short)0x64);
                        bw.Write((short)afi.Hashes.Count);
                        foreach (var hash in afi.Hashes)
                            bw.Write(hash);
                    }
                    afi.FileData.CopyTo(bw.BaseStream);
                }
            }
        }

        public void Close()
        {
            _stream?.Close();
            _stream = null;
        }
    }
}
