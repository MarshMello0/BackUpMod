using System;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using Steamworks;

[ModTitle("BackUp")]
[ModDescription("A mod which can help you create backups of your worlds and easily revert over to them if something go wrong.")]
[ModAuthor(". Marsh.Mello .")]
[ModVersion("1.2.3")]
[RaftVersion("1.03B")]
public class BackUp : Mod
{
    int timeWait = 300; //This is in seconds and the default(300) is 5 minutes
    ulong usersID;
    bool worldLoaded = false;
    private string worldsPath;
    private string modsPath = "mods/BackUpMod";
    private string configPath = "mods/BackUpMod/config.txt";

    void Start()//This runs at the start
    {
        CreateCommands();//Sets up the commands
        CheckBaseFiles();//Checks the users files
    }

    void Update()//This update runs every tick and checks if the player is in a world
    {
        if (SceneManager.GetActiveScene().name == "MainScene" && !worldLoaded)
        {
            worldLoaded = true;
            StartCoroutine(Timer());//Starts off the timer
        }
        else if (SceneManager.GetActiveScene().name != "MainScene" && worldLoaded)
        {
            worldLoaded = false;
        }
    }

    void CheckBaseFiles()
    {
        if (!Directory.Exists(modsPath))//Checks if the main folder exists
        {
            Log("BackUp Mod: Base folder was missing so I created a new one");
            Directory.CreateDirectory(modsPath);
        }
        if (File.Exists(configPath))//Checks if the config file exists
        {
            StreamReader reader = new StreamReader(configPath);
            string id = reader.ReadLine();
            ulong result;
            if (ulong.TryParse(id, out result))
            {
                usersID = result;
            }
            else
            {
                Log("BackUp Mod: Error loading config.txt on line 1, going to re find your ID");
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
            worldsPath = GetWorldsPath();
        }
        else
        {
            FindID();
        }
    }

    private void FindID()//Thanks to TeKGameR for this 
    {
        Semih_Network network = FindObjectOfType<Semih_Network>();
        FieldInfo field = Enumerable.FirstOrDefault<FieldInfo>(typeof(Semih_Network).GetFields((BindingFlags)36), (FieldInfo x) => x.Name == "localSteamID");
        CSteamID steamid = (CSteamID)field.GetValue(network);
        usersID = steamid.m_SteamID;

        Log("Backup Mod:ID was found and set!");
        UpdateConfigFile();
        worldsPath = GetWorldsPath();
    }//This finds the users steam ID

    private void CreateCommands()//Creates the commands
    {
        RConsole.registerCommand("backup", "Use backup help for help about BackUp", "backup", CheckCommands);
        
    }

    private string GetWorldsPath()//This returns the location of the worlds in the Appdata folder
    {
        return Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "/LocalLow/Redbeet Interactive/Raft/User/User_" + usersID + "/World";
    }

    private void CheckCommands()//This is where it checks which command was called
    {
        string[] lastCommand = RConsole.lastCommands.LastOrDefault().Split(' ');//stores all the last commands in an array

        string caseSwitch = lastCommand[1];//Checks the first one only

        switch(caseSwitch)//send the rest of the commands to functions
        {
            case "help":
                Help();
                break;
            case "time":
                SetTime(lastCommand);
                break;
            case "now":
                BackUpNow();
                break;
            case "revert":
                RevertWorld(lastCommand);
                break;                
        }

    }

    private void SetTime(string[] args)//sets the wait time
    {
        if (args.Length >= 2)
        {
            string amount = args[2];
            int result;
            if (int.TryParse(amount, out result))
            {
                timeWait = result;
                Log("BackUp Mod: The time between each backup has changed to " + timeWait);
                UpdateConfigFile();
            }
        }
        else
        {
            Log("Error");
            Log("backup time TimePerBackUpInSeconds");
        }
    }

    private void BackUpNow()//Backs up the world
    {
        if (!worldLoaded)
        {
            Log("BackUp Mod: Please be in a world before you backup");
            return;//Not to backup if in main menu
        }

        Log("BackUp Mod: Backing Up");
        try
        {
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
            }

            if (File.Exists(modsPath + "/" + WorldName() + ".rgd"))//If its already saved, we are deleting it to override it
            {
                File.Delete(modsPath + "/" + WorldName() + ".rgd");
            }

            UnityEngine.Object.FindObjectOfType<PauseMenu>().SaveGame();//Save to the file first

            File.Copy(worldsPath + "/" + WorldName() + ".rgd", modsPath + "/" + WorldName() + ".rgd");

        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
        Log("BackUp Mod: Finished Backing up");
    }

    private void RevertWorld(string[] args)//This first checks if the args they put at the end are correct for revert
    {
        if (args.Length > 2)
        {
            if (args.Length == 3)
            {
                LoadFromBackup(args[2]);
            }
            else
            {
                string revertWorldName = args[2];
                for (int i = 0; i < args.Length - 3; i++)
                {
                    revertWorldName = revertWorldName + " " + args[i + 3];
                    if (i == args.Length - 4)
                    {
                        LoadFromBackup(revertWorldName);//If they are it will move the world file
                    }
                }
            }

        }

        if (args.Length == 2)
        {
            Log("Error - revert takes 1 extras");
            Log("backup revert worldname");
        }
    }

    private void LoadFromBackup(string name)//Moves the backup file into the worlds folder in appdata
    {
        if (worldLoaded)
        {
            Log("Please return to the main menu to revert a world.");
            return;
        }

        if (!File.Exists(modsPath + "/" + name + ".rgd"))
        {
            Log("BackUp Mod: Can't seem to find any backups for that world, Sorry");
            return;
        }
        else
        {
            Log("Reverting world called " + name);

            try
            {
                File.Delete(worldsPath + "/" + name + ".rgd");
            }
            catch (Exception e)
            {
                Log("BackUp Mod: Tried to delete old world, but there was an error, revert stopped");
                Log(e.ToString());
                return;
            }

            try
            {
                File.Move("mods/BackUpMod/" + name + ".rgd", worldsPath + "/" + name + ".rgd");
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


    void UpdateConfigFile()//Updates the config file which has usersID and timewait
    {
        if (File.Exists(configPath)) File.Delete(configPath);

        StreamWriter writer = new StreamWriter(configPath);

        writer.WriteLine(usersID);
        writer.WriteLine(timeWait);
        writer.Close();

        Log("BackUp Mod: Updated Config.txt");
    }

    private void Log(string text)//Just an easier function to call instead of RConsole.Log
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

    private void Help()//Displays all of the commands when backup help is called
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

    private string WorldName()//returns the worldname when its needed
    {
        return SaveAndLoad.CurrentGameFileName;
    }

    private IEnumerator Timer()//The timer which triggers the backup
    {
        yield return new WaitForSeconds(timeWait);
        if (worldLoaded)//the timer will only continue if the user is in a world
        {
            BackUpNow();
            StartCoroutine(Timer());
        }
    }
}
