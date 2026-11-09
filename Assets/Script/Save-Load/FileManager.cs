using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using TMPro;
using UnityEngine;
using System;

public class FileManager : MonoBehaviour
{
    #region Singleton
    private static FileManager _singleton;
    public static FileManager instance
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
                DontDestroyOnLoad(value);
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(FileManager)} instance already exists, destoying duplicate");
                Destroy(value);
            }
        }
    }

    private void Awake()
    {
        instance = this;
    }
    #endregion

    IDataService DataService = new JsonDataService();
    bool encryptionEnabled = false;
    long saveTime;
    long loadTime;

    public bool loadedSuccessfully = false;

    public void ToggleEncryption()
    {
        encryptionEnabled = !encryptionEnabled;
    }

    public void SerializeBlankJson()
    {
        PlayerData data = new PlayerData();
        PlayerManager manager = PlayerManager.instance;
        long startTime = DateTime.Now.Ticks;

        if (DataService.SaveData("/player-stats.json", data, encryptionEnabled)) // File name, what to save, whether to encrypt or not
        {
            saveTime = DateTime.Now.Ticks - startTime;
            Debug.Log($"Save Time: {(saveTime / 10000f):N4} ms");
        }
        else
        {
            Debug.LogError("Could not save file!");
        }
    }

    public void SerializeJson()
    {
        PlayerData data = new PlayerData();
        PlayerManager manager = PlayerManager.instance;
        long startTime = DateTime.Now.Ticks;

        // Create player data save file and populate values

        data.levelsBeaten = manager.levelsBeaten;

        // List of all towers unlocked
        foreach (TowerEntry entry in manager.unlockedTowers)
        {
            TowerJson tower = new TowerJson();
            tower.towerId = entry.tower.towerId;
            tower.level = entry.level;
            tower.amount = entry.amount;
            data.unlockedTowers.Add(tower);
        }

        // List of equipped towers
        foreach (TowerEntry entry in manager.equippedTowers)
        {
            TowerJson tower = new TowerJson();
            tower.towerId = entry.tower.towerId;
            tower.level = entry.level;
            tower.amount = entry.amount;
            data.equippedTowers.Add(tower);
        }


        if (DataService.SaveData("/player-stats.json", data, encryptionEnabled)) // File name, what to save, whether to encrypt or not
        {
            saveTime = DateTime.Now.Ticks - startTime;
            Debug.Log($"Save Time: {(saveTime / 10000f):N4} ms");
        }
        else
        {
            Debug.LogError("Could not save file!");
        }
    }

    public void LoadData()
    {
        long startTime = DateTime.Now.Ticks;
        try
        {
            PlayerData data = DataService.LoadData<PlayerData>("/player-stats.json", encryptionEnabled); // Find file by name and what kind of file it is
            loadTime = DateTime.Now.Ticks - startTime;
            PlayerManager.instance.LoadFromFile(data);
            // Do stuff with the file
            Debug.Log($"Loaded in {(loadTime / 10000f):N4}ms");

        }
        catch (Exception e)
        {
            if (!e.Message.Contains("does not exist")) // Differentiates between the first time a game is saved/file moved vs. an actual error
            {
                loadedSuccessfully = false;
                Debug.LogError("Could not load file!");
                Debug.LogError($"FileManager unable to load due to: {e.Message} {e.StackTrace}");
            }
            else
            {
                Debug.Log("No existing file to load from!");
                loadedSuccessfully = true;
                PlayerManager.instance.LoadTowers(Inventory.instance);
            }
            //outputField.text = "<color=#ff0000>Error loading data!</color>";
        }
    }
}
