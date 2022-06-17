using Newtonsoft.Json;
using System.IO;

namespace TeamspeakActivityBot.Helper
{
    public class JsonFile<T> where T : new()
    {
        private FileInfo jsonFile { get; set; }

        private object fileLock = new();

        private T _data;

        public JsonFile(FileInfo file)
        {
            jsonFile = file;
        }

        public T Data
        {
            get
            {
                if (_data == null)
                    Read();

                return _data;
            }
            set
            {
                _data = value;
                Save();
            }
        }

        public void Save()
        {
            lock (fileLock)
            {
                using (var fStream = this.jsonFile.OpenWrite())
                {
                    using (var writer = new StreamWriter(fStream))
                    {
                        writer.Write(JsonConvert.SerializeObject(_data));
                    }
                }
            }
        }

        public void Read()
        {
            if (!File.Exists(this.jsonFile.FullName))
            {
                _data = new T();
                return;
            }


            lock (fileLock)
            {
                using (var fStream = this.jsonFile.OpenRead())
                {
                    using (var reader = new StreamReader(fStream))
                    {
                        _data = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    }
                }
            }
        }
    }


}
