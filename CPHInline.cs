using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class Constants
{
    #region Platforms
    public const string TWITCH  = "twitch";
    public const string YOUTUBE = "youtube";
    public const string TROVO   = "trovo";
    #endregion

    #region Inputs
    public static readonly string INPUT_0 = "input0";
    public static readonly string INPUT_1 = "input1";
    public static readonly string INPUT_2 = "input2";
    #endregion

    #region Standard Variables
    public static readonly string USER_TYPE      = "userType";
    public static readonly string EVENT_SOURCE   = "eventSource";
    public static readonly string COMMAND        = "command";
    public static readonly string COMMAND_SOURCE = "commandSource";
    public static readonly string USER_ID        = "userId";
    public static readonly string USER_NAME      = "userName";
    #endregion
}

public class CPHInline
{
    private static readonly HashSet<string> _platforms = new (StringComparer.OrdinalIgnoreCase) { Constants.TWITCH, Constants.YOUTUBE/*, Constants.TROVO*/ };
    private static readonly string POINTS_VARIABLE_NAME = "points";
    //private static long CHAT_INCREMENT_POINTS = 5; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED PER CHAT MESSAGE

#region Platform Helpers

#region Triggering Platform
    private string GetPlatformTriggeringAction()
    {
        if (CPH.TryGetArg(Constants.USER_TYPE, out string userType))
        {
            if (_platforms.Contains(userType))
            {
            	CPH.LogInfo($"Using {Constants.USER_TYPE} to determine platform is {userType}.");
                return userType;
            }
        }

        if (CPH.TryGetArg(Constants.EVENT_SOURCE, out string eventSource))
        {
            if (eventSource == Constants.COMMAND)
            {
                if (CPH.TryGetArg(Constants.COMMAND_SOURCE, out string commandSource))
                {
                    if (_platforms.Contains(commandSource))
                    {
                    	CPH.LogInfo($"Using {Constants.COMMAND_SOURCE} to determine platform is {commandSource}.");
                        return commandSource;
                    }
                }
            }
            else if (_platforms.Contains(eventSource))
            {
            	CPH.LogInfo($"Using {Constants.EVENT_SOURCE} to determine platform is {eventSource}.");
                return eventSource;
            }
        }

        CPH.LogError("Unable to determine triggering platform.");
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
#endregion

#region Message

    private static readonly bool BOT_DEFAULT = true; // CHANGE THIS TO NOT USE BOT ACCOUNT BY DEFAULT
    private static readonly string BOT_VARIABLE_NAME = "bot"; // CHANGE THIS TO USE A DIFFERENT VARIABLE FOR WHETHER TO USE BOT ACCOUNT WHEN SENDING A PLATFORM MESSAGE
    private static readonly string MESSAGE_VARIABLE_NAME = "message"; // CHANGE THIS TO USE A DIFFERENT VARIABLE FOR THE MESSAGE TO SEND
    private bool GetBotPreference()
    {
        if (!CPH.TryGetArg(BOT_VARIABLE_NAME, out bool bot))
        {
            bot = BOT_DEFAULT;
        }
        return bot;
    }

    public bool SendPlatformMessage()
    {
        if (!CPH.TryGetArg(MESSAGE_VARIABLE_NAME, out string message))
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        return SendPlatformMessage(platform, message);
    }

    private bool SendPlatformMessage(string platform, string message)
    {
    	var bot = GetBotPreference();
        switch (platform)
        {
            case Constants.TWITCH:
                CPH.SendMessage(message, bot);
                return true;
            case Constants.YOUTUBE:
                CPH.SendYouTubeMessageToLatestMonitored(message, bot);
                return true;
            case Constants.TROVO:
                CPH.SendTrovoMessage(message, bot);
                return true;
        }

        return false;
    }

#endregion

#endregion

    private long? GetUserPoints(string platform, string userId)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                return GetTwitchUserPointsById(userId);
            case Constants.YOUTUBE:
                return GetYouTubeUserPointsById(userId);
            case Constants.TROVO:
                return GetTrovoUserPointsById(userId);
            default:
                return null;
        }
    }

    private void SetUserPoints(string platform, string userId, long points)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                SetTwitchUserPointsById(userId, points);
                break;
            case Constants.YOUTUBE:
                SetYouTubeUserPointsById(userId, points);
                break;
            case Constants.TROVO:
                SetTrovoUserPointsById(userId, points);
                break;
        }
    }

    public bool SendUserPointsMessage()
    {
        if (!CPH.TryGetArg(Constants.USER_ID, out string userId))
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        long? userPoints = GetUserPoints(platform, userId);
        if (userPoints == null)
        {
            return false;
        }

        if (!CPH.TryGetArg(Constants.USER_NAME, out string userName))
        {
            return false;
        }

        return SendPlatformMessage(platform, $"{userName}, you have {userPoints} points!  Use !commands to see how to spend them!");
    }

    public bool SetPoints()
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
    	{
    	    return false;
    	}

        // Remove to allow negative points (penalty points?)
        if (points < 0)
        {
            return false;
        }

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        switch (platform)
        {
            case Constants.TWITCH:
                return SetPointsTwitch(points);
            case Constants.YOUTUBE:
                return SetPointsYouTube(points);
            case Constants.TROVO:
                return SetPointsTrovo(points);
            default:
                return false;
        }
    }

    public bool AddPoints()
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
    	{
    	    return false;
    	}

        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        switch (platform)
        {
            case Constants.TWITCH:
                return AddPointsTwitch(points);
            case Constants.YOUTUBE:
                return AddPointsYouTube(points);
            case Constants.TROVO:
                return AddPointsTrovo(points);
            default:
                return false;
        }
    }

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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTwitch(points);
    }

    private bool SetPointsTwitch(long amountToSet)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUsername))
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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTwitch(points);
    }

    private bool AddPointsTwitch(long amountToAdd)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUsername))
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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTrovo(points);
    }

    private bool SetPointsTrovo(long amountToSet)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUser))
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
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTrovo(points);
    }

    private bool AddPointsTrovo(long amountToAdd)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUser))
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

    private static long DEFAULT_POINTS_PER_TICK = 10; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED PER PRESENT VIEWER SWEEP
    private static readonly string POINTS_PER_TICK_VARIABLE_NAME = "pointsGivenPerTick";
    public bool AddWatchPoints()
    {
        if (!CPH.TryGetArg("isLive", out bool live))
        {
            CPH.LogError("isLive not set.  Not adding watch points.");
            return false;
        }

        if (!live)
        {
            CPH.LogInfo("Stream is not live.  Not adding watch points.");
            return true;
        }

        if (!CPH.TryGetArg(Constants.EVENT_SOURCE, out string platform))
        {
            CPH.LogError("Unable to determine platform.  Not adding watch points.");
            return false;
        }

        if (!CPH.TryGetArg(POINTS_PER_TICK_VARIABLE_NAME, out long pointsToAdd))
        {
            pointsToAdd = DEFAULT_POINTS_PER_TICK;
        }

        //CPH.TryGetArg("users", out List<Dictionary<string,object>> users);
        List<Dictionary<string,object>> users = (List<Dictionary<string,object>>)args["users"];
        CPH.LogInfo($"[Points Admin] [Present Viewers] WARN Starting Present Viewers from {platform} with {users.Count} users.");

        long? points;
        string userId;
        for (int i = 0; i < users.Count; i++)
        {
            userId = users[i]["id"].ToString();
            points = GetUserPoints(platform, userId);
            if (points != null)
            {
                SetUserPoints(platform, userId, points.Value + pointsToAdd);
            }
        }

        CPH.LogInfo($"[Points Admin] [Present Viewers] WARN Ended Present Viewers from {platform}");
        return true;
    }

    public bool GetYouTubeTarget()
    {
        // make sure the first arg is a number
    	if (!CPH.TryGetArg(Constants.INPUT_0, out long input0))
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

#region Bot Account Helpers
    public bool SetIsBotAccount()
    {
        if (!CPH.TryGetArg(Constants.USER_NAME, out string userName))
        {
            return false;
        }

        var isBotAccount = IsBotAccount(userName);
        CPH.SetArgument("isBotAccount", isBotAccount);
        return true;
    }

    private static readonly string TWITCH_BOT_ACCOUNT_USER_NAME = "teamventuregaming";
    private static readonly string YOUTUBE_BOT_ACCOUNT_USER_NAME = "Team Venture Bot";
    private static readonly string TROVO_BOT_ACCOUNT_USER_NAME = ""; // TODO: Add Trovo bot username

    private bool IsBotAccount(string username)
    {
        var platform = GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        return IsBotAccount(username, platform);
    }

    private static bool IsBotAccount(string username, string platform)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                return StringComparer.OrdinalIgnoreCase.Equals(username, TWITCH_BOT_ACCOUNT_USER_NAME);
            case Constants.YOUTUBE:
                return StringComparer.OrdinalIgnoreCase.Equals(username, YOUTUBE_BOT_ACCOUNT_USER_NAME);
            case Constants.TROVO:
                return false;
                //return StringComparer.OrdinalIgnoreCase.Equals(username, TROVO_BOT_ACCOUNT_USER_NAME);
        }

        return false;
    }
#endregion

#region Redeems

private static readonly string UKNOWN_REDEEMER_USER_NAME = "Redeemer";
private bool RedeemSfxCommon(string redeemName, long redeemCost, Dictionary<string, string> actionLookup, string defaultAction = "random")
{
    // TODO: get list of enabled actions based on the lookup
    // TODO: if none are enabled, send a message and return false

    if (!CPH.TryGetArg(Constants.USER_ID, out string userId))
    {
        CPH.LogError("No user ID found.");
        return false;
    }

    var platform = GetPlatformTriggeringAction();
    if (platform == null)
    {
        CPH.LogError("No platform found.");
        return false;
    }

    long? userPoints = GetUserPoints(platform, userId);
    if (userPoints == null)
    {
        CPH.LogError("No user points found.");
        return false;
    }

    if (!CPH.TryGetArg(Constants.USER_NAME, out string userName))
    {
        userName = UKNOWN_REDEEMER_USER_NAME;
        CPH.LogInfo($"No user name found, using {userName} as default.");
    }

    if (!CPH.TryGetArg(Constants.INPUT_0, out string sfxKey) || String.IsNullOrWhiteSpace(sfxKey))
    {
        sfxKey = defaultAction;
        CPH.LogInfo($"No action name found, using `{sfxKey}` as default.");
    }

    if (!actionLookup.TryGetValue(sfxKey, out string action))
    {
        // TODO: may need to split this list into multiple messages
        var sfxKeyList = string.Join(", ", actionLookup.Keys);
        SendPlatformMessage(platform, $"{redeemName} costs {redeemCost} points and when used by itself picks a random clip.");
        SendPlatformMessage(platform, $"You can also specify one of these: {sfxKeyList}.");
        return true;
    }

    if (userPoints < redeemCost)
    {
        SendPlatformMessage(platform, $"{userName}, {redeemName} requires {redeemCost}, but you only have {userPoints}.");
        return false;
    }

    SetUserPoints(platform, userId, userPoints.Value - redeemCost);

    CPH.SetArgument("redeemActionName", action);
    return CPH.RunAction(action, false);
}

#region Smash
    private static long SMASH_REDEEM_COST = 50; // CHANGE THIS TO SET THE COST OF REDEEMING A SMASH
    private static Dictionary<string, string> smashActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "defeated",    "[Smash SFX] - Defeated" },
        { "fight",       "[Smash SFX] - Fight" },
        { "finishhim",   "[Smash SFX] - Finish Him" },
        { "letsgo",      "[Smash SFX] - Lets Go" },
        { "roundone",    "[Smash SFX] - Round One" },
        { "stupendous",  "[Smash SFX] - Stupendous" },
        { "suddendeath", "[Smash SFX] - Sudden Death" },
        { "teamventure", "[Smash SFX] - Team Venture" },
        { "thewinneris", "[Smash SFX] - The Winner Is" },
        { "traderjen",   "[Smash SFX] - Trader Jen" },
        { "victory",     "[Smash SFX] - Victory" },
        { "welcome",     "[Smash SFX] - Welcome" },
        { "zombie",      "[Smash SFX] - Zombie" },
        { "random",      "[Smash SFX] - Random" } // TODO: pick a random song from the enabled list instead of this
    };

    public bool RedeemSmash()
    {
        return RedeemSfxCommon("!smash", SMASH_REDEEM_COST, smashActionLookup);
    }
#endregion

#region Song
    private static long SONG_REDEEM_COST = 250; // CHANGE THIS TO SET THE COST OF REDEEMING A SONG
    private static Dictionary<string, string> songActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "christmastrash", "[Song Bite] - Christmas Trash" },
        { "diggin",         "[Song Bite] - Won't Stop Diggin" },
        { "digginend",      "[Song Bite] - Won't Stop Diggin Finale" },
        { "digtrash",       "[Song Bite] - Dig In Trash" },
        { "dodgebites",     "[Song Bite] - Dodge Bites" },
        { "dodgingzombies", "[Song Bite] - Dodging Zombies" },
        { "morninglight",   "[Song Bite] - Morning Light" },
        { "shoveltrash",    "[Song Bite] - Shovel in the Trash" },
        { "tvwin",          "[Song Bite] - Team Venture for the Win" },
        { "random",         "[Song Bite] - Random" } // TODO: pick a random song from the enabled list instead of this
    };

    public bool RedeemSong()
    {
        return RedeemSfxCommon("!song", SONG_REDEEM_COST, songActionLookup);
    }
#endregion

#endregion
}
