using System;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using Steamworks;

public class BackUp : Mod
{
    public BackUp() : base("BackUp", "A mod which can help you create backups of your worlds and easily revert over to them if something go wrong.", "1.2.1", "1.02") { }

    int timeWait = 300; //This is in seconds and the default(300) is 5 minutes
    bool idSet; //if they have set the ID in the config file yet or not
    ulong usersID;
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
            ulong result;
            if (ulong.TryParse(id, out result))
            {
                usersID = result;
                idSet = true;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 1, going to re find your ID");
                idSet = false;
                FindID();
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
            reader.Close();
            Log("BackUp Mod: Loaded Config.txt");
        }
        else
        {
            FindID();
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
                        Log("backup time TimePerBackUpInSeconds");
                    }
                    
                }
                else if (lastCommand.Split(' ')[1] == "now")
                {
                    BackupSave();
                }
                else if (lastCommand.Split(' ')[1] == "revert")
                {
                    if (lastCommand.Split(' ').Length > 2)
                    {
                        if (lastCommand.Split(' ').Length == 3)
                        {
                            LoadFromBackup(lastCommand.Split(' ')[2]);
                        }
                        else
                        {
                            string revertWorldName = lastCommand.Split(' ')[2];
                            for (int i = 0; i < lastCommand.Split(' ').Length - 3; i++)
                            {
                                Log(i + lastCommand.Split(' ')[i + 3]);
                                revertWorldName = revertWorldName + " " + lastCommand.Split(' ')[i + 3];
                                Log(revertWorldName);
                                Log(lastCommand.Split(' ').Length - 4 + "");
                                if (i == lastCommand.Split(' ').Length - 4)//This should be the end of the world name
                                {
                                    Log("reverting");
                                    LoadFromBackup(revertWorldName);
                                }
                            }
                        }

                    }

                    if (lastCommand.Split(' ').Length == 2)
                    {
                        Log("Error - revert takes 1 extras");
                        Log("backup revert worldname");
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
        Log(" ");
        Log("time");
        Log("time TimeToWaitInSeconds, time is to set a custom time when every backup is made, by default it is every 5 minutes");
        Log("");
        Log("help");
        Log("help displays this help messages");
        Log("");
        Log("now");
        Log("now, now performs a backup");
        Log("");
        Log("revert");
        Log("revert worldName, this is how you revert to a backup you must be in the main menu to use it");
    }

    private void FindID()//Thanks to TeKGameR for this
    {
        Semih_Network network = FindObjectOfType<Semih_Network>();
        FieldInfo field = Enumerable.FirstOrDefault<FieldInfo>(typeof(Semih_Network).GetFields((BindingFlags)36), (FieldInfo x) => x.Name == "localSteamID");
        CSteamID steamid = (CSteamID)field.GetValue(network);
        usersID = steamid.m_SteamID;
        idSet = true;

        Log("Backup Mod:ID was found and set!");
        UpdateConfigFile();
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

    private void BackupSave()
    {
        if (!worldLoaded)
        {
            Log("BackUp Mod: Please be in a world before you backup");
            return;//Not to backup if in main menu
        }
        UpdateWorldName();
        Log("BackUp Mod: Backing Up");
        try
        {
            if (!Directory.Exists("mods/BackUpMod"))
            {
                Directory.CreateDirectory("mods/BackUpMod");
            }

            if (File.Exists("mods/BackUpMod/" + worldName + ".rgd"))//If its already saved, we are deleting it to override it
            {
                File.Delete("mods/BackUpMod/" + worldName + ".rgd");
            }

            UnityEngine.Object.FindObjectOfType<PauseMenu>().SaveGame();//Save to the file first

            File.Copy(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "/LocalLow/Redbeet Interactive/Raft/User/User_" + usersID + "/World/" + worldName + ".rgd", "mods/BackUpMod/" + worldName + ".rgd");

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
            BackupSave();
        }
        StartCoroutine(Timer());
        
    }

    void NotSetUpID()
    {
        Log("ID seemed to be missing, researching");
        FindID();
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


    private void LoadFromBackup(string name)
    {
        if (!idSet)//If their ID is not set yet, tell them to set it
        {
            NotSetUpID();
            return;
        }

        if (worldLoaded)
        {
            Log("Please return to the main menu to revert a world.");
            return;
        }

        if (!File.Exists("mods/BackUpMod/" + name + ".rgd"))
        {
            Log("BackUp Mod: Can't seem to find any backups for that world, Sorry");
            return;
        }
        else
        {
            Log("Reverting world called " + name);

            try
            {
                File.Delete(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "/LocalLow/Redbeet Interactive/Raft/User/User_" + usersID + "/World/" + name + ".rgd");
            }
            catch (Exception e)
            {
                Log("BackUp Mod: Tried to delete old world, but there was an error, revert stopped");
                Log(e.ToString());
                return;
            }

            try
            {
                File.Move("mods/BackUpMod/" + name + ".rgd", Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "/LocalLow/Redbeet Interactive/Raft/User/User_" + usersID + "/World/" + name + ".rgd");
            }
            catch (Exception e)
            {
                Log("BackUp Mod: Tried to move backup world, but there was an error, revert stopped");
                Log(e.ToString());
                return;
            }

            Log("BackUp Mod: Revert Completed");
        }
    }
}