// Encrypted PlayerPrefs
// Written by Sven Magnus
// MD5 code by Matthew Wegner

#if !TP_CORE
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

namespace TiltingPoint {

    public class EncryptedPlayerPrefs  {
        
        /// <summary>
        /// Using device immutable information as encryption keys.
        /// </summary>
        private static readonly string privateKey = SystemInfo.deviceUniqueIdentifier;
        
        private static readonly string[] keys = {
            SystemInfo.deviceModel, SystemInfo.deviceName, SystemInfo.graphicsDeviceName
        };
        
        private static string Md5(string strToEncrypt) {
            UTF8Encoding ue = new UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);
     
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
     
            string hashString = "";
     
            for (int i = 0; i < hashBytes.Length; i++) {
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }
     
            return hashString.PadLeft(32, '0');
        }
        
        /// <summary>
        /// Encrypts a message and saves it into PlayerPrefs.
        /// </summary>
        public static void SaveEncryption(string key, string type, string value) {
            int keyIndex = (int)Mathf.Floor(Random.value * keys.Length);
            string secretKey = keys[keyIndex];
            string check = Md5(type + "_" + privateKey + "_" + secretKey + "_" + value);
            PlayerPrefs.SetString(key + "_encryption_check", check);
            PlayerPrefs.SetInt(key + "_used_key", keyIndex);
        }
        
        /// <summary>
        /// Checks if encrypted value exists.
        /// </summary>
        public static bool CheckEncryption(string key, string type, string value) {
            int keyIndex = PlayerPrefs.GetInt(key + "_used_key");
            string secretKey = keys[keyIndex];
            string check = Md5(type + "_" + privateKey + "_" + secretKey + "_" + value);
            if(!PlayerPrefs.HasKey(key + "_encryption_check")) return false;
            string storedCheck = PlayerPrefs.GetString(key + "_encryption_check");
            return storedCheck == check;
        }
        
        /// <summary>
        /// Encrypts and saves an int value.
        /// </summary>
        public static void SetInt(string key, int value) {
            PlayerPrefs.SetInt(key, value);
            SaveEncryption(key, "int", value.ToString());
        }
        
        /// <summary>
        /// Encrypts and saves a float value.
        /// </summary>
        public static void SetFloat(string key, float value) {
            PlayerPrefs.SetFloat(key, value);
            SaveEncryption(key, "float", Mathf.Floor(value*1000).ToString());
        }
        
        /// <summary>
        /// Encrypts and saves a string value.
        /// </summary>
        public static void SetString(string key, string value) {
            PlayerPrefs.SetString(key, value);
            SaveEncryption(key, "string", value);
        }
        
        /// <summary>
        /// Decrypts and returns an int.
        /// </summary>
        public static int GetInt(string key) {
            return GetInt(key, 0);
        }
        
        /// <summary>
        /// Decrypts and returns a float.
        /// </summary>
        public static float GetFloat(string key) {
            return GetFloat(key, 0f);
        }
        
        /// <summary>
        /// Decrypts and returns a string.
        /// </summary>
        public static string GetString(string key) {
            return GetString(key, "");
        }
        
        /// <summary>
        /// Decrypts and returns an int or a default value if not found.
        /// </summary>
        public static int GetInt(string key,int defaultValue) {
            int value = PlayerPrefs.GetInt(key);
            if(!CheckEncryption(key, "int", value.ToString())) return defaultValue;
            return value;
        }
        
        /// <summary>
        /// Decrypts and returns a float or a default value if not found.
        /// </summary>
        public static float GetFloat(string key, float defaultValue) {
            float value = PlayerPrefs.GetFloat(key);
            if(!CheckEncryption(key, "float", Mathf.Floor(value*1000).ToString())) return defaultValue;
            return value;
        }
        
        /// <summary>
        /// Decrypts and returns a string or a default value if not found.
        /// </summary>
        public static string GetString(string key, string defaultValue) {
            string value = PlayerPrefs.GetString(key);
            if(!CheckEncryption(key, "string", value)) return defaultValue;
            return value;
        }
        
        /// <summary>
        /// Returns true if a key is found in PlayerPrefs.
        /// </summary>
        public static bool HasKey(string key) {
            return PlayerPrefs.HasKey(key);
        }
        
        /// <summary>
        /// Delete value and encryption information from PlayerPrefs.
        /// </summary>
        public static void DeleteKey(string key) {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + "_encryption_check");
            PlayerPrefs.DeleteKey(key + "_used_key");
        }
    }
}
#endif