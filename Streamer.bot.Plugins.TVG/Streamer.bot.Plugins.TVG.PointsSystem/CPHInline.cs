using System;
using System.Collections.Generic;
using System.Linq;

#region Common Extensions
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
    public static readonly string IS_MOD         = "isModerator";
    public static readonly string IS_SUBSCRIBED  = "isSubscribed";
    public static readonly string IS_VIP         = "isVip";

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

    public static bool TryGetIsModerator(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, out bool isMod)
    {
        if (CPH.TryGetArg(IS_MOD, out isMod))
        {
            return true;
        }

        return false;
    }
    public static bool IsModerator(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH)
    {
        return TryGetIsModerator(CPH, out var isMod) && isMod;
    }

    public static bool TryGetIsSubscribed(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, out bool isSubscribed)
    {
        if (CPH.TryGetArg(IS_SUBSCRIBED, out isSubscribed))
        {
            return true;
        }

        return false;
    }
    public static bool IsSubscribed(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH)
    {
        return TryGetIsSubscribed(CPH, out var isSubscribed) && isSubscribed;
    }

    public static bool TryGetIsVip(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, out bool isVip)
    {
        if (CPH.TryGetArg(IS_VIP, out isVip))
        {
            return true;
        }

        return false;
    }
    public static bool IsVip(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH)
    {
        return TryGetIsVip(CPH, out var isVip) && isVip;
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

    public static bool SendPlatformMessageIfNotBotAccount(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string messageVariableName = "platformMessage")
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

        if (CPH.TryGetUserName(out var username) && IsBotAccount(platform, username))
        {
            return false;
        }

        return SendPlatformMessage(CPH, platform, message);
    }

    public static bool SendPlatformMessage(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string messageVariableName = "platformMessage")
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

    public static bool SendPlatformMessage(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string platform, string message, bool bot, int maxMessageSize = 200)
    {
        return SendPlatformMessages(CPH, platform, GetSplitMessagesList(message, maxMessageSize), bot);
    }

    public static bool SendPlatformMessages(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string platform, IEnumerable<string> messages, bool bot)
    {
        if (messages == null)
        {
            return false;
        }

        var enumerator = messages.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return false;
        }

        Action<string> sendMessageAction;
        switch (platform)
        {
            case Constants.TWITCH:
                sendMessageAction = (message) => CPH.SendMessage(message, bot);
                break;
            case Constants.YOUTUBE:
                sendMessageAction = (message) => CPH.SendYouTubeMessageToLatestMonitored(message, bot);
                break;
            case Constants.TROVO:
                sendMessageAction = (message) => CPH.SendTrovoMessage(message, bot);
                break;
            default:
                return false;
        }

        sendMessageAction(enumerator.Current);

        while (enumerator.MoveNext())
        {
            CPH.Wait(CPH.Between(1500, 2500));
            sendMessageAction(enumerator.Current);
        }

        return true;
    }

    public static IEnumerable<string> GetSplitMessagesList(string message, int maxMessageSize = 200)
    {
        // TODO: can probably do this more efficiently
        int searchStart = Math.Max(1, maxMessageSize - 20);
        while (message.Length > maxMessageSize)
        {
            // break up the message
            var splitSpot = message.IndexOfAny([' ', '.', ','], searchStart) + 1; // could try to preference . and ,
            if (splitSpot > maxMessageSize)
            {
                splitSpot = maxMessageSize;
            }

            var toSend = message.Substring(0, splitSpot);

            yield return toSend;

            message = message.Remove(0, splitSpot);
        }

        yield return message;
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
    //private static readonly string TROVO_BOT_ACCOUNT_USER_NAME = ""; // TODO: Add Trovo bot username

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

        var youtubeUserName = rawInput.Remove(0, removeLength + inputsToRemove);
        if (youtubeUserName[0] == '@')
        {
            youtubeUserName = youtubeUserName.Remove(0, 1);
        }

        return youtubeUserName;
    }

    public static IEnumerable<Streamer.bot.Plugin.Interface.Model.UserVariableValue<T>> GetKnownUserVariablesForUsername<T>(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string youtubeUserName, string usersVarName, bool isPersisted)
    {
        var userPointsList = CPH.GetYouTubeUsersVar<T>(usersVarName, isPersisted);
        return userPointsList.Where(userValue => StringComparer.OrdinalIgnoreCase.Equals(youtubeUserName, userValue.UserLogin));
    }

    public static IEnumerable<Streamer.bot.Plugin.Interface.Model.UserVariableValue<T>> GetKnownUserVariables<T>(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, int inputsToRemove, string usersVarName, bool isPersisted)
    {
        var youtubeUserName = GetYouTubeUserNameFromRawInput(CPH, inputsToRemove);
        if (string.IsNullOrEmpty(youtubeUserName))
        {
            CPH.LogError("Unable to get youtube username from raw input.");
            return Enumerable.Empty<Streamer.bot.Plugin.Interface.Model.UserVariableValue<T>>();
        }

        return GetKnownUserVariablesForUsername<T>(CPH, youtubeUserName!, usersVarName, isPersisted);
    }
}

public static class TwitchExtensions
{
    public static bool TryGetTwitchUserIdByUsername(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string login, out string userId)
    {
        var targetUserInfo = CPH.TwitchGetUserInfoByLogin(login);
        if (targetUserInfo == null)
        {
            CPH.LogError($"Unable to get Twitch User Info for {login}");
            userId = String.Empty;
            return false;
        }
        userId = targetUserInfo.UserId;
        return true;
    }
}

public static class ActionExtensions
{
    /// <summary>
    /// Gets the current ActionData for a custom keyed set of actions
    /// </summary>
    /// <param name="map">
    /// Mapping of custom keys to action names
    /// </param>
    /// <returns>The current ActionData for a custom keyed set of actions</returns>
    /// <remarks>
    /// The goal of this method is to allow mapping of custom key name for
    /// an action name to the current status of that action.  This allows
    /// users in chat to type an alias for an action instead of the full
    /// action name.
    /// 
    /// For Example,
    /// var map = new Dictionary&lt;string, string&gt;(StringComparer.OrdinalIgnoreCase)
    /// {
    ///     { "hug", "[User Actions] - Hug" } // "hug" is the alias and "[User Actions] - Hug" is the actual action name in Streamer.bot
    /// };
    /// </remarks>
    public static Dictionary<string, Streamer.bot.Plugin.Interface.Model.ActionData?> GetActionMap(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, Dictionary<string, string> map)
    {
        var actionsByName = CPH.GetActions().ToDictionary(x => x.Name, StringComparer.Ordinal);
        var ret = new Dictionary<string, Streamer.bot.Plugin.Interface.Model.ActionData?>(map.Comparer);
        foreach (var kvp in map)
        {
            actionsByName.TryGetValue(kvp.Value, out var action);
            ret.Add(kvp.Key, action);
        }
        return ret;

        // TODO: this complains about nullability not matching for some reason
        //return map.Select(x =>
        //{
        //    actionsByName.TryGetValue(x.Value, out var action);
        //    return (x.Key, action);
        //}).ToDictionary(x => x.Key, x => x.action, map.Comparer);
    }

    public static bool RunMappedAction(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, Dictionary<string, string> map, string key, bool runImmediately = true)
    {
        if (!map.TryGetValue(key, out var actionName))
        {
            CPH.LogError($"Requested action `{key}` does not exist in map.");
            return false;
        }

        var actionsByName = CPH.GetActions().ToDictionary(x => x.Name, StringComparer.Ordinal);
        if (!actionsByName.TryGetValue(actionName, out var action))
        {
            CPH.LogError($"Requested action `{actionName}` does not exist in Streamer.bot.");
            return false;
        }

        if (!action.Enabled)
        {
            CPH.LogWarn($"Requested action `{actionName}` is not currently enabled.");
            return false;
        }

        return CPH.RunAction(action.Name, runImmediately);
    }

    public static bool RunRandomEnabledMappedAction(this Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, Dictionary<string, string> map, bool runImmediately = true)
    {
        var enabledActions = GetActionMap(CPH, map).Where(x => x.Value?.Enabled ?? false).Select(x => x.Value!).ToList();
        if (enabledActions.Count == 0)
        {
            CPH.LogWarn("The specified map contains no enabled actions.");
            return false;
        }

        if (enabledActions.Count == 1)
        {
            return CPH.RunAction(enabledActions[0].Name, runImmediately);
        }

        return CPH.RunAction(enabledActions[CPH.Between(0, enabledActions.Count - 1)].Name, runImmediately);
    }
}
#endregion

public class PointsSystem
{
    private readonly Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH;
    private readonly string PointsVariableName;

    public PointsSystem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        this.CPH = CPH;
        this.PointsVariableName = pointsVariableName;
    }

    #region Platform Agnostic

    #region Core
    /// <summary>
    /// Gets the points for a given user on the specified platform
    /// or null if that user has never been assigned points
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public long? GetUserPoints(string platform, string userId)
        => GetUserPoints(this.CPH, this.PointsVariableName, platform, userId);

    /// <summary>
    /// Sets the points for a given user on the specified platform
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <param name="points"></param>
    public void SetUserPoints(string platform, string userId, long points)
        => SetUserPoints(this.CPH, this.PointsVariableName, platform, userId, points);

    /// <summary>
    /// Adds points to the given user on the specified platform
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="userId"></param>
    /// <param name="pointsToAdd"></param>
    public void AddUserPoints(string platform, string userId, long pointsToAdd)
        => AddUserPoints(this.CPH, this.PointsVariableName, platform, userId, pointsToAdd);

    public void ClearPointsForAllPlatforms()
        => ClearPointsForAllPlatforms(this.CPH, this.PointsVariableName);

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

    public static void AddUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, long pointsToAdd)
    {
        switch (platform)
        {
            case Constants.TWITCH:
                AddTwitchUserPointsById(CPH, pointsVariableName, userId, pointsToAdd);
                break;
            case Constants.YOUTUBE:
                AddYouTubeUserPointsById(CPH, pointsVariableName, userId, pointsToAdd);
                break;
            case Constants.TROVO:
                AddTrovoUserPointsById(CPH, pointsVariableName, userId, pointsToAdd);
                break;
        }
    }

    public static void ClearPointsForAllPlatforms(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        CPH.UnsetAllUsersVar(pointsVariableName, true);
        CPH.LogInfo($"Points have been cleared for all platforms for {pointsVariableName}");
    }
    #endregion

    #region Parse Arguments
    public bool SetPoints()
        => SetPoints(this.CPH, this.PointsVariableName);

    public bool AddPoints()
        => AddPoints(this.CPH, this.PointsVariableName);

    public bool TryGetTriggeringUserPoints(out long? points)
        => TryGetTriggeringUserPoints(this.CPH, this.PointsVariableName, out points);

    public bool AddPointsToTriggeringUser(long points)
        => AddPointsToTriggeringUser(this.CPH, this.PointsVariableName, points);

    public bool SendTriggeringUserPointsMessage(Func<string, long, string> messageFunc)
        => SendTriggeringUserPointsMessage(this.CPH, this.PointsVariableName, messageFunc);

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

    public static bool TryGetTriggeringUserPoints(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, out long? points)
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
        return true;
    }

    public static bool AddPointsToTriggeringUser(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
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

    public static bool SendTriggeringUserPointsMessage(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, Func<string, long, string> messageFunc)
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
            // TODO: could default to 0 points, or send a message saying this user has not been assigned points
            return false;
        }

        if (!CPH.TryGetUserName(out var userName))
        {
            // TODO: could default a username or send a different message
            return false;
        }

        return CPH.SendPlatformMessage(platform, messageFunc(userName, userPoints.Value));
    }
    #endregion

    #endregion

    #region Platform Specific

    #region Twitch

    #region Core
    /// <summary>
    /// Gets points for the Twitch user with the specified
    /// userId or null if no points have been specified
    /// </summary>
    /// <param name="userId">
    /// UserId of the Twitch user
    /// </param>
    /// <returns></returns>
    public long? GetTwitchUserPointsById(string userId)
        => GetTwitchUserPointsById(this.CPH, this.PointsVariableName, userId);

    public long? GetTwitchUserPointsByUsername(string targetUsername)
        => GetTwitchUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername);

    /// <summary>
    /// Sets points for the Twitch user with the specified userId
    /// </summary>
    /// <param name="userId">
    /// UserId of the Twitch user
    /// </param>
    /// <param name="amountToSet">
    /// Points to set for this user
    /// </param>
    public void SetTwitchUserPointsById(string userId, long amountToSet)
        => SetTwitchUserPointsById(this.CPH, this.PointsVariableName, userId, amountToSet);

    public bool SetTwitchUserPointsByUsername(string targetUsername, long amountToSet)
        => SetTwitchUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToSet);

    public (long? oldPoints, long newPoints) AddTwitchUserPointsById(string userId, long amountToAdd)
        => AddTwitchUserPointsById(this.CPH, this.PointsVariableName, userId, amountToAdd);

    public bool AddTwitchUserPointsByUsername(string targetUsername, long amountToAdd)
        => AddTwitchUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToAdd);

    /// <summary>
    /// Gets points for the Twitch user with the specified
    /// userId or null if no points have been specified
    /// </summary>
    /// <param name="userId">
    /// UserId of the Twitch user
    /// </param>
    /// <returns></returns>
    public static long? GetTwitchUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetTwitchUserVarById<long?>(userId, pointsVariableName, true);
    }

    /// <summary>
    /// Gets points for the Twitch user with the specified
    /// username or null if no points have been specified
    /// or a userId could not be determined
    /// </summary>
    /// <param name="targetUsername">
    /// Username of the Twitch user
    /// </param>
    /// <returns></returns>
    public static long? GetTwitchUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername)
    {
        if (!TwitchExtensions.TryGetTwitchUserIdByUsername(CPH, targetUsername, out var userId))
        {
            return null;
        }

        return GetTwitchUserPointsById(CPH, pointsVariableName, userId);
    }

    /// <summary>
    /// Sets points for the Twitch user with the specified userId
    /// </summary>
    /// <param name="userId">
    /// UserId of the Twitch user
    /// </param>
    /// <param name="amountToSet">
    /// Points to set for this user
    /// </param>
    public static void SetTwitchUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long amountToSet)
    {
        CPH.SetTwitchUserVarById(userId, pointsVariableName, amountToSet, true);
    }

    /// <summary>
    /// Sets points for the Twitch user with the specified username
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="targetUsername">
    /// Username of the Twitch user
    /// </param>
    /// <param name="amountToSet"></param>
    /// <returns>
    /// True if a userId could be determined from the username
    /// </returns>
    public static bool SetTwitchUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long amountToSet)
    {
        if (!TwitchExtensions.TryGetTwitchUserIdByUsername(CPH, targetUsername, out var userId))
        {
            return false;
        }

        SetTwitchUserPointsById(CPH, pointsVariableName, userId, amountToSet);

        CPH.SetArgument("pointsToSet", amountToSet);
        CPH.SetArgument("pointsTargetUsername", targetUsername);
        CPH.LogInfo($"Set Twitch user {targetUsername} ({userId}) points to {amountToSet}.");

        return true;
    }

    /// <summary>
    /// Adds points to the Twitch user with the specified userId
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="userId">
    /// UserId of the Twitch user
    /// </param>
    /// <param name="amountToAdd">
    /// Points to add to this user
    /// </param>
    public static (long? oldPoints, long newPoints) AddTwitchUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long amountToAdd)
    {
        var current = GetTwitchUserPointsById(CPH, pointsVariableName, userId);
        var newPoints = current == null ? amountToAdd : (current.Value + amountToAdd);
        // NOTE: Remove this to enable negative points (penalty points?)
        if (newPoints < 0)
        {
            newPoints = 0;
        }
        SetTwitchUserPointsById(CPH, pointsVariableName, userId, newPoints);
        return (current, newPoints);
    }

    /// <summary>
    /// Adds points to the Twitch user with the specified username
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="targetUsername">
    /// Username of the Twitch user
    /// </param>
    /// <param name="amountToAdd"></param>
    /// <returns>
    /// True if a userId could be determined from the username
    /// </returns>
    public static bool AddTwitchUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long amountToAdd)
    {
        if (!TwitchExtensions.TryGetTwitchUserIdByUsername(CPH, targetUsername, out var userId))
        {
            return false;
        }

        (long? oldPoints, long newPoints) = AddTwitchUserPointsById(CPH, pointsVariableName, userId, amountToAdd);

        CPH.SetArgument("oldPoints", oldPoints);
        CPH.SetArgument("newPoints", newPoints);
        CPH.SetArgument("pointsToAdd", amountToAdd);
        CPH.SetArgument("pointsTargetUsername", targetUsername);
        CPH.LogInfo($"Added {amountToAdd} points to Twitch user {targetUsername} ({userId}).  Old Points: {oldPoints} => New Points: {newPoints}.");

        return true;
    }
    #endregion

    #region Parse Commands
    /// <summary>
    /// Tries to parse a command to determine the user and points to set
    /// </summary>
    /// <returns></returns>
    public bool SetPointsTwitch()
        => SetPointsTwitch(this.CPH, this.PointsVariableName);

    public bool AddPointsTwitch()
        => AddPointsTwitch(this.CPH, this.PointsVariableName);

    /// <summary>
    /// Tries to parse a command to determine the user and points to set
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <returns>
    /// True if the command was able to be parsed and successful set user points;
    /// otherwise, false.
    /// </returns>
    /// <remarks>
    /// The expected format is:
    ///     [command] [points] [username]
    /// </remarks>
    public static bool SetPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName)
    {
        if (!CPH.TryGetArg(Constants.INPUT_0, out long points))
        {
            return false;
        }

        return SetPointsTwitch(CPH, pointsVariableName, points);
    }

    /// <summary>
    /// Tries to parse a command to determine the user to which to add points
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="points">Points to set</param>
    /// <returns>
    /// True if the command was able to be parsed and successful set user points;
    /// otherwise, false.
    /// </returns>
    /// <remarks>
    /// The expected format is:
    ///     [command] [input0] [username]
    /// This method ignores input0 and uses the specified points
    /// </remarks>
    public static bool SetPointsTwitch(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long points)
    {
        if (!CPH.TryGetArg(Constants.INPUT_1, out string targetUsername))
        {
            return false;
        }

        return SetTwitchUserPointsByUsername(CPH, pointsVariableName, targetUsername, points);
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

        return AddTwitchUserPointsByUsername(CPH, pointsVariableName, targetUsername, points);
    }
    #endregion

    #endregion

    #region YouTube

    #region Core
    public long? GetYouTubeUserPointsById(string userId)
        => GetYouTubeUserPointsById(this.CPH, this.PointsVariableName, userId);

    public IEnumerable<Streamer.bot.Plugin.Interface.Model.UserVariableValue<long>> GetYouTubeUserPointsByUsername(string targetUsername)
        => GetYouTubeUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername);

    public void SetYouTubeUserPointsById(string userId, long points)
        => SetYouTubeUserPointsById(this.CPH, this.PointsVariableName, userId, points);

    public List<string> SetYouTubeUserPointsByUsername(string targetUsername, long amountToSet)
        => SetYouTubeUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToSet);

    public (long? oldPoints, long newPoints) AddYouTubeUserPointsById(string userId, long amountToAdd)
        => AddYouTubeUserPointsById(this.CPH, this.PointsVariableName, userId, amountToAdd);

    public IEnumerable<(string userId, long? oldPoints, long newPoints)> AddYouTubeUserPointsByUsername(string targetUsername, long amountToAdd)
        => AddYouTubeUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToAdd);

    public static long? GetYouTubeUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetYouTubeUserVarById<long?>(userId, pointsVariableName, true);
    }

    public static IEnumerable<Streamer.bot.Plugin.Interface.Model.UserVariableValue<long>> GetYouTubeUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername)
    {
        return YouTubeExtensions.GetKnownUserVariablesForUsername<long>(CPH, targetUsername, pointsVariableName, true);
    }

    public static void SetYouTubeUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long points)
    {
        CPH.SetYouTubeUserVarById(userId, pointsVariableName, points, true);
    }

    public static List<string> SetYouTubeUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long points)
    {
        var userIds = new List<string>();
        foreach (var userVar in GetYouTubeUserPointsByUsername(CPH, pointsVariableName, targetUsername))
        {
            SetYouTubeUserPointsById(CPH, pointsVariableName, userVar.UserId, points);
            userIds.Add(userVar.UserId);
        }
        return userIds;
    }

    /// <summary>
    /// Adds points to the YouTube user with the specified userId
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="userId">
    /// UserId of the YouTube user
    /// </param>
    /// <param name="amountToAdd">
    /// Points to add to this user
    /// </param>
    public static (long? oldPoints, long newPoints) AddYouTubeUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long amountToAdd)
    {
        var current = GetYouTubeUserPointsById(CPH, pointsVariableName, userId);
        var newPoints = current == null ? amountToAdd : (current.Value + amountToAdd);
        // NOTE: Remove this to enable negative points (penalty points?)
        if (newPoints < 0)
        {
            newPoints = 0;
        }
        SetYouTubeUserPointsById(CPH, pointsVariableName, userId, newPoints);
        return (current, newPoints);
    }

    /// <summary>
    /// Adds points to the YouTube user with the specified username
    /// </summary>
    /// <param name="CPH"></param>
    /// <param name="pointsVariableName"></param>
    /// <param name="targetUsername">
    /// Username of the YouTube user
    /// </param>
    /// <param name="amountToAdd">
    /// Points to add to this user
    /// </param>
    public static List<(string userId, long? oldPoints, long newPoints)> AddYouTubeUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long amountToAdd)
    {
        var userIds = new List<(string userId, long? oldPoints, long newPoints)>();
        foreach (var userVar in GetYouTubeUserPointsByUsername(CPH, pointsVariableName, targetUsername))
        {
            (var oldPoints, var newPoints) = AddYouTubeUserPointsById(CPH, pointsVariableName, userVar.UserId, amountToAdd);
            userIds.Add((userVar.UserId, oldPoints, newPoints));
        }
        return userIds;
    }
    #endregion

    #region Parse Commands
    public bool SetPointsYouTube()
        => SetPointsYouTube(this.CPH, this.PointsVariableName);

    public bool AddPointsYouTube()
        => AddPointsYouTube(this.CPH, this.PointsVariableName);

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
        var username = YouTubeExtensions.GetYouTubeUserNameFromRawInput(CPH, 1);
        if (String.IsNullOrWhiteSpace(username))
        {
            CPH.LogError("Unable to determine youtube user from Raw Input");
            return false;
        }

        var updatedUserPoints = SetYouTubeUserPointsByUsername(CPH, pointsVariableName, username!, points);
        if (updatedUserPoints.Count == 0)
        {
            CPH.LogWarn($"Unable to set user's points to {points} for username {username}.  No matching users found.");
            return false;
        }

        CPH.LogInfo($"Set {updatedUserPoints.Count} user's points to {points} with username {username}.");
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
        var username = YouTubeExtensions.GetYouTubeUserNameFromRawInput(CPH, 1);
        if (String.IsNullOrWhiteSpace(username))
        {
            CPH.LogError("Unable to determine youtube user from Raw Input");
            return false;
        }

        var updatedUserPoints = AddYouTubeUserPointsByUsername(CPH, pointsVariableName, username!, points);
        if (updatedUserPoints.Count == 0)
        {
            CPH.LogWarn($"Unable to add {points} points to username {username}.  No matching users found.");
            return false;
        }

        CPH.LogInfo($"Added {points} points to {updatedUserPoints.Count} user's points with username {username}.");
        return true;
    }
    #endregion

    #endregion

    #region Trovo

    #region Core
    public long? GetTrovoUserPointsById(string userId)
        => GetTrovoUserPointsById(this.CPH, this.PointsVariableName, userId);

    public long? GetTrovoUserPointsByUsername(string targetUsername)
        => GetTrovoUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername);

    public void SetTrovoUserPointsById(string userId, long points)
        => SetTrovoUserPointsById(this.CPH, this.PointsVariableName, userId, points);

    public bool SetTrovoUserPointsByUsername(string targetUsername, long amountToSet)
        => SetTrovoUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToSet);

    public (long? oldPoints, long newPoints) AddTrovoUserPointsById(string userId, long amountToAdd)
        => AddTrovoUserPointsById(this.CPH, this.PointsVariableName, userId, amountToAdd);

    public bool AddTrovoUserPointsByUsername(string targetUsername, long amountToAdd)
        => AddTrovoUserPointsByUsername(this.CPH, this.PointsVariableName, targetUsername, amountToAdd);

    public static long? GetTrovoUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId)
    {
        return CPH.GetTrovoUserVarById<long?>(userId, pointsVariableName, true);
    }

    public static long? GetTrovoUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername)
    {
        // TODO: figure this out
        // for now, in the UI, can use "Trovo - Add Target Info" which provides the userId which can be used with the get by id method
        throw new NotImplementedException("Unsure how to get trovo userId from username.");
    }

    public static void SetTrovoUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long amountToSet)
    {
        CPH.SetTrovoUserVar(userId, pointsVariableName, amountToSet, true);
    }

    public static bool SetTrovoUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long amountToSet)
    {
        // TODO: figure this out
        // for now, in the UI, can use "Trovo - Add Target Info" which provides the userId which can be used with the set by id method
        throw new NotImplementedException("Unsure how to get trovo userId from username.");
    }

    public static (long? oldPoints, long newPoints) AddTrovoUserPointsById(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string userId, long amountToAdd)
    {
        var current = GetTrovoUserPointsById(CPH, pointsVariableName, userId);
        var newPoints = current == null ? amountToAdd : (current.Value + amountToAdd);
        // NOTE: Remove this to enable negative points (penalty points?)
        if (newPoints < 0)
        {
            newPoints = 0;
        }
        SetTrovoUserPointsById(CPH, pointsVariableName, userId, newPoints);
        return (current, newPoints);
    }

    public static bool AddTrovoUserPointsByUsername(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string targetUsername, long amountToAdd)
    {
        // TODO: figure this out
        // for now, in the UI, can use "Trovo - Add Target Info" which provides the userId which can be used with the add by id method
        throw new NotImplementedException("Unsure how to get trovo userId from username.");
    }
    #endregion

    #region Parse Commands
    public bool SetPointsTrovo()
        => SetPointsTrovo(this.CPH, this.PointsVariableName);

    public bool AddPointsTrovo()
        => AddPointsTrovo(this.CPH, this.PointsVariableName);

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

        return SetTrovoUserPointsByUsername(CPH, pointsVariableName, targetUser, amountToSet);
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

        return AddTrovoUserPointsByUsername(CPH, pointsVariableName, targetUser, amountToAdd);
    }
    #endregion

    #endregion

    #endregion

    #region Redeems

    private static readonly string UKNOWN_REDEEMER_USER_NAME = "Redeemer";

    #region Simple
    public bool TryRedeem(long redeemCost, string actionName, bool runImmediately = true)
        => TryRedeem(this.CPH, this.PointsVariableName, redeemCost, actionName, runImmediately);

    public static bool TryRedeem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, long redeemCost, string actionName, bool runImmediately = true)
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

        return TryRedeem(CPH, pointsVariableName, platform, userId, redeemCost, actionName, runImmediately);
    }

    public static bool TryRedeem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, long redeemCost, string actionName, bool runImmediately = true)
    {
        if (!CPH.GetActions().ToDictionary(x => x.Name).TryGetValue(actionName, out var action) || !action.Enabled)
        {
            return false;
        }

        long? userPoints = GetUserPoints(CPH, pointsVariableName, platform, userId);
        if (userPoints == null)
        {
            userPoints = 0;
        }

        if (userPoints < redeemCost)
        {
            if (!CPH.IsModerator())
            {
                return false;
            }
        }
        else
        {
            SetUserPoints(CPH, pointsVariableName, platform, userId, userPoints.Value - redeemCost);
        }

        return CPH.RunAction(action.Name, runImmediately);
    }
    #endregion

    #region Group
    public bool TryRedeem(string redeemName, long redeemCost, Dictionary<string, string> actionLookup, bool runImmediately = true)
        => TryRedeem(this.CPH, this.PointsVariableName, redeemName, redeemCost, actionLookup, runImmediately);

    public static bool TryRedeem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string redeemName, long redeemCost, Dictionary<string, string> actionLookup, bool runImmediately = true)
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

        return TryRedeem(CPH, pointsVariableName, platform, userId, redeemName, redeemCost, actionLookup, runImmediately);
    }

    public static bool TryRedeem(Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH, string pointsVariableName, string platform, string userId, string redeemName, long redeemCost, Dictionary<string, string> actionLookup, bool runImmediately = true)
    {
        long? userPoints = GetUserPoints(CPH, pointsVariableName, platform, userId);
        if (userPoints == null)
        {
            userPoints = 0;
        }

        var actionsByKey = CPH.GetActionMap(actionLookup);
        var enabledActionsByKey = actionsByKey.Where(x => x.Value?.Enabled ?? false).ToDictionary(x => x.Key, x => x.Value!, actionLookup.Comparer);
        if (enabledActionsByKey.Count == 0)
        {
            CPH.LogWarn($"User tried to execute {redeemName}, but none of the actions were enabled.");
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{redeemName} currently has no enabled actions!");
            return false;
        }
        
        bool sendNotEnoughPointsMessage()
        {
            if (!CPH.TryGetArg(Constants.USER_NAME, out string userName))
            {
                userName = UKNOWN_REDEEMER_USER_NAME;
                CPH.LogInfo($"No user name found, using {userName} as default.");
            }
            return PlatformExtensions.SendPlatformMessage(CPH, platform, $"{userName}, {redeemName} requires {redeemCost}, but you only have {userPoints}.");
        }
        
        bool checkAndSpendPoints()
        {
            if (userPoints < redeemCost)
            {
                if (!CPH.IsModerator())
                {
                    sendNotEnoughPointsMessage();
                    return false;
                }
                // could send a different message about mod not having enough point, but allowed it anyway
            }
            else
            {
                SetUserPoints(CPH, pointsVariableName, platform, userId, userPoints.Value - redeemCost);
            }
            return true;
        }

        if (!CPH.TryGetArg(Constants.INPUT_0, out string actionKey) || String.IsNullOrWhiteSpace(actionKey)) // select one at random when not specified
        {
            if (!checkAndSpendPoints())
            {
                return false;
            }

            var enabledActions = enabledActionsByKey.Values.ToList();
            //var selectedAction = enabledActions[Rand.Next(enabledActions.Count)];
            var selectedAction = enabledActions[CPH.Between(0, enabledActions.Count - 1)]; // TODO: confirm the range is [min, max] and not [min, max)
            CPH.LogInfo($"Redeem action not specified.  Randomly selecting {selectedAction.Name}");

            CPH.SetArgument("redeemActionName", selectedAction.Name);
            return CPH.RunAction(selectedAction.Name, false);
        }

        if (!actionsByKey.TryGetValue(actionKey, out var action))
        {
            var activeKeyList = string.Join(", ", enabledActionsByKey.Keys);
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{redeemName} costs {redeemCost} points and when used by itself picks a random redeem.");
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"You can also specify one of these: {activeKeyList}.");
            return false;
        }

        if (action?.Enabled != true)
        {
            var activeKeyList = string.Join(", ", enabledActionsByKey.Keys);
            PlatformExtensions.SendPlatformMessage(CPH, platform, $"{actionKey} is not currently active. Try one of these instead: {activeKeyList}.");
            return false;
        }

        if (!checkAndSpendPoints())
        {
            return false;
        }

        CPH.SetArgument("redeemActionName", action.Name);
        return CPH.RunAction(action.Name, runImmediately);
    }
    #endregion

    #endregion
}

public class CPHInline
{
    private static readonly Streamer.bot.Plugin.Interface.IInlineInvokeProxy CPH; // TODO: make sure to remove / comment this line before copying into Streamer.bot

    // Venture points are for fun redeems that should be readily accessible to viewers
    private readonly Lazy<PointsSystem> tvgVenturePointsLazy;
    private PointsSystem tvgVenturePoints => tvgVenturePointsLazy.Value;

    // Play Points (PP) are for 7 Days to Die redeems that we want to be more limited potentially
    private readonly Lazy<PointsSystem> tvgPlayPointsLazy;
    private PointsSystem tvgPlayPoints => tvgPlayPointsLazy.Value;

    public CPHInline()
    {
        this.tvgVenturePointsLazy = new Lazy<PointsSystem>(() => new PointsSystem(CPH, "vp"));
        this.tvgPlayPointsLazy = new Lazy<PointsSystem>(() => new PointsSystem(CPH, "pp"));
    }

    public bool SendPlatformMessage()
    {
        return PlatformExtensions.SendPlatformMessage(CPH);
    }

    public bool SendPlatformMessageIfNotBotAccount()
    {
        return PlatformExtensions.SendPlatformMessageIfNotBotAccount(CPH);
    }

    public bool SendUserCommandsMessage()
    {
        // TODO: make this somehow figure out what commands to offer
        return PlatformExtensions.SendPlatformMessage(CPH, "Use !commandsvp to spend venture points and use !commandspp to spend your pp.");
    }

    public bool SendUserVenturePointsMessage()
    {
        return this.tvgVenturePoints.SendTriggeringUserPointsMessage((userName, userPoints) => $"{userName}, you have {userPoints} venture points!  Use !commandsvp to see how to spend them!");
    }

    public bool SetVenturePoints()
    {
        return this.tvgVenturePoints.SetPoints();
    }

    public bool AddVenturePoints()
    {
        return this.tvgVenturePoints.AddPoints();
    }

    public bool ResetAllUserVenturePoints()
    {
        this.tvgVenturePoints.ClearPointsForAllPlatforms();
        return true;
    }

    public bool SendUserPlayPointsMessage()
    {
        return this.tvgPlayPoints.SendTriggeringUserPointsMessage((userName, userPoints) => $"{userName}, you have {userPoints} play points!  Use !commandspp to see how to spend them!");
    }

    public bool SetPlayPoints()
    {
        return this.tvgPlayPoints.SetPoints();
    }

    public bool AddPlayPoints()
    {
        return this.tvgPlayPoints.AddPoints();
    }

    public bool ResetAllUserPlayPoints()
    {
        this.tvgPlayPoints.ClearPointsForAllPlatforms();
        return true;
    }

    private static long DEFAULT_POINTS_PER_TICK = 10; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED PER PRESENT VIEWER SWEEP
    private static readonly string POINTS_PER_TICK_VARIABLE_NAME = "pointsGivenPerTick";
    private bool AddWatchPointsCommon(PointsSystem pointsSystem)
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

        if (!CPH.TryGetArg("users", out List<Dictionary<string, object>> users))
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
            pointsSystem.AddUserPoints(platform, userId, pointsToAdd);
        }

        CPH.LogInfo($"Ended Present Viewers from {platform}");
        return true;
    }

    public bool AddWatchVenturePoints()
    {
        return AddWatchPointsCommon(this.tvgVenturePoints);
    }

    public bool AddWatchPlayPoints()
    {
        return AddWatchPointsCommon(this.tvgPlayPoints);
    }

    private static long DEFAULT_POINTS_TO_GIVE = 10; // CHANGE THIS TO SET THE AMOUNT OF POINTS ADDED TO TRIGGERING USERS BY DEFAULT
    private static readonly string POINTS_TO_GIVE_VARIABLE_NAME = "pointsToGive";
    private bool AddPointsToTriggeringUserId(PointsSystem pointsSystem)
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

        return pointsSystem.AddPointsToTriggeringUser(pointsToAdd);
    }

    public bool AddVenturePointsToTriggeringUserId()
    {
        return AddPointsToTriggeringUserId(this.tvgVenturePoints);
    }

    public bool AddPlayPointsToTriggeringUserId()
    {
        return AddPointsToTriggeringUserId(this.tvgPlayPoints);
    }

    public bool SetIsBotAccount()
    {
        var isBotAccount = PlatformExtensions.IsBotAccount(CPH);
        var variableName = "isBotAccount";
        CPH.LogInfo($"Setting variable {variableName} to {isBotAccount}");
        CPH.SetArgument(variableName, isBotAccount);
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

        return this.tvgVenturePoints.TryRedeem(command, 50, smashActionLookup);
    }

    public bool RandomSmash()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, smashActionLookup, false);
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

        return this.tvgVenturePoints.TryRedeem(command, 250, songActionLookup);
    }

    public bool RandomSong()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, songActionLookup, false);
    }
#endregion

#region MMGA

#region Song
    private static readonly Dictionary<string, string> mmgaSongActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "backbone",      "[Song Bite MMGA] - Backbone" },
        { "brewfreedom",   "[Song Bite MMGA] - Brewing Freedom" },
        { "campfirebanjo", "[Song Bite MMGA] - Camp Fire Banjo" },
        { "dualkings",     "[Song Bite MMGA] - Dual Kings" },
        { "fallennation",  "[Song Bite MMGA] - Fallen Nation" },
        { "mmgabanjo",     "[Song Bite MMGA] - MMGA Banjo" },
        { "readytogo",     "[Song Bite MMGA] - Ready To Go" },
        { "shotgunWatch",  "[Song Bite MMGA] - Shotgun Watch" }
    };

    public bool RedeemSongMMGA()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgVenturePoints.TryRedeem(command, 250, mmgaSongActionLookup);
    }

    public bool RandomSongMMGA()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, mmgaSongActionLookup, false);
    }
#endregion

#region Clip
    private static readonly Dictionary<string, string> mmgaClipActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "bestpres",     "[MMGA Clip] - Best President" },
        { "biggestahole", "[MMGA Clip] - Biggest AHole" },
        { "fixedfence",   "[MMGA Clip] - Fixed The Fence" },
        { "oops",         "[MMGA Clip] - Oops" },
        { "ungrateful",   "[MMGA Clip] - Ungrateful Jen" }
    };

    public bool RedeemClipMMGA()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgVenturePoints.TryRedeem(command, 250, mmgaClipActionLookup);
    }

    public bool RandomClipMMGA()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, mmgaClipActionLookup, false);
    }
#endregion

#endregion

#region SilenTVentures

#region Song
    private static readonly Dictionary<string, string> silentSongActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "ghosthonor", "[Song Bite SilenTVenture] - Ghost Honor" },
        { "grandduo",   "[Song Bite SilenTVenture] - Grand Duo" },
        { "queenmoon",  "[Song Bite SilenTVenture] - Queen Moonlight" },
        { "tvstrong",   "[Song Bite SilenTVenture] - TV Strong & Bold" }
    };

    public bool RedeemSongSilent()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgVenturePoints.TryRedeem(command, 250, silentSongActionLookup);
    }

    public bool RandomSongSilent()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, silentSongActionLookup, false);
    }
#endregion

#region Clip

#endregion

#endregion

#region CCJ

#region Song
    private static readonly Dictionary<string, string> crabSongActionLookup = new (StringComparer.OrdinalIgnoreCase)
    {
        { "crabjest",     "[Song Bite CCJ] - Crab Jest" },
        { "crabjestlong", "[Song Bite CCJ] - Crab Jest Long" },
        { "crabshoes",    "[Song Bite CCJ] - Crab Shoes" },
        { "jimmytrick",   "[Song Bite CCJ] - Jimmy Trick" },
        { "jugglehope",   "[Song Bite CCJ] - Juggle Hope" },
        { "makeit",       "[Song Bite CCJ] - Make It Through The Night" },
        { "pavcommands",  "[Song Bite CCJ] - Pav Commands" },
        { "towerdoom",    "[Song Bite CCJ] - Tower Doom Short" },
        { "welcomesight", "[Song Bite CCJ] - Welcome Sight" }
    };

    public bool RedeemSongCCJ()
    {
        if (!CPH.TryGetArg(Constants.COMMAND, out string command))
        {
            return false;
        }

        return this.tvgVenturePoints.TryRedeem(command, 250, crabSongActionLookup);
    }

    public bool RandomSongCCJ()
    {
        return ActionExtensions.RunRandomEnabledMappedAction(CPH, crabSongActionLookup, false);
    }
#endregion

#region Clip

#endregion

#endregion

#endregion

#region Play Points



#endregion
}