using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gibbed.IO;
using Gibbed.Dunia.FileFormats;

namespace DumpBinaryStrings
{
    internal class BinaryResourceFileDumper
    {
        public static List<string> Dump(Stream input)
        {
            var endian = Endian.Little;

            var magic = input.ReadValueU32(endian);
            if (magic != 0x4643626E) // FCbn
            {
                throw new FormatException();
            }

            if (input.ReadValueU16(endian) != 2)
            {
                throw new FormatException();
            }

            var flags = input.ReadValueU16(endian);
            if (flags != 0)
            {
                throw new FormatException();
            }

            input.Seek(4, SeekOrigin.Current);
            input.Seek(4, SeekOrigin.Current);

            var values = new List<string>();
            Object.Dump(input, values, endian);
            return values;
        }

        public class Object
        {
            public static void Dump(Stream input, List<string> values, Endian endian)
            {
                bool isOffset;
                var childCount = input.ReadCount(out isOffset, endian);

                if (isOffset == true)
                {
                    return;
                }

                input.Seek(4, SeekOrigin.Current);

                var valueCount = input.ReadCount(out isOffset, endian);
                if (isOffset == true)
                {
                    throw new NotImplementedException();
                }

                for (var i = 0; i < valueCount; i++)
                {
                    input.Seek(4, SeekOrigin.Current);
                    byte[] value;

                    uint size;
                    long position = input.Position;

                    size = input.ReadCount(out isOffset, endian);
                    if (isOffset == true)
                    {
                        input.Seek(position - size, SeekOrigin.Begin);

                        size = input.ReadCount(out isOffset, endian);
                        if (isOffset == true)
                        {
                            throw new FormatException();
                        }

                        value = new byte[size];
                        input.Read(value, 0, value.Length);

                        input.Seek(position, SeekOrigin.Begin);
                        input.ReadCount(out isOffset, endian);
                    }
                    else
                    {
                        value = new byte[size];
                        input.Read(value, 0, value.Length);
                    }

                    if (value.Length > 1 && value[value.Length - 1] == 0)
                    {
                        var text = Encoding.ASCII.GetString(value, 0, value.Length - 1);
                        if (text.IndexOf('?') == -1 && text.IndexOf('\0') == -1)
                        {
                            if (text.IndexOf('\\') != -1)
                            {
                                values.Add(text);
                            }
                        }
                    }
                }

                for (var i = 0; i < childCount; i++)
                {
                    Object.Dump(input, values, endian);
                }
            }
        }
    }
}
