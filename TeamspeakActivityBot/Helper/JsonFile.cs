﻿using Newtonsoft.Json;
using System.IO;

namespace TeamspeakActivityBot.Helper
{
    public class JsonFile<T> where T : new()
    {
        private FileInfo jsonFile { get; set; }

        private object fileLock = new();

        private T _data;

        private bool _fileRead;
        private bool _fileSaved;

        public JsonFile(FileInfo file)
        {
            jsonFile = file;
            _fileRead = false;
            _fileSaved = false;
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
            lock (fileLock)
            {
                using (var fStream = this.jsonFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
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
            if (!File.Exists(this.jsonFile.FullName))
            {
                _data = new T();
                Save();
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

            _fileRead = true;

            if (!_fileSaved)
            {
                Save();
            }
        }
    }


}