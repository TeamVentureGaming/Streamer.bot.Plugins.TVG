using System;
using System.Collections.Generic;
using System.Linq;

public static class Constants
{
    #region Platforms
    public const string TWITCH  = "twitch";
    public const string YOUTUBE = "youtube";
    public const string TROVO   = "trovo";
    #endregion

    #region Inputs
    public static readonly string RAW_INPUT = "rawInput";

    public static readonly string INPUT_0 = "input0";
    public static readonly string INPUT_1 = "input1";
    public static readonly string INPUT_2 = "input2";

    public static string GetInputString(int inputNumber)
    {
        return $"input{inputNumber}";
    }

    public static bool TryGetInput<T>(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, int inputNumber, out T value)
    {
        if (CPH.TryGetArg(GetInputString(inputNumber), out value))
        {
            return true;
        }

        return false;
    }
    #endregion

    #region Standard Variables
    public static readonly string USER_TYPE      = "userType";
    public static readonly string EVENT_SOURCE   = "eventSource";
    public static readonly string COMMAND        = "command";
    public static readonly string COMMAND_SOURCE = "commandSource";
    public static readonly string USER_ID        = "userId";
    public static readonly string USER_NAME      = "userName";

    public static bool TryGetUserName(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, out string userName)
    {
        if (CPH.TryGetArg(USER_NAME, out userName))
        {
            return true;
        }

        return false;
    }

    public static bool TryGetUserId(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, out string userId)
    {
        if (CPH.TryGetArg(USER_ID, out userId))
        {
            return true;
        }

        return false;
    }
    #endregion
}

public static class PlatformExtensions
{
    #region Triggering Platform
    private static readonly HashSet<string> _platforms = new (StringComparer.OrdinalIgnoreCase) { Constants.TWITCH, Constants.YOUTUBE, Constants.TROVO };

    public static string? GetPlatformTriggeringAction(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH)
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

    public static bool SavePlatformTriggeringActionToArgument(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string argumentName = "triggeringPlatform")
    {
        var platform = GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            return false;
        }

        CPH.SetArgument(argumentName, platform);
        return true;
    }
    #endregion

    #region Send Platform Message
    public static bool GetBotPreference(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string botPreferenceVariableName = "bot", bool useBotDefault = true)
    {
        if (!CPH.TryGetArg(botPreferenceVariableName, out bool bot))
        {
            bot = useBotDefault;
            CPH.LogInfo($"No bot preference specified in variable `{botPreferenceVariableName}`.  Using default bot account preference: {bot}.");
        }
        else
        {
            CPH.LogInfo($"Bot preference {bot} specified in variable `{botPreferenceVariableName}`.");
        }
        return bot;
    }

    public static bool SendPlatformMessage(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string messageVariableName = "message")
    {
        if (!CPH.TryGetArg(messageVariableName, out string message))
        {
            CPH.LogError($"No message variable with name {messageVariableName} was set.");
            return false;
        }

        if (String.IsNullOrWhiteSpace(message))
        {
            CPH.LogWarn("Message is blank.");
            return false;
        }

        var platform = GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            return false;
        }

        return SendPlatformMessage(CPH, platform, message);
    }

    public static bool SendPlatformMessage(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string platform, string message, bool bot)
    {
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

    public static bool SendPlatformMessage(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string platform, string message)
    {
        var bot = GetBotPreference(CPH);
        return SendPlatformMessage(CPH, platform, message, bot);
    }
    #endregion

    #region Bot Account Helpers
    // TODO: pretty sure we can query Streamer.bot for this info
    // TODO: set these to your bot account names on each platform
    private static readonly string TWITCH_BOT_ACCOUNT_USER_NAME = "teamventuregaming";
    private static readonly string YOUTUBE_BOT_ACCOUNT_USER_NAME = "Team Venture Bot";
    private static readonly string TROVO_BOT_ACCOUNT_USER_NAME = ""; // TODO: Add Trovo bot username

    public static bool IsBotAccount(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH)
    {
        if (!CPH.TryGetUserName(out string userName))
        {
            return false;
        }

        var platform = CPH.GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        return IsBotAccount(platform, userName);
    }

    public static bool IsBotAccount(string platform, string username)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                return StringComparer.OrdinalIgnoreCase.Equals(username, TWITCH_BOT_ACCOUNT_USER_NAME);
            case Constants.YOUTUBE:
                return StringComparer.OrdinalIgnoreCase.Equals(username, YOUTUBE_BOT_ACCOUNT_USER_NAME);
            //case Constants.TROVO:
            //    return StringComparer.OrdinalIgnoreCase.Equals(username, TROVO_BOT_ACCOUNT_USER_NAME);
        }

        return false;
    }
    #endregion
}

public static class YouTubeExtensions
{
    public static string? GetYouTubeUserNameFromRawInput(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, int inputsToRemove = 1)
    {
        if (!CPH.TryGetArg(Constants.RAW_INPUT, out string rawInput))
        {
            CPH.LogError("Inable to get raw input.");
            return null;
        }

        //This Removes Inputs we dont need
        int removeLength = 0;
        for (int i = 0; i < inputsToRemove; i++)
        {
            if (CPH.TryGetInput(i, out string moreInput))
            {
                removeLength += moreInput.Length;
            }
            else
            {
                CPH.LogError($"Unablet to get input {i} of {inputsToRemove}");
            }
        }

        //This now works out the username we are trying to find
        var youtubeUserName = rawInput.Remove(0, removeLength + inputsToRemove);
        if (youtubeUserName[0] == '@')
        {
            youtubeUserName = youtubeUserName.Remove(0, 1);
        }

        return youtubeUserName;
    }

    public static IEnumerable<string> GetKnownUserIdsForUserVar(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string youtubeUserName, string usersVarName, bool isPersisted)
    {
        var userPointsList = CPH.GetYouTubeUsersVar<string>(usersVarName, isPersisted);
        return userPointsList.Where(userValue => StringComparer.OrdinalIgnoreCase.Equals(youtubeUserName, userValue.UserLogin)).Select(x => x.UserId).ToList();
    }

    public static IEnumerable<string> GetKnownUserIdsForUserVar(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, int inputsToRemove, string usersVarName, bool isPersisted)
    {
        var youtubeUserName = GetYouTubeUserNameFromRawInput(CPH, inputsToRemove);
        if (string.IsNullOrEmpty(youtubeUserName))
        {
            CPH.LogError("Unable to get youtube username from raw.");
            return Enumerable.Empty<string>();
        }

        return GetKnownUserIdsForUserVar(CPH, youtubeUserName!, usersVarName, isPersisted);
    }

    public static bool GetYouTubeTarget(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, int inputsToRemove, string usersVarName, bool isPersisted)
    {
        var youtubeUserName = GetYouTubeUserNameFromRawInput(CPH, inputsToRemove);
        if (string.IsNullOrEmpty(youtubeUserName))
        {
            CPH.LogError("Unable to get youtube username from raw.");
            return false;
        }

        var knownUserIds = GetKnownUserIdsForUserVar(CPH, youtubeUserName!, usersVarName, isPersisted).ToList();
        var knownUserCount = knownUserIds.Count;

        CPH.SetArgument("targetUserName", youtubeUserName);
        CPH.SetArgument("usersFound", knownUserCount);
        CPH.LogInfo($"[Points Admin] YouTube - User To Handle: {youtubeUserName} matches {knownUserCount} user(s).");

        for (int i = 0; i < knownUserIds.Count; i++)
        {
            var knownUserId = knownUserIds[i];
            CPH.SetArgument("targetFoundUserId" + i, knownUserId);
            CPH.LogInfo($"[Points Admin] YouTube - {youtubeUserName} with the ID of {knownUserId} has been found");
        }

        return true;
    }
}

public class PointsSystem
{
    private readonly Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH;
    private readonly string PointsVariableName;

    public PointsSystem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        this.CPH = CPH;
        this.PointsVariableName = pointsVariableName;
    }

    /// <summary>
    /// Gets the points for a given user on the specified platform
    /// or null if that user has never been assigned points
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public long? GetUserPoints(string platform, string userId)
        => GetUserPoints(this.CPH, this.PointsVariableName, platform, userId);

    public bool SetPoints()
        => SetPoints(this.CPH, this.PointsVariableName);

    /// <summary>
    /// Sets the points for a given user on the specified platform
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <param name="points"></param>
    public void SetUserPoints(string platform, string userId, long points)
        => SetUserPoints(this.CPH, this.PointsVariableName, platform, userId, points);

    public bool AddPoints()
        => AddPoints(this.CPH, this.PointsVariableName);

    public bool AddPointsToTriggeringUserId(long points)
        => AddPointsToTriggeringUserId(this.CPH, this.PointsVariableName, points);

    /// <summary>
    /// Adds points to the given user on the specified platform
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <param name="pointsToAdd"></param>
    public void AddUserPoints(string platform, string userId, long pointsToAdd)
        => AddUserPoints(this.CPH, this.PointsVariableName, platform, userId, pointsToAdd);

    public bool TryGetUserPoints(out long? points)
        => TryGetUserPoints(this.CPH, this.PointsVariableName, out points);

    public void ClearPointsForAllPlatforms()
        => ClearPointsForAllPlatforms(this.CPH, this.PointsVariableName);

    public bool SendUserPointsMessage(Func<string, long, string> messageFunc)
        => SendUserPointsMessage(this.CPH, this.PointsVariableName, messageFunc);

    public bool TryRedeem(string redeemName, long redeemCost, Dictionary<string, string> actionLookup)
        => TryRedeem(this.CPH, this.PointsVariableName, redeemName, redeemCost, actionLookup);

    public static long? GetUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                return GetTwitchUserPointsById(CPH, pointsVariableName, userId);
            case Constants.YOUTUBE:
                return GetYouTubeUserPointsById(CPH, pointsVariableName, userId);
            case Constants.TROVO:
                return GetTrovoUserPointsById(CPH, pointsVariableName, userId);
            default:
                return null;
        }
    }

    public static bool SetPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
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

        var platform = PlatformExtensions.GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            return false;
        }

        switch (platform)
        {
            case Constants.TWITCH:
                return SetPointsTwitch(CPH, pointsVariableName, points);
            case Constants.YOUTUBE:
                return SetPointsYouTube(CPH, pointsVariableName, points);
            case Constants.TROVO:
                return SetPointsTrovo(CPH, pointsVariableName, points);
            default:
                return false;
        }
    }

    public static void SetUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, long points)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                SetTwitchUserPointsById(CPH, pointsVariableName, userId, points);
                break;
            case Constants.YOUTUBE:
                SetYouTubeUserPointsById(CPH, pointsVariableName, userId, points);
                break;
            case Constants.TROVO:
                SetTrovoUserPointsById(CPH, pointsVariableName, userId, points);
                break;
        }
    }

    public static bool AddPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        var platform = PlatformExtensions.GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            return false;
        }

        switch (platform)
        {
            case Constants.TWITCH:
                return AddPointsTwitch(CPH, pointsVariableName, points);
            case Constants.YOUTUBE:
                return AddPointsYouTube(CPH, pointsVariableName, points);
            case Constants.TROVO:
                return AddPointsTrovo(CPH, pointsVariableName, points);
            default:
                return false;
        }
    }

    public static bool AddPointsToTriggeringUserId(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        if (!CPH.TryGetUserId(out var userId))
        {
            return false;
        }

        var platform = PlatformExtensions.GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            return false;
        }

        AddUserPoints(CPH, pointsVariableName, platform, userId, points);
        return true;
    }

    public static void AddUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, long pointsToAdd)
    {
        var currentPoints = GetUserPoints(CPH, pointsVariableName, platform, userId);
        SetUserPoints(CPH, pointsVariableName, platform, userId, (currentPoints ?? 0) + pointsToAdd);

        // TODO: could optimize like this, but it's another switch to maintain if platforms expand
        //switch (platform)
        //{
        //    case Constants.TWITCH:
        //        SetTwitchUserPointsById(CPH, pointsVariableName, userId, (GetTwitchUserPointsById(CPH, pointsVariableName, userId) ?? 0) + pointsToAdd);
        //        break;
        //    case Constants.YOUTUBE:
        //        SetYouTubeUserPointsById(CPH, pointsVariableName, userId, (GetYouTubeUserPointsById(CPH, pointsVariableName, userId) ?? 0) + pointsToAdd);
        //        break;
        //    case Constants.TROVO:
        //        SetTrovoUserPointsById(CPH, pointsVariableName, userId, (GetTrovoUserPointsById(CPH, pointsVariableName, userId) ?? 0) + pointsToAdd);
        //        break;
        //}
    }

    public static bool TryGetUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, out long? points)
    {
        if (!CPH.TryGetUserId(out var userId))
        {
            points = null;
            return false;
        }

        var platform = CPH.GetPlatformTriggeringAction();
        if (platform == null)
        {
            points = null;
            return false;
        }

        points = GetUserPoints(CPH, pointsVariableName, platform, userId);
        if (points == null)
        {
            return false;
        }

        return true;
    }

    public static void ClearPointsForAllPlatforms(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        CPH.UnsetAllUsersVar(pointsVariableName, true);
        CPH.LogInfo($"Points have been cleared for all platforms for {pointsVariableName}");
    }

    public static bool SendUserPointsMessage(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, Func<string, long, string> messageFunc)
    {
        if (!CPH.TryGetUserId(out var userId))
        {
            return false;
        }

        var platform = CPH.GetPlatformTriggeringAction();
        if (platform == null)
        {
            return false;
        }

        return SendUserPointsMessage(CPH, pointsVariableName, platform, userId, messageFunc);
    }

    public static bool SendUserPointsMessage(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, Func<string, long, string> messageFunc)
    {
        long? userPoints = GetUserPoints(CPH, pointsVariableName, platform, userId);
        if (userPoints == null)
        {
            // TODO: could default to 0 points
            return false;
        }

        if (!CPH.TryGetUserName(out var userName))
        {
            // TODO: could default a username or send a different message
            return false;
        }

        return CPH.SendPlatformMessage(platform, messageFunc(userName, userPoints.Value));
    }

    //private static readonly Random Rand = new ();
    private static readonly string UKNOWN_REDEEMER_USER_NAME = "Redeemer";

    public static bool TryRedeem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string redeemName, long redeemCost, Dictionary<string, string> actionLookup)
    {
        if (!CPH.TryGetUserId(out string userId))
        {
            CPH.LogError("No user ID found.");
            return false;
        }

        var platform = PlatformExtensions.GetPlatformTriggeringAction(CPH);
        if (platform == null)
        {
            CPH.LogError("No platform found.");
            return false;
        }

        long? userPoints = GetUserPoints(CPH, pointsVariableName, platform, userId);
        if (userPoints == null)
        {
            // TODO: could default points to 0
            CPH.LogError("No user points found.");
            return false;
        }

        var actionsByName = CPH.GetActions().ToDictionary(x => x.Name);
        var actionsByKey = actionLookup.Select(x =>
        {
            actionsByName.TryGetValue(x.Value, out var action);
            return (x.Key, action);
        }).ToDictionary(x => x.Key, x => x.action, actionLookup.Comparer);
        var enabledActionsByKey = actionsByKey.Where(x => x.Value?.Enabled ?? false).ToDictionary(x => x.Key, x => x.Value, actionLookup.Comparer);
        if (enabledActionsByKey.Count == 0)
        {
            CPH.LogWarn($"User tried to execute {redeemName}, but not of the actions were enabled.");
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{redeemName} currently has no enabled actions!");
            return false;
        }

        if (!CPH.TryGetArg(Constants.USER_NAME, out string userName))
        {
            userName = UKNOWN_REDEEMER_USER_NAME;
            CPH.LogInfo($"No user name found, using {userName} as default.");
        }

        if (!CPH.TryGetArg(Constants.INPUT_0, out string actionKey))
        {
            if (userPoints < redeemCost)
            {
                PlatformExtensions.SendPlatformMessage(CPH, platform, $"{userName}, {redeemName} requires {redeemCost}, but you only have {userPoints}.");
                return false;
            }

            var enabledActions = enabledActionsByKey.Values.ToList();
            //var selectedAction = enabledActions[Rand.Next(enabledActions.Count)];
            var selectedAction = enabledActions[CPH.Between(0, enabledActions.Count - 1)]; // TODO: confirm the range is [min, max] and not [min, max)
            CPH.LogInfo($"Redeem action not specified.  Randomly selecting {selectedAction!.Name}");

            SetUserPoints(CPH, pointsVariableName, platform, userId, userPoints.Value - redeemCost);

            CPH.SetArgument("redeemActionName", selectedAction.Name);
            return CPH.RunAction(selectedAction.Name, false);
        }

        if (!actionsByKey.TryGetValue(actionKey, out var action))
        {
            // TODO: may need to split this list into multiple messages
            var activeKeyList = string.Join(", ", actionsByKey.Where(x => x.Value?.Enabled ?? false).Select(x => x.Key));
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{redeemName} costs {redeemCost} points and when used by itself picks a random redeem.");
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"You can also specify one of these: {activeKeyList}.");
            return false;
        }

        if (action?.Enabled != true)
        {
            // TODO: may need to split this list into multiple messages
            var activeKeyList = string.Join(", ", actionsByKey.Where(x => x.Value?.Enabled ?? false).Select(x => x.Key));
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{actionKey} is not currently active. Try one of these instead: {activeKeyList}.");
            return false;
        }

        if (userPoints < redeemCost)
        {
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{userName}, {redeemName} requires {redeemCost}, but you only have {userPoints}.");
            return false;
        }

        SetUserPoints(CPH, pointsVariableName, platform, userId, userPoints.Value - redeemCost);

        CPH.SetArgument("redeemActionName", action.Name);
        return CPH.RunAction(action.Name, false);
    }

    #region Twitch
    public long? GetTwitchUserPointsById(string userId)
        => GetTwitchUserPointsById(this.CPH, this.PointsVariableName, userId);

    public void SetTwitchUserPointsById(string userId, long points)
        => SetTwitchUserPointsById(this.CPH, this.PointsVariableName, userId, points);

    public bool SetPointsTwitch()
        => SetPointsTwitch(this.CPH, this.PointsVariableName);

    public bool SetPointsTwitch(long amountToSet, string targetUsername)
        => SetPointsTwitch(this.CPH, this.PointsVariableName, amountToSet, targetUsername);

    public bool AddPointsTwitch()
        => AddPointsTwitch(this.CPH, this.PointsVariableName);

    public bool AddPointsTwitch(long amountToAdd, string targetUsername)
        => AddPointsTwitch(this.CPH, this.PointsVariableName, amountToAdd, targetUsername);

    public static long? GetTwitchUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetTwitchUserVarById<long?>(userId, pointsVariableName, true);
    }

    public static void SetTwitchUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long points)
    {
        CPH.SetTwitchUserVarById(userId, pointsVariableName, points, true);
    }

    public static bool SetPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTwitch(CPH, pointsVariableName, points);
    }

    public static bool SetPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUsername))
        {
            return false;
        }

        return SetPointsTwitch(CPH, pointsVariableName, points, targetUsername);
    }

    public static bool SetPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToSet, string targetUsername)
    {
        var targetUserInfo = CPH.TwitchGetUserInfoByLogin(targetUsername);
        if (targetUserInfo == null)
        {
            CPH.LogError($"Unable to get Twitch User Info for {targetUsername}");
            return false;
        }

        SetTwitchUserPointsById(CPH, pointsVariableName, targetUserInfo.UserId, amountToSet);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("points", amountToSet);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", targetUsername);
        CPH.LogInfo($"[Points Admin] [Set Points] Twitch - Set {targetUsername}({targetUserInfo.UserId}) by {addedBy} => {amountToSet}");

        return true;
    }

    public static bool AddPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTwitch(CPH, pointsVariableName, points);
    }

    public static bool AddPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUsername))
        {
            return false;
        }

        return AddPointsTwitch(CPH, pointsVariableName, points, targetUsername);
    }

    public static bool AddPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToAdd, string targetUsername)
    {
        var targetUserInfo = CPH.TwitchGetUserInfoByLogin(targetUsername);
        if (targetUserInfo == null)
        {
            CPH.LogError($"Unable to get Twitch User Info for {targetUsername}");
            return false;
        }

        long? currentPoints = GetTwitchUserPointsById(CPH, pointsVariableName, targetUserInfo.UserId);
        long newPoints = (currentPoints ?? 0) + amountToAdd;
        SetTwitchUserPointsById(CPH, pointsVariableName, targetUserInfo.UserId, newPoints);

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
    public long? GetYouTubeUserPointsById(string userId)
        => GetYouTubeUserPointsById(this.CPH, this.PointsVariableName, userId);

    public void SetYouTubeUserPointsById(string userId, long points)
        => SetYouTubeUserPointsById(this.CPH, this.PointsVariableName, userId, points);

    public bool SetPointsYouTube()
        => SetPointsYouTube(this.CPH, this.PointsVariableName);

    public bool AddPointsYouTube()
        => AddPointsYouTube(this.CPH, this.PointsVariableName);

    public static long? GetYouTubeUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetYouTubeUserVarById<long?>(userId, pointsVariableName, true);
    }

    public static void SetYouTubeUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long points)
    {
        CPH.SetYouTubeUserVarById(userId, pointsVariableName, points, true);
    }

    public static bool SetPointsYouTube(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsYouTube(CPH, pointsVariableName, points);
    }

    public static bool SetPointsYouTube(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        var userIds = GetKnownUserIds(CPH, pointsVariableName).ToList();
        if (userIds.Count == 0)
        {
            CPH.LogWarn("No known user ids were found.");
            return false;
        }

        foreach (var userId in userIds)
        {
            SetYouTubeUserPointsById(CPH, pointsVariableName, userId, points);
        }
        return true;
    }

    public static bool AddPointsYouTube(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsYouTube(CPH, pointsVariableName, points);
    }

    public static bool AddPointsYouTube(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        var userIds = GetKnownUserIds(CPH, pointsVariableName).ToList();
        if (userIds.Count == 0)
        {
            CPH.LogWarn("No known user ids were found.");
            return false;
        }

        foreach (var userId in userIds)
        {
            var currentUserPoints = GetYouTubeUserPointsById(CPH, pointsVariableName, userId);
            SetYouTubeUserPointsById(CPH, pointsVariableName, userId, (currentUserPoints ?? 0) + points);
        }
        return true;
    }

    private static IEnumerable<string> GetKnownUserIds(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        return YouTubeExtensions.GetKnownUserIdsForUserVar(CPH, 1, pointsVariableName, true);
    }
    #endregion

    #region Trovo
    public long? GetTrovoUserPointsById(string userId)
        => GetTrovoUserPointsById(this.CPH, this.PointsVariableName, userId);

    public void SetTrovoUserPointsById(string userId, long points)
        => SetTrovoUserPointsById(this.CPH, this.PointsVariableName, userId, points);

    public bool SetPointsTrovo()
        => SetPointsTrovo(this.CPH, this.PointsVariableName);

    public bool SetPointsTrovo(long amountToSet)
        => SetPointsTrovo(this.CPH, this.PointsVariableName, amountToSet);

    public bool SetPointsTrovo(long amountToSet, string userName)
        => SetPointsTrovo(this.CPH, this.PointsVariableName, amountToSet, userName);

    public bool AddPointsTrovo()
        => AddPointsTrovo(this.CPH, this.PointsVariableName);

    public bool AddPointsTrovo(long amountToAdd)
        => AddPointsTrovo(this.CPH, this.PointsVariableName, amountToAdd);

    public bool AddPointsTrovo(long amountToAdd, string targetUser)
        => AddPointsTrovo(this.CPH, this.PointsVariableName, amountToAdd, targetUser);

    public static long? GetTrovoUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetTrovoUserVarById<long?>(userId, pointsVariableName, true);
    }

    public static void SetTrovoUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userName, long points)
    {
        CPH.SetTrovoUserVar(userName, pointsVariableName, points, true);
    }

    public static bool SetPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTrovo(CPH, pointsVariableName, points);
    }

    public static bool SetPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToSet)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUser))
        {
            return false;
        }

        return SetPointsTrovo(CPH, pointsVariableName, amountToSet, targetUser);
    }

    public static bool SetPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToSet, string userName)
    {
        SetTrovoUserPointsById(CPH, pointsVariableName, userName, amountToSet);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("points", amountToSet);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", userName);
        CPH.LogInfo($"[Points Admin] [Add Points] Trovo - Set To {userName} by {addedBy} => {amountToSet}");
        return true;
    }

    public static bool AddPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return AddPointsTrovo(CPH, pointsVariableName, points);
    }

    public static bool AddPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToAdd)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUser))
        {
            return false;
        }

        return AddPointsTrovo(CPH, pointsVariableName, amountToAdd, targetUser);
    }

    public static bool AddPointsTrovo(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long amountToAdd, string userName)
    {
        long? currentPoints = GetTrovoUserPointsById(CPH, pointsVariableName, userName);
        long newPoints = (currentPoints ?? 0) + amountToAdd;
        SetTrovoUserPointsById(CPH, pointsVariableName, userName, newPoints);

        CPH.TryGetArg("user", out string addedBy);
        CPH.SetArgument("newPoints", newPoints);
        CPH.SetArgument("oldPoints", currentPoints);
        CPH.SetArgument("pointsAdded", amountToAdd);
        CPH.SetArgument("addedBy", addedBy);
        CPH.SetArgument("addedTo", userName);
        CPH.LogInfo($"[Points Admin] [Add Points] Twitch - Added To {userName} by {addedBy} => {currentPoints} + {amountToAdd} = {newPoints}");
        return true;
    }
    #endregion
}

public class CPHInline
{
    private static readonly Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH; // TODO: make sure to remove / comment this line before copying into Streamer.bot

    private readonly Lazy<PointsSystem> tvgAudioPointsLazy;
    private PointsSystem tvgAudioPoints => tvgAudioPointsLazy.Value;

    public CPHInline()
    {
        this.tvgAudioPointsLazy = new Lazy<PointsSystem>(() => new PointsSystem(CPH, "points"));
    }

    public bool SendUserPointsMessage()
    {
        return this.tvgAudioPoints.SendUserPointsMessage((userName, userPoints) => $"{userName}, you have {userPoints} points!  Use !commands to see how to spend them!");
    }

    public bool SetPoints()
    {
        return this.tvgAudioPoints.SetPoints();
    }

    public bool AddPoints()
    {
        return this.tvgAudioPoints.AddPoints();
    }

    public bool ResetAllUserPoints()
    {
        this.tvgAudioPoints.ClearPointsForAllPlatforms();
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

        if (!CPH.TryGetArg("users", out List<Dictionary<string,object>> users))
        {
            CPH.LogError("Unable to get Users from arguments.");
            return false;
        }

        if (!CPH.TryGetArg(POINTS_PER_TICK_VARIABLE_NAME, out long pointsToAdd))
        {
            pointsToAdd = DEFAULT_POINTS_PER_TICK;
            CPH.LogWarn($"Using default points per tick value {pointsToAdd}");
        }
        else
        {
            CPH.LogInfo($"Using points per tick specified as {pointsToAdd}");
        }

        CPH.LogInfo($"Starting Present Viewers from {platform} with {users.Count} users.");

        string? userId;
        for (int i = 0; i < users.Count; i++)
        {
            userId = users[i]?["id"]?.ToString();
            if (userId == null)
            {
                CPH.LogError("Could not determine user id from users object.");
                continue;
            }
            this.tvgAudioPoints.AddUserPoints(platform, userId, pointsToAdd);
        }

        CPH.LogInfo($"Ended Present Viewers from {platform}");
        return true;
    }

    private static long DEFAULT_POINTS_TO_GIVE = 10; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED TO TRIGGERING USERS BY DEFAULT
    private static readonly string POINTS_TO_GIVE_VARIABLE_NAME = "pointsToGive";
    public bool AddPointsToTriggeringUserId()
    {
        if (!CPH.TryGetArg(POINTS_TO_GIVE_VARIABLE_NAME, out long pointsToAdd))
        {
            pointsToAdd = DEFAULT_POINTS_TO_GIVE;
            CPH.LogWarn($"Using default points to give value {pointsToAdd}");
        }
        else
        {
            CPH.LogInfo($"Using points to give specified as {pointsToAdd}");
        }

        return this.tvgAudioPoints.AddPointsToTriggeringUserId(pointsToAdd);
    }

    public bool FirstWords()
    {
        return this.tvgAudioPoints.AddPointsToTriggeringUserId(300);
    }

    public bool Chatting()
    {
        return this.tvgAudioPoints.AddPointsToTriggeringUserId(10);
    }

    public bool SetIsBotAccount()
    {
        var isBotAccount = PlatformExtensions.IsBotAccount(CPH);
        CPH.SetArgument("isBotAccount", isBotAccount);
        return true;
    }

#region Redeems

#region Smash
    private static readonly Dictionary<string, string> smashActionLookup = new (StringComparer.OrdinalIgnoreCase)
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
        { "zombie",      "[Smash SFX] - Zombie" }
    };

    public bool RedeemSmash()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgAudioPoints.TryRedeem(command, 50, smashActionLookup);
    }
#endregion

#region Song
    private static readonly Dictionary<string, string> songActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "christmastrash", "[Song Bite] - Christmas Trash" },
        { "diggin",         "[Song Bite] - Won't Stop Diggin" },
        { "digginend",      "[Song Bite] - Won't Stop Diggin Finale" },
        { "digtrash",       "[Song Bite] - Dig In Trash" },
        { "dodgebites",     "[Song Bite] - Dodge Bites" },
        { "dodgingzombies", "[Song Bite] - Dodging Zombies" },
        { "morninglight",   "[Song Bite] - Morning Light" },
        { "shoveltrash",    "[Song Bite] - Shovel in the Trash" },
        { "tvwin",          "[Song Bite] - Team Venture for the Win" }
    };

    public bool RedeemSong()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgAudioPoints.TryRedeem(command, 250, songActionLookup);
    }
#endregion

#endregion
}