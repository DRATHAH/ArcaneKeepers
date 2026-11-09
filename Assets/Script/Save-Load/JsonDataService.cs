using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class JsonDataService : IDataService
{
    // In case we want encryption (not active atm)
    private const string KEY = "9MXD+le3LcckJyb+swNmQ3FvQ72B8uv1K6qmG0U5Suw=";
    private const string IV = "FRdAoXLVgJ6vCR9fBOiIjg==";

    public bool SaveData<T>(string RelativePath, T Data, bool Encrypted)
    {
        string path = Application.persistentDataPath + RelativePath; // Get location where we want to write save file (Users>Name>AppData>LocalLow>DefaultCompany>Arcane Keepers)

        try
        {
            if (File.Exists(path))
            {
                Debug.LogFormat("Data exists. Deleting old file and writing new one!");
                File.Delete(path);
            }
            else
            {
                Debug.Log("Creating file for the first time!");
            }
            using FileStream stream = File.Create(path);
            if (Encrypted)
            {
                WriteEncryptedData(Data, stream);
            }
            else
            {
                stream.Close();
                File.WriteAllText(path, JsonConvert.SerializeObject(Data)); // Converts data into Json file
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unable to save data due to: {e.Message} {e.StackTrace}");
            return false;
        }
    }

    private void WriteEncryptedData<T>(T Data, FileStream stream)
    {
        using Aes aesProvider = Aes.Create();
        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);
        using ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
        using CryptoStream cryptoStream = new CryptoStream(
            stream,
            cryptoTransform,
            CryptoStreamMode.Write
        );

        // Gets randomized IV and Key to use for aesProvider (for encryption)
        //Debug.Log($"Initialization Vector: {Convert.ToBase64String(aesProvider.IV)}");
        //Debug.Log($"Key: {Convert.ToBase64String(aesProvider.Key)}");
        cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Data)));
    }

    public T LoadData<T>(string RelativePath, bool Encrypted)
    {
        string path = Application.persistentDataPath + RelativePath;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{path} does not exist!"); // Immediately returns an error to the method that called this function
        }

        try
        {
            T data;
            if (Encrypted)
            {
                data = ReadEncryptedData<T>(path);
            }
            else
            {
                data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load data due to: {e.Message} {e.StackTrace}");
            throw;
        }
    }

    private T ReadEncryptedData<T>(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        using Aes aesProvider = Aes.Create();

        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);

        using ICryptoTransform cryptoTransform = aesProvider.CreateDecryptor(
            aesProvider.Key,
            aesProvider.IV
        );
        using MemoryStream decryptionStream = new MemoryStream(fileBytes); // Replaces file stream, has file data already in it
        using CryptoStream cryptoStream = new CryptoStream(
            decryptionStream,
            cryptoTransform,
            CryptoStreamMode.Read
        );
        using StreamReader reader = new StreamReader(cryptoStream);

        string result = reader.ReadToEnd();

        Debug.Log($"Decrypted result (if following is not legible, probably wrong key or iv): {result}");
        return JsonConvert.DeserializeObject<T>(result);
    }
}
