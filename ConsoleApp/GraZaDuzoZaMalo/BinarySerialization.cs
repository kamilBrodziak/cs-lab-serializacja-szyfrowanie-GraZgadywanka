using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GraZaDuzoZaMalo {
    public class SaveException : Exception {
        public SaveException() : base() { }
        public SaveException(string message) : base(message) {}
    }
    public static class BinarySerialization {
        private static string _filePath = Path.GetFullPath("save.txt");

        public static bool SaveExists() {
            return new FileInfo(_filePath).Exists;
        }
        public static void SerializeToFile<T>(T obj) {
            try {
                using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, obj);
            } catch(Exception) {
                throw new SaveException("Wystąpił błąd z zapisaniem pliku.");
            }
            
        }

        public static T DeserializeFromFile<T>() {
            try {
                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                var binaryFormatter = new BinaryFormatter();
                return (T)binaryFormatter.Deserialize(fileStream);
            } catch(Exception) {
                throw new SaveException("Wystąpił błąd z odczytem zapisu gry. Gra zostanie odpalona od nowa.");
            }
            
        }

        public static void DeleteSave() {
            if(SaveExists()) {
                File.Delete(_filePath);
            }
        }
    }
}
