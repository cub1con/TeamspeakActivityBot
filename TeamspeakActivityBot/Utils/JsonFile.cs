using Newtonsoft.Json;
using System.IO;

namespace TeamspeakActivityBot.Utils
{
    public class JsonFile<T> where T : new()
    {
        private FileInfo _jsonFile { get; set; }

        private object _fileLock = new();

        private T _data;

        private bool _fileRead;
        private bool _fileSaved;

        public JsonFile(FileInfo file)
        {
            _jsonFile = file;
            _fileRead = false;
            Read();
        }

        public T Data
        {
            get
            {
                if (!_fileRead)
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
            lock (_fileLock)
            {
                using (var fStream = this._jsonFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(fStream))
                    {
                        writer.Write(JsonConvert.SerializeObject(_data));
                    }
                }
            }

            _fileSaved = true;
        }

        public void Read()
        {
            if (!File.Exists(this._jsonFile.FullName))
            {
                _data = new T();
                Save();
            }


            lock (_fileLock)
            {
                using (var fStream = this._jsonFile.OpenRead())
                {
                    using (var reader = new StreamReader(fStream))
                    {
                        _data = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    }
                }
            }

            _fileRead = true;

            if (!_fileSaved)
            {
                Save();
            }
        }
    }


}
