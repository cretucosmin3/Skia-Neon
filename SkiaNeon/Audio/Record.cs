using Silk.NET.OpenAL;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace SkiaNeon.Audio
{
    public unsafe class Record
    {
        short numChannels = -1;
        int sampleRate = -1;
        int byteRate = -1;
        short blockAlign = -1;
        short bitsPerSample = -1;
        BufferFormat format = 0;
        bool isLooping = false;

        public uint Source;
        public uint Buffer;

        public int Position {
            get
            {
                AudioManager.al.GetSourceProperty(Source, GetSourceInteger.SampleOffset, out var pos);
                return pos;
            }
        }

        public int Finished
        {
            get
            {
                AudioManager.al.GetSourceProperty(Source, GetSourceInteger.SourceState, out int state);
                return state;
            }
        }

        public Record(string path, bool loop = false)
        {
            isLooping = loop;
            Load(path);
        }

        public void Play()
        {

            AudioManager.al.SetSourceProperty(Source, SourceInteger.Buffer, Buffer);
            AudioManager.al.SourcePlay(Source);
        }

        public void Stop()
        {
            AudioManager.al.SourceStop(Source);
        }

        public void Destroy()
        {
            Stop();
            AudioManager.al.DeleteSource(Source);
            AudioManager.al.DeleteBuffer(Buffer);
        }

        private void Load(string path)
        {
            path = Path.Combine(new string[] {
                Directory.GetCurrentDirectory(),
                path
            });

            var filePath = path;
            ReadOnlySpan<byte> file = File.ReadAllBytes(filePath);
            int index = 0;
            if (file[index++] != 'R' || file[index++] != 'I' || file[index++] != 'F' || file[index++] != 'F')
            {
                Console.WriteLine("Given file is not in RIFF format");
                return;
            }

            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
            index += 4;

            if (file[index++] != 'W' || file[index++] != 'A' || file[index++] != 'V' || file[index++] != 'E')
            {
                Console.WriteLine("Given file is not in WAVE format");
                return;
            }

            Source = AudioManager.al.GenSource();
            Buffer = AudioManager.al.GenBuffer();
            AudioManager.al.SetSourceProperty(Source, SourceBoolean.Looping, isLooping);

            while (index + 4 < file.Length)
            {
                var identifier = "" + (char)file[index++] + (char)file[index++] + (char)file[index++] + (char)file[index++];
                var size = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                index += 4;
                if (identifier == "fmt ")
                {
                    if (size != 16)
                    {
                        Console.WriteLine($"Unknown Audio Format with subchunk1 size {size}");
                    }
                    else
                    {
                        var audioFormat = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        if (audioFormat != 1)
                        {
                            Console.WriteLine($"Unknown Audio Format with ID {audioFormat}");
                        }
                        else
                        {
                            numChannels = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;
                            sampleRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                            index += 4;
                            byteRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                            index += 4;
                            blockAlign = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;
                            bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;

                            if (numChannels == 1)
                            {
                                if (bitsPerSample == 8)
                                    format = BufferFormat.Mono8;
                                else if (bitsPerSample == 16)
                                    format = BufferFormat.Mono16;
                                else
                                {
                                    Console.WriteLine($"Can't Play mono {bitsPerSample} sound.");
                                }
                            }
                            else if (numChannels == 2)
                            {
                                if (bitsPerSample == 8)
                                    format = BufferFormat.Stereo8;
                                else if (bitsPerSample == 16)
                                    format = BufferFormat.Stereo16;
                                else
                                {
                                    Console.WriteLine($"Can't Play stereo {bitsPerSample} sound.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Can't play audio with {numChannels} sound");
                            }
                        }
                    }
                }
                else if (identifier == "data")
                {
                    var data = file.Slice(44, size);
                    index += size;

                    fixed (byte* pData = data)
                        AudioManager.al.BufferData(Buffer, format, pData, size, sampleRate);
                    Console.WriteLine($"Read {size} bytes Data");
                }
                else if (identifier == "JUNK")
                {
                    // this exists to align things
                    index += size;
                }
                else if (identifier == "iXML")
                {
                    var v = file.Slice(index, size);
                    var str = Encoding.ASCII.GetString(v);
                    Console.WriteLine($"iXML Chunk: {str}");
                    index += size;
                }
                else
                {
                    Console.WriteLine($"Unknown Section: {identifier}");
                    index += size;
                }
            }

            Console.WriteLine
            (
                $"Success. Detected RIFF-WAVE audio file, PCM encoding. {numChannels} Channels, {sampleRate} Sample Rate, {byteRate} Byte Rate, {blockAlign} Block Align, {bitsPerSample} Bits per Sample"
            );
        }
    }
}
