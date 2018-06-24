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
    bool idSet = false; //if they have set the ID in the config file yet or not
    int usersID;
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

        if (!File.Exists("mods/BackUpMod/config.txt"))
        {
            Log("BackUp Mod: config.txt was missing, so I created a new one");
            File.Create("mods/BackUpMod/config.txt");
        }
        else
        {
            StreamReader reader = new StreamReader("mods/BackUpMod/config.txt");
            string id = reader.ReadLine();//
            int result;
            if (int.TryParse(id, out result))
            {
                usersID = result;
                idSet = true;
            }
            else
            {
                Log("BackUp Mod: In the Config.txt the user ID has been modfided and can't be loaded");
                idSet = false;
            }
            reader.Close();
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
                        SetTime(int.Parse(lastCommand.Split(' ')[2]));

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
                        SetTime(int.Parse(lastCommand.Split(' ')[2]));

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
                        SetTime(int.Parse(lastCommand.Split(' ')[2]));

                        string amount = lastCommand.Split(' ')[2];
                        int result;
                        if (int.TryParse(amount, out result))
                        {
                            SetAmount(result);
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

    private void SetupID(int id)
    {
        CheckBaseFiles();//Just incase some person said, LET DELETE THESE FILES AND BREAK THE MOD

        File.Delete("mods/BackUpMod/config.txt");

        StreamWriter writer = new StreamWriter("mods/BackUpMod/config.txt");

        writer.WriteLine(id);
        writer.WriteLine(timeWait);
        writer.WriteLine(amountOfBackups);
        writer.WriteLine(backupNumber);
        writer.Close();

        idSet = true;//They have set the id up

    }
    private void SetTime(int time)
    {
        if (idSet)
        {
            timeWait = time;
            Log("BackUp Mod: The time between each backup has changed to " + timeWait);
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
        }
        else
        {
            NotSetUpID();
        }

    }

    private void BackupSave(int backupNumber)
    {
        if (!idSet)
        {
            Log("BackUp Mod: Tried to back up and set ID is false, please contact Marsh.Mello on the forms");
            return;
        }

        Log("BackUp Mod: Backing Up");
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(timeWait);
        BackupSave(backupNumber);
    }

    void NotSetUpID()
    {
        Log("Please set up your steam ID first with 'backup id steamIDNumber' ");
    }
    
    public void Log(string text)
    {
        RConsole.Log(text);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F9))
        {
            Log(Semih_Network.GameSceneName);
            //Log(SceneManager.GetActiveScene().name);
        }

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

[HarmonyPatch(typeof(RGD_Game))]
class RGD_Game_Variable_Patch
{
    static void Prefix(RGD_Game __instance)
    {
        string worldName = (string)AccessTools.Field(typeof(RGD_Game), "name").GetValue(__instance);
        RConsole.Log(worldName);
    }
}