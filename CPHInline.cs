using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private static readonly string INPUT_0 = "input0";
    private static readonly string INPUT_1 = "input1";
    private static readonly string POINTS_VARIABLE_NAME = "points";
    private static readonly HashSet<string> _platforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "twitch", "youtube", "trovo" };
    //private static long CHAT_INCREMENT_POINTS = 5; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED PER CHAT MESSAGE
    private static long DEFAULT_POINTS_PER_TICK = 10; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED PER PRESENT VIEWER SWEEP
    private static long SMASH_REDEEM_COST = 50; // CHANGE THIS TO SET THE COST OF REDEEMING A SMASH
    private static long SONG_REDEEM_COST = 250; // CHANGE THIS TO SET THE COST OF REDEEMING A SONG

    public string GetPlatformTriggeringAction()
    {
        if (CPH.TryGetArg("userType", out string userType))
        {
            if (_platforms.Contains(userType))
            {
                return userType;
            }
        }

        if (CPH.TryGetArg("eventSource", out string eventSource))
        {
            if (eventSource == "command")
            {
                if (CPH.TryGetArg("commandSource", out string commandSource))
                {
                    if (_platforms.Contains(commandSource))
                    {
                        return commandSource;
                    }
                }
            }
            else if (_platforms.Contains(eventSource))
            {
                return eventSource;
            }
        }

        return null;
    }

    public bool SetPlatformTriggeringAction()
    {
        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        CPH.SetArgument("triggeringPlatform", platform);
        return true;
    }

    public bool GetUserPoints()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        long? userPoints = GetUserPoints(userId, platform);
        if (userPoints == null)
        {
            return false;
        }

        if (!CPH.TryGetArg("userName", out string userName))
        {
            return false;
        }

        if (!CPH.TryGetArg("bot", out bool bot))
        {
            bot = true;
        }

        return SendPlatformMessage(platform, $"{userName}, you have {userPoints} points", bot);
    }

    public bool SetPoints()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
    	  {
    		    return false;
    	  }

        return SetPoints(points, GetPlatformTriggeringAction());
    }

    private bool SetPoints(long points, string platform)
    {
        // Remove to allow negative points (penalty points?)
        if (points < 0)
        {
            return false;
        }

        switch (platform)
        {
            case "twitch":
                return SetPointsTwitch(points);
            case "youtube":
                return SetPointsYouTube(points);
            case "trovo":
                return SetPointsTrovo(points);
            default:
                return false;
        }
    }

    public bool AddPoints()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
    	  {
    		    return false;
    	  }

        return AddPoints(points, GetPlatformTriggeringAction());
    }

    private bool AddPoints(long points, string platform)
    {
        switch (platform)
        {
            case "twitch":
                return AddPointsTwitch(points);
            case "youtube":
                return AddPointsYouTube(points);
            case "trovo":
                return AddPointsTrovo(points);
            default:
                return false;
        }
    }

    // public bool AddChatPoints()
    // {
    //     if (!CPH.TryGetArg("userName", out string userName))
    //     {
    //         return false;
    //     }
    //
    //     var platform = GetPlatformTriggeringAction();
    //     switch (platform)
    //     {
    //         case "twitch":
    //             return AddPointsTwitch(CHAT_INCREMENT_POINTS, userName);
    //         case "youtube":
    //             return AddPointsYouTube(CHAT_INCREMENT_POINTS, userName);
    //         case "trovo":
    //             return AddPointsTrovo(CHAT_INCREMENT_POINTS, userName);
    //         default:
    //             return false;
    //     }
    // }

#region Twitch
    private void SetTwitchUserPointsById(string userId, long points)
    {
        CPH.SetTwitchUserVarById(userId, POINTS_VARIABLE_NAME, points, true);
    }

    private long? GetTwitchUserPointsById(string userId)
    {
        return CPH.GetTwitchUserVarById<long?>(userId, POINTS_VARIABLE_NAME, true);
    }

    public bool SetPointsTwitch()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTwitch(points);
    }

    private bool SetPointsTwitch(long amountToSet)
    {
        if (!CPH.TryGetArg(INPUT_1, out string targetUsername))
        {
            return false;
        }

        return SetPointsTwitch(amountToSet, targetUsername);
    }

    private bool SetPointsTwitch(long amountToSet, string targetUsername)
    {
        var targetUserInfo = CPH.TwitchGetUserInfoByLogin(targetUsername);
        if (targetUserInfo == null)
        {
            CPH.LogError($"Unable to get Twitch User Info for {targetUsername}");
            return false;
        }

        SetTwitchUserPointsById(targetUserInfo.UserId, amountToSet);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("points", amountToSet);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", targetUsername);
        CPH.LogInfo($"[Points Admin] [Set Points] Twitch - Set {targetUsername}({targetUserInfo.UserId}) by {addedBy} => {amountToSet}");

        return true;
    }

    public bool AddPointsTwitch()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTwitch(points);
    }

    private bool AddPointsTwitch(long amountToAdd)
    {
        if (!CPH.TryGetArg(INPUT_1, out string targetUsername))
        {
            return false;
        }

        return AddPointsTwitch(amountToAdd, targetUsername);
    }

    private bool AddPointsTwitch(long amountToAdd, string targetUsername)
    {
        var targetUserInfo = CPH.TwitchGetUserInfoByLogin(targetUsername);
        if (targetUserInfo == null)
        {
            CPH.LogError($"Unable to get Twitch User Info for {targetUsername}");
            return false;
        }

        long? currentPoints = GetTwitchUserPointsById(targetUserInfo.UserId);
        long newPoints = (currentPoints ?? 0) + amountToAdd;
        SetTwitchUserPointsById(targetUserInfo.UserId, newPoints);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("newPoints", newPoints);
        CPH.SetArgument("oldPoints", currentPoints);
        CPH.SetArgument("pointsAdded", amountToAdd);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", targetUsername);
        CPH.LogInfo($"[Points Admin] [Add Points] Twitch - Added To {targetUsername} by {addedBy} => {currentPoints} + {amountToAdd} = {newPoints}");
        
        return true;
    }
#endregion

#region YouTube
    private void SetYouTubeUserPointsById(string userId, long points)
    {
        CPH.SetYouTubeUserVarById(userId, POINTS_VARIABLE_NAME, points, true);
    }

    private long? GetYouTubeUserPointsById(string userId)
    {
        return CPH.GetYouTubeUserVarById<long?>(userId, POINTS_VARIABLE_NAME, true);
    }

    public bool SetPointsYouTube()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsYouTube(points);
    }

    private bool SetPointsYouTube(long amountToSet)
    {
        if (!GetYouTubeTarget())
        {
            return false;
        }

        if (!CPH.TryGetArg("usersFound", out int usersFound) || usersFound == 0)
        {
            return false;
        }

        CPH.TryGetArg("targetUserName", out string target);
        for (int i = 0; i < usersFound; i++)
        {
            CPH.TryGetArg($"targetFoundUserId{i}", out string userId);

            SetYouTubeUserPointsById(userId, amountToSet);

            //Set Arguments
            CPH.SetArgument("target", target);
            CPH.SetArgument("points", amountToSet);
            CPH.LogInfo($"[Points Admin] [Set Points] YouTube - {target}/{userId} => {amountToSet}");
        }

        return true;
    }

    public bool AddPointsYouTube()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsYouTube(points);
    }

    public bool AddPointsYouTube(long amountToAdd)
    {
        if (!GetYouTubeTarget())
        {
            return false;
        }

        if (!CPH.TryGetArg("usersFound", out int usersFound) || usersFound == 0)
        {
            return false;
        }

        CPH.TryGetArg("targetUserName", out string target);
        int failCount = 0;
        long newPoints = 0; 
        for (int i = 0; i < usersFound ; i++)
        {
        	  CPH.TryGetArg($"targetFoundUserId{i}", out string userId);

            long? currentPoints = GetYouTubeUserPointsById(userId);
            if (currentPoints != null)
            {
                newPoints = currentPoints.Value + amountToAdd;
                SetYouTubeUserPointsById(userId, newPoints);
                //Log Info
                CPH.LogInfo($"[Points Admin] [Add Points] YouTube - {target}/{userId} => {currentPoints} + {amountToAdd} = {newPoints}");
            }
            else
            {
                failCount++;
            }
        }

        if (failCount == usersFound)
        {
            CPH.LogError($"[Points Admin] [Add Points] YouTube - All found users had non numeric points values. Stopping action.");
        }
        else
        {
            //Set Arguments
            CPH.SetArgument("target", target);
            CPH.SetArgument("pointsToAdd", amountToAdd);
            CPH.SetArgument("newPoints", newPoints);
        }
        return failCount < usersFound;
    }
#endregion

#region Trovo
    private void SetTrovoUserPointsById(string userId, long points)
    {
        CPH.SetTrovoUserVar(userId, POINTS_VARIABLE_NAME, points, true);
    }

    private long? GetTrovoUserPointsById(string userId)
    {
        return CPH.GetTrovoUserVarById<long?>(userId, POINTS_VARIABLE_NAME, true);
    }

    public bool SetPointsTrovo()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTrovo(points);
    }

    private bool SetPointsTrovo(long amountToSet)
    {
        if (!CPH.TryGetArg(INPUT_1, out string targetUser))
        {
            return false;
        }

        return SetPointsTrovo(amountToSet, targetUser);
    }

    private bool SetPointsTrovo(long amountToSet, string targetUser)
    {
        SetTrovoUserPointsById(targetUser, amountToSet);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("points", amountToSet);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", targetUser);
        CPH.LogInfo($"[Points Admin] [Add Points] Trovo - Set To {targetUser} by {addedBy} => {amountToSet}");
        return true;
    }

    public bool AddPointsTrovo()
    {
        if (!CPH.TryGetArg(INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTrovo(points);
    }

    private bool AddPointsTrovo(long amountToAdd)
    {
        if (!CPH.TryGetArg(INPUT_1, out string targetUser))
        {
            return false;
        }

        return AddPointsTrovo(amountToAdd, targetUser);
    }

    private bool AddPointsTrovo(long amountToAdd, string targetUser)
    {
        long? currentPoints = GetTrovoUserPointsById(targetUser);
        long newPoints = (currentPoints ?? 0) + amountToAdd;
        SetTrovoUserPointsById(targetUser, newPoints);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("newPoints", newPoints);
        CPH.SetArgument("oldPoints", currentPoints);
        CPH.SetArgument("pointsAdded", amountToAdd);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", targetUser);
        CPH.LogInfo($"[Points Admin] [Add Points] Twitch - Added To {targetUser} by {addedBy} => {currentPoints} + {amountToAdd} = {newPoints}");
        return true;
    }
#endregion

    public bool ResetAllUserPoints()
    {
        CPH.UnsetAllUsersVar(POINTS_VARIABLE_NAME, true);
        CPH.LogInfo("[Points Admin] [Points Resetter] Points have been reset");
        return true;
    }

    public bool AddWatchPoints()
    {
        if (!CPH.TryGetArg("pointsGivenPerTick", out long pointsToAdd))
        {
            pointsToAdd = DEFAULT_POINTS_PER_TICK;
        }
        CPH.TryGetArg("isLive", out bool live);
        CPH.TryGetArg("eventSource", out string platform);

        //List<Dictionary<string,object>> users = null;
        //CPH.TryGetArg("users", out List<Dictionary<string,object>> users);
        List<Dictionary<string,object>> users = (List<Dictionary<string,object>>)args["users"];
        //CPH.LogInfo(platform +" "+ users.Count);
        CPH.LogInfo($"[Points Admin] [Present Viewers] WARN Starting Present Viewers from {platform}");
        long? points;
        string userId;
        if (live)
        {
			      for (int i = 0; i < users.Count; i++)
            {
                userId = users[i]["id"].ToString();

                points = GetUserPoints(userId, platform);
                if (points != null)
                {
                    SetUserPoints(userId, platform, points.Value + pointsToAdd);
                }
            }
        }

        CPH.LogInfo($"[Points Admin] [Present Viewers] WARN Ended Present Viewers from {platform}");
        return true;
    }
    
    private long? GetUserPoints(string userId, string platform)
    {
        switch (platform)
        {
            case "twitch":
                return GetTwitchUserPointsById(userId) ?? 0;
            case "youtube":
                return GetYouTubeUserPointsById(userId) ?? 0;
            case "trovo":
                return GetTrovoUserPointsById(userId) ?? 0;
            default:
                return null;
        }
    }
    
    private void SetUserPoints(string userId, string platform, long points)
    {
        switch (platform)
        {
            case "twitch":
                SetTwitchUserPointsById(userId, points);
                break;
            case "youtube":
                SetYouTubeUserPointsById(userId, points);
                break;
            case "trovo":
                SetTrovoUserPointsById(userId, points);
                break;
        }
    }

    public bool SendPlatformMessage()
    {
        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        if (!CPH.TryGetArg("message", out string message))
        {
            return false;
        }

        if (!CPH.TryGetArg("bot", out bool bot))
        {
            bot = true;
        }

        return SendPlatformMessage(platform, message, bot);
    }

    private bool SendPlatformMessage(string platform, string message, bool bot = true)
    {
        switch (platform)
        {
            case "twitch":
                CPH.SendMessage(message, bot);
                return true;
            case "youtube":
                CPH.SendYouTubeMessageToLatestMonitored(message, bot);
                return true;
            case "trovo":
                CPH.SendTrovoMessage(message, bot);
                return true;
        }

        return false;
    }

    public bool GetYouTubeTarget()
    {
        // make sure the first arg is a number
    	  if (!CPH.TryGetArg(INPUT_0, out long input0))
    	  {
    		    return false;
    	  }

        //This is pulling in the Info Given
        CPH.TryGetArg("rawInput", out string rawInput);
        if (!CPH.TryGetArg("inputsTaken", out long inputsToRemove))
        {
            inputsToRemove = 1;
        }

        //This Removes Inputs we dont need
        string textRemove = "";
        for (int i = 0; i < inputsToRemove; i++)
        {
            CPH.TryGetArg("input" + i, out string moreInput);
            textRemove += moreInput;
        }

        //This now works out the username we are trying to find
        string youtubeUserName = rawInput.Remove(0, textRemove.Length + ((int)inputsToRemove + 1 - 1));
        if (youtubeUserName[0] == '@')
        {
            youtubeUserName = youtubeUserName.Remove(0, 1);
        }

        //Sets the Argument
        CPH.SetArgument("targetUserName", youtubeUserName);

        //Logs the Info
        CPH.LogInfo($"[Points Admin] [Add Points] YouTube - Text Removed: {textRemove} => User To Hande: {youtubeUserName} ");

        //Creates a New List for the found users to go into.
        var userList = new List<Tuple<string, string>>();
        //This pulls all the users who have Points
        List<UserVariableValue<string>> userPointsList = CPH.GetYouTubeUsersVar<string>("points", true);
        //This then goes through each one and works out if we have a match.
        foreach (var userValue in userPointsList)
        {
            string username = userValue.UserLogin;
            if (StringComparer.OrdinalIgnoreCase.Equals(youtubeUserName, username))
            {
                //check wether the value is valid for long
                if (Int64.TryParse(userValue.Value, out _))
                {
                    userList.Add(new Tuple<string, string>(username, userValue.UserId));
                    CPH.LogInfo($"[Points Admin] YouTube - {username} with the ID  of {userValue.UserId} has been found");
                }
                else
                {
                    CPH.LogError($"[Points Admin] YouTube - {username} with the ID  of {userValue.UserId} has a not numeric points value. Needs to be fixed.");
                }
            }
        }

        //This Goes through list and sets arguments.
        for (int d = 0; d < userList.Count; d++)
        {
            CPH.SetArgument("targetFoundUserName" + d, userList[d].Item1);
            CPH.SetArgument("targetFoundUserId" + d, userList[d].Item2);
        }

        //This Just saves the count and logs info!
        CPH.SetArgument("usersFound", userList.Count);
        CPH.LogInfo($"[Points Admin] [Add Points] YouTube - {userList.Count} users have been found!");
        return true;
    }

    public bool SetIsBotAccount()
    {
        if (!CPH.TryGetArg("userName", out string userName))
        {
            return false;
        }

        var isBotAccount = IsBotAccount(GetPlatformTriggeringAction(), userName);
        CPH.SetArgument("isBotAccount", isBotAccount);
        return true;
    }

    private bool IsBotAccount(string platform, string username)
    {
        switch (platform)
        {
            case "twitch":
                return StringComparer.OrdinalIgnoreCase.Equals(username, "teamventuregaming");
            case "youtube":
                return StringComparer.OrdinalIgnoreCase.Equals(username, "Team Venture Bot");
            case "trovo":
                return false; // TODO: Add Trovo bot username
        }

        return false;
    }

    public bool RedeemSmash()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        long? userPoints = GetUserPoints(userId, platform);
        if (userPoints == null)
        {
            return false;
        }

        if (!CPH.TryGetArg("userName", out string userName))
        {
            userName = "Redeemer";
        }

        if (!CPH.TryGetArg("bot", out bool bot))
        {
            bot = true;
        }

        if (!CPH.TryGetArg(INPUT_0, out string smashName) || String.IsNullOrWhiteSpace(smashName))
        {
            smashName = "random";
        }

        var actionLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "defeated", "Defeated" },
            { "fight", "Fight" },
            { "finishhim", "Finish Him" },
            { "letsgo", "Lets Go" },
            { "random", "Random" },
            { "roundone", "Round One" },
            { "stupendous", "Stupendous" },
            { "suddendeath", "Sudden Death" },
            { "teamventure", "Team Venture" },
            { "thewinneris", "The Winner Is" },
            { "traderjen", "Trader Jen" },
            { "victory", "Victory" },
            { "zombie", "Zombie" }
        };

        if (!actionLookup.TryGetValue(smashName, out string action))
        {
            var smashTriggerList = string.Join(", ", actionLookup.Keys);
            SendPlatformMessage(platform, "!smash by itself picks a random announcement.", bot);
            SendPlatformMessage(platform, $"You can also specify one of these: {smashTriggerList}.", bot);
            return true;
        }

        if (userPoints < SMASH_REDEEM_COST)
        {
            SendPlatformMessage(platform, $"{userName}, Smash requires {SMASH_REDEEM_COST}, but you only have {userPoints}.", bot);
            return false;
        }

        SetUserPoints(userId, platform, userPoints.Value - SMASH_REDEEM_COST);

        return RedeemSmash(action);
    }

    private bool RedeemSmash(string smashName)
    {
        var smashActionName = $"[Smash SFX] - {smashName}";
        CPH.SetArgument("smashActionName", smashActionName);
        return CPH.RunAction(smashActionName, false);
    }

    public bool RedeemSong()
    {
        if (!CPH.TryGetArg("userId", out string userId))
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        long? userPoints = GetUserPoints(userId, platform);
        if (userPoints == null)
        {
            return false;
        }

        if (!CPH.TryGetArg("userName", out string userName))
        {
            userName = "Redeemer";
        }

        if (!CPH.TryGetArg("bot", out bool bot))
        {
            bot = true;
        }

        if (!CPH.TryGetArg(INPUT_0, out string songName) || String.IsNullOrWhiteSpace(songName))
        {
            songName = "random";
        }

        var actionLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "brewfreedom", "Brewing Freedom" },
            { "digtrash", "Dig In Trash" },
            { "dodgingzombies", "Dodging Zombies" },
            { "random", "Random" },
            { "shoveltrash", "Shovel in the Trash" },
            { "tvwin", "Team Venture for the Win" },
            { "diggin", "Won't Stop Diggin" },
            { "digginend", "Won't Stop Diggin Finale" }
        };

        if (!actionLookup.TryGetValue(songName, out string action))
        {
            var songTriggerList = string.Join(", ", actionLookup.Keys);
            var howToUseSong = $"!song by itself picks a random song or you can specify one of these: {songTriggerList}.";
            SendPlatformMessage(platform, howToUseSong, bot);
            return true;
        }

        if (userPoints < SONG_REDEEM_COST)
        {
            SendPlatformMessage(platform, $"{userName}, Song requires {SONG_REDEEM_COST}, but you only have {userPoints}.", bot);
            return false;
        }

        SetUserPoints(userId, platform, userPoints.Value - SONG_REDEEM_COST);

        return RedeemSong(action);
    }

    private bool RedeemSong(string songName)
    {
        var songActionName = $"[Song Bite] - {songName}";
        CPH.SetArgument("songActionName", songActionName);
        return CPH.RunAction(songActionName, false);
    }
}
