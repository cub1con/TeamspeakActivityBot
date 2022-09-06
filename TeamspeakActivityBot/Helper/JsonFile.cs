using Newtonsoft.Json;
using System.IO;

namespace TeamspeakActivityBot.Helper
{
    public class JsonFile<T> where T : new()
    {
        private FileInfo jsonFile { get; set; }

        private object fileLock = new();

        private T _data;

        private bool saveOnSet;

        public JsonFile(string file, bool saveOnSet = true)
        {
            jsonFile = new FileInfo(file);
            this.saveOnSet = saveOnSet;
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

                if (saveOnSet)
                    Save();
            }
        }

        public void Save()
        {
            lock (fileLock)
            {
                var jsonString = JsonConvert.SerializeObject(_data, Formatting.Indented);
                using var fStream = this.jsonFile.CreateText();
                fStream.Write(jsonString);
            }
        }

        public void Read()
        {
            if (!File.Exists(this.jsonFile.FullName))
            {
                _data = new T();
                Save();
                return;
            }


            lock (fileLock)
            {
                using var fStream = this.jsonFile.OpenRead();
                using var reader = new StreamReader(fStream);
                _data = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }
    }


}
