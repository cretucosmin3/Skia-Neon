using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;

namespace SkiaNeon.Audio
{
    public unsafe static class AudioManager
    {
        public static ALContext alc = null;
        public static AL al = null;
        public static Context* context = null;
        public static Device* device = null;

        public static Dictionary<string, Record> Records = new Dictionary<string, Record>();

        public static void Load()
        {
            try
            {
                al = AL.GetApi(true);
                alc = ALContext.GetApi(true);
            }
            catch
            {
                try
                {
                    al = AL.GetApi(false);
                    alc = ALContext.GetApi(false);
                }
                catch
                {
                    Console.WriteLine("Could not create device");
                    return;
                }
            }

            device = alc.OpenDevice("");

            if (device == null)
            {
                throw new Exception("Could not create device");
            }

            context = alc.CreateContext(device, null);
            alc.MakeContextCurrent(context);

            al.GetError();
        }

        public static void Play(string name, bool loop = false)
        {
            var path = $"sounds/{name}.wav";
            bool AlreadyLoaded = Records.ContainsKey(name);
            
            if (AlreadyLoaded)
            {
                var Record = Records[name];
                Record.Play();
            }
            else
            {
                var NewRecord = new Record(path, loop);
                Records.Add(name, NewRecord);
                NewRecord.Play();
            }
        }

        public static void Preload(string name)
        {
            var path = $"sounds/{name}.wav";
            bool AlreadyLoaded = Records.ContainsKey(name);

            if (AlreadyLoaded)
            {
                Console.WriteLine("Already loaded");
            }
            else
            {
                var NewRecord = new Record(path);
                Records.Add(name, NewRecord);
            }
        }

        public static void Clean()
        {
            foreach (var record in Records.Values)
            {
                record.Destroy();
            }

            alc.DestroyContext(context);
            alc.CloseDevice(device);
            al.Dispose();
            alc.Dispose();
        }
    }
}
