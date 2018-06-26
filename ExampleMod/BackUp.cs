using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Harmony;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;

public class BackUp : Mod
{
    public BackUp() : base("BackUp", "BackUp", "0.5", "1.01B")
    {
        var harmony = HarmonyInstance.Create("com.raft.marshmello.ObjectReplace");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    int timeWait = 300; //This is in seconds and the default(300) is 5 minutes
    int amountOfBackups = 5; //This is just how many back up that it will save before it starts over riding them
    int backupNumber = 1;
    bool idSet; //if they have set the ID in the config file yet or not
    Int64 usersID;
    bool worldLoaded = false;
    public string worldName;

    void Start()
    {
        CreateCommands();
        CheckBaseFiles();
        StartCoroutine(Timer());
    }

    void CheckBaseFiles()
    {
        if (!Directory.Exists("mods/BackUpMod"))
        {
            Log("BackUp Mod: Base folder was missing so I created a new one");
            Directory.CreateDirectory("mods/BackUpMod");
        }
        if (File.Exists("mods/BackUpMod/config.txt"))
        {
            StreamReader reader = new StreamReader("mods/BackUpMod/config.txt");
            string id = reader.ReadLine();
            Int64 result;
            if (Int64.TryParse(id, out result))
            {
                usersID = result;
                idSet = true;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 1, please re enter your ID");
                idSet = false;
            }

            string waitTimeString = reader.ReadLine();
            int result2;
            if (int.TryParse(waitTimeString,out result2))
            {
                timeWait = result2;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 2, set to default");
            }

            string amountOfBackupsString = reader.ReadLine();
            int result3;
            if (int.TryParse(amountOfBackupsString, out result3))
            {
                amountOfBackups = result3;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 3, set to default");
            }

            string backupNumberString = reader.ReadLine();
            int result4;
            if (int.TryParse(backupNumberString, out result4))
            {
                backupNumber = result4;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 4, set to default");
            }
            reader.Close();
            Log("BackUp Mod: Loaded Config.txt");
        }
    }

    private void CreateCommands()
    {
        RConsole.registerCommand("backup", "Use backup help for help about BackUp", "backup", CheckCommand);
        
    }

    private void CheckCommand()
    {
        string lastCommand = RConsole.lastCommands.LastOrDefault();

        if (lastCommand.Split(' ').Length > 1)
        {
            if (lastCommand.Split(' ')[0] == "backup")
            {
                if (lastCommand.Split(' ')[1] == "help")
                {
                    Help();
                }
                else if (lastCommand.Split(' ')[1] == "time")
                {
                    if (lastCommand.Split(' ').Length >= 2)
                    {
                        string amount = lastCommand.Split(' ')[2];
                        int result;
                        if (int.TryParse(amount, out result))
                        {
                            SetTime(result);
                        }
                    }
                    else
                    {
                        Log("Error");
                        Log("");
                    }
                    
                }
                else if (lastCommand.Split(' ')[1] == "amount")
                {
                    if (lastCommand.Split(' ').Length >= 2)
                    {
                        string amount = lastCommand.Split(' ')[2];
                        int result;
                        if (int.TryParse(amount, out result))
                        {
                            SetAmount(result);
                        }
                    }
                    else
                    {
                        Log("Error");
                        Log("");
                    }
                }
                else if (lastCommand.Split(' ')[1] == "id")
                {
                    if (lastCommand.Split(' ').Length >= 2)
                    {
                        string amount = lastCommand.Split(' ')[2];
                        Int64 result;
                        if (Int64.TryParse(amount, out result))
                        {
                            SetupID(result);
                        }
                        else
                        {
                            Log("Error");
                            Log("Please put a valid id");
                        }
                    }
                    else
                    {
                        Log("Error");
                        Log("Please put a valid id");
                    }
                }
                else if (lastCommand.Split(' ')[1] == "now")
                {
                    if (lastCommand.Split(' ').Length >= 2)
                    {
                        BackupSave(int.Parse(lastCommand.Split(' ')[2].ToString()));
                    }
                    else
                    {
                        Log("Error");
                        Log("Please put a valid id");
                    }
                }
                else
                {
                    Log("Unknow Command");
                    Log("Try 'backup help' for a list of commands");
                }

            }
        }
    }

    private void Help()
    {
        Log("<align=center><size=200%> Backup Mod Help </size> </align>");
        Log("<align=center><size=150%>All commands<b><u> must </u></b>start with backup </size> </align>");
        Log("");
        Log("id");
        Log("id YourSteamID, id is one of the first commands you must do to step up the mod");
        Log("You have to put your steam ID after it so that it can find your world save"); 
        Log(" ");
        Log("time");
        Log("time TimeToWaitInSeconds, time is to set a custom time when every backup is made, by default it is every 5 minutes");
        Log("");
        Log("amount");
        Log("amount HowManyBackups, amount is how many back ups you want it to make before it starts overriding the old ones, default it 5.");
        Log("");
        Log("help");
        Log("help displays this help messages");

    }

    private void SetupID(Int64 id)
    {
        Log(id.ToString());
        try
        {
            CheckBaseFiles();//Just incase some person said, LET DELETE THESE FILES AND BREAK THE MOD
            usersID = id;
            idSet = true;//They have set the id up
            UpdateConfigFile();
            Log("BackUp Mod: ID is now set to " + usersID);
        }
        catch (Exception e)
        {
            Log("BackUp Mod: Error ");
            Log(e.ToString());
        }
    }
    private void SetTime(int time)
    {
        if (idSet)
        {
            timeWait = time;
            Log("BackUp Mod: The time between each backup has changed to " + timeWait);
            UpdateConfigFile();
        }
        else
        {
            NotSetUpID();
        }

    }
    private void SetAmount(int amount)
    {
        if (idSet)
        {
            amountOfBackups = amount;
            Log("BackUp Mod: Number of backups has changed to " + amountOfBackups);
            UpdateConfigFile();
        }
        else
        {
            NotSetUpID();
        }

    }

    private void BackupSave(int backupNumber)
    {
        if (!worldLoaded) return;//Not to backup if in main menu
        UpdateWorldName();
        Log("BackUp Mod: Backing Up");
        try
        {
            if (!Directory.Exists("mods/BackUpMod/" + worldName))
            {
                Directory.CreateDirectory("mods/BackUpMod/" + worldName);
            }

            if (File.Exists("mods/BackUpMod/" + worldName + "/" + worldName + "_" + backupNumber + ".rgd"))//If its already saved, we are deleting it to override it
            {
                File.Delete("mods/BackUpMod/" + worldName + "/" + worldName + "_" + backupNumber + ".rgd");
            }

            UnityEngine.Object.FindObjectOfType<PauseMenu>().SaveGame();//Save to the file first

            File.Copy(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "/LocalLow/Redbeet Interactive/Raft/User/User_" + usersID + "/World/" + worldName + ".rgd", "mods/BackUpMod/" + worldName + "/" + worldName + "_" + backupNumber + ".rgd");

        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
        Log("BackUp Mod: Finishd Backing up");
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(timeWait);
        if (idSet)
        {
            BackupSave(backupNumber);
        }
        StartCoroutine(Timer());
        
    }

    void NotSetUpID()
    {
        Log("Please set up your steam ID first with 'backup id steamIDNumber' ");
    }
    
    public void Log(string text)
    {
        try
        {
            RConsole.Log(text);
        }
        catch (Exception e)
        {
            RConsole.Log(e.ToString());
        }
        
    }

    private void UpdateWorldName()
    {
        worldName = SaveAndLoad.CurrentGameFileName;
    }

    void UpdateConfigFile()
    {
        UpdateWorldName();

        if (File.Exists("mods/BackUpMod/config.txt")) File.Delete("mods/BackUpMod/config.txt");

        StreamWriter writer = new StreamWriter("mods/BackUpMod/config.txt");

        writer.WriteLine(usersID);
        writer.WriteLine(timeWait);
        writer.WriteLine(amountOfBackups);
        writer.WriteLine(backupNumber);
        writer.Close();

        Log("BackUp Mod: Updated Config.txt");
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainScene" && !worldLoaded)
        {
            worldLoaded = true;
        }
        else if (SceneManager.GetActiveScene().name != "MainScene" && worldLoaded)
        {
            worldLoaded = false;
        }
    }


}