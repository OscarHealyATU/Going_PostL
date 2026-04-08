using System;
using System.Linq;
using UnityEngine;

public static class PlayerService
{
    public const int ExpPerDelivery = 100;
    public const int ExpPerLevel = 1000;

    public static event Action<double> OnMoneyChanged;

    public static event Action<int, int, int> OnExperienceChanged;
    // level, currentExpIntoLevel, expNeededThisLevel

    public static event Action<int, int, int, int, int, int> OnExperienceChangedDetailed;
    // oldLevel, oldExpIntoLevel, oldExpNeeded, newLevel, newExpIntoLevel, newExpNeeded

    public static Player Get()
    {
        var db = DbBoot.Instance.Db;
        var player = db.Table<Player>().FirstOrDefault();

        if (player == null)
            throw new Exception("No Player row exists.");

        return player;
    }

    public static void SetMoney(double newMoney)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.money = newMoney;
        db.Update(player);

        OnMoneyChanged?.Invoke(player.money);
    }

    public static void AddMoney(double amount)
    {
        if (amount == 0)
            return;

        var player = Get();
        SetMoney(player.money + amount);
    }

    public static bool TrySpendMoney(double amount)
    {
        if (amount < 0)
            amount = Math.Abs(amount);

        var player = Get();
        if (player.money < amount)
            return false;

        SetMoney(player.money - amount);
        return true;
    }

    public static bool SpendMoney(double amount, bool trackAsDayExpense)
    {
        if (!TrySpendMoney(amount))
            return false;

        if (trackAsDayExpense && DayManager.Instance != null)
            DayManager.Instance.RegisterMoneySpent(amount);

        return true;
    }

    public static double GetMoney()
    {
        return Get().money;
    }

    // ----------------------------
    // Experience / Level
    // ----------------------------
    public static int GetTotalExperience()
    {
        return Get().totalExperience;
    }

    public static int GetLevel()
    {
        return (GetTotalExperience() / ExpPerLevel) + 1;
    }

    public static int GetLevel(Player player)
    {
        if (player == null)
            return 1;

        return (player.totalExperience / ExpPerLevel) + 1;
    }

    public static int GetExperienceIntoCurrentLevel()
    {
        return GetTotalExperience() % ExpPerLevel;
    }

    public static int GetExperienceIntoCurrentLevel(Player player)
    {
        if (player == null)
            return 0;

        return player.totalExperience % ExpPerLevel;
    }

    public static int GetExperienceNeededForNextLevel()
    {
        return ExpPerLevel;
    }

    public static float GetLevelProgress01()
    {
        return GetExperienceIntoCurrentLevel() / (float)ExpPerLevel;
    }

    public static float GetLevelProgress01(Player player)
    {
        if (player == null)
            return 0f;

        return (player.totalExperience % ExpPerLevel) / (float)ExpPerLevel;
    }

    public static void AddExperience(int amount)
    {
        AddExperience(amount, notifyDayManager: false);
    }

    public static void AddExperience(int amount, bool notifyDayManager)
    {
        if (amount <= 0)
            return;

        var db = DbBoot.Instance.Db;
        var player = Get();

        int oldTotalExp = player.totalExperience;
        int oldLevel = (oldTotalExp / ExpPerLevel) + 1;
        int oldExpIntoLevel = oldTotalExp % ExpPerLevel;
        int oldExpNeeded = ExpPerLevel;

        player.totalExperience += amount;
        db.Update(player);

        int newTotalExp = player.totalExperience;
        int newLevel = (newTotalExp / ExpPerLevel) + 1;
        int newExpIntoLevel = newTotalExp % ExpPerLevel;
        int newExpNeeded = ExpPerLevel;

        if (newLevel > oldLevel)
        {
            Debug.Log($"[PlayerService] Level Up! {oldLevel} -> {newLevel}");
        }

        if (notifyDayManager && DayManager.Instance != null)
        {
            // Reserved for future non-delivery XP sources if needed.
        }

        OnExperienceChangedDetailed?.Invoke(
            oldLevel,
            oldExpIntoLevel,
            oldExpNeeded,
            newLevel,
            newExpIntoLevel,
            newExpNeeded
        );

        OnExperienceChanged?.Invoke(
            newLevel,
            newExpIntoLevel,
            newExpNeeded
        );
    }

    public static void RewardDeliveryExperience()
    {
        AddExperience(ExpPerDelivery);
    }

    public static void RewardDelivery(double moneyAmount)
    {
        AddMoney(moneyAmount);
        AddExperience(ExpPerDelivery);

        if (DayManager.Instance != null)
            DayManager.Instance.RegisterDelivery(moneyAmount, ExpPerDelivery);
    }

    // ----------------------------
    // Return Point
    // ----------------------------
    public static void SaveReturnPoint(Vector3 position, float yaw)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.returnValid = 1;
        player.returnX = position.x;
        player.returnY = position.y;
        player.returnZ = position.z;
        player.returnYaw = yaw;

        db.Update(player);
    }

    public static bool TryGetReturnPoint(out Vector3 position, out float yaw)
    {
        var player = Get();

        if (player.returnValid != 1)
        {
            position = Vector3.zero;
            yaw = 0f;
            return false;
        }

        position = new Vector3(player.returnX, player.returnY, player.returnZ);
        yaw = player.returnYaw;
        return true;
    }

    public static void ClearReturnPoint()
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.returnValid = 0;
        player.returnX = 0f;
        player.returnY = 0f;
        player.returnZ = 0f;
        player.returnYaw = 0f;

        db.Update(player);
    }

    // ----------------------------
    // Resume Game save system
    // ----------------------------
    public static void SaveResumePoint(string sceneName, Vector3 position, float yaw)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.hasResumePoint = 1;
        player.savedScene = sceneName;
        player.savedX = position.x;
        player.savedY = position.y;
        player.savedZ = position.z;
        player.savedYaw = yaw;

        db.Update(player);
    }

    public static bool HasResumePoint()
    {
        var player = Get();
        return player.hasResumePoint == 1 && !string.IsNullOrEmpty(player.savedScene);
    }

    public static bool TryGetResumePoint(out string sceneName, out Vector3 position, out float yaw)
    {
        var player = Get();

        if (player.hasResumePoint != 1 || string.IsNullOrEmpty(player.savedScene))
        {
            sceneName = null;
            position = Vector3.zero;
            yaw = 0f;
            return false;
        }

        sceneName = player.savedScene;
        position = new Vector3(player.savedX, player.savedY, player.savedZ);
        yaw = player.savedYaw;
        return true;
    }

    public static void ClearResumePoint()
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.hasResumePoint = 0;
        player.savedScene = null;
        player.savedX = 0f;
        player.savedY = 0f;
        player.savedZ = 0f;
        player.savedYaw = 0f;

        db.Update(player);
    }

    public static void ResetForNewGame(double startingMoney = 10000.0)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        int oldLevel = GetLevel();
        int oldExpIntoLevel = player.totalExperience % ExpPerLevel;

        player.money = startingMoney;
        player.totalExperience = 0;

        player.returnValid = 0;
        player.returnX = 0f;
        player.returnY = 0f;
        player.returnZ = 0f;
        player.returnYaw = 0f;

        player.hasResumePoint = 0;
        player.savedScene = null;
        player.savedX = 0f;
        player.savedY = 0f;
        player.savedZ = 0f;
        player.savedYaw = 0f;

        db.Update(player);

        OnMoneyChanged?.Invoke(player.money);

        OnExperienceChangedDetailed?.Invoke(
            oldLevel,
            oldExpIntoLevel,
            ExpPerLevel,
            1,
            0,
            ExpPerLevel
        );

        OnExperienceChanged?.Invoke(
            1,
            0,
            ExpPerLevel
        );
    }

    public static void RefreshAllUI()
    {
        var player = Get();
        int level = GetLevel(player);
        int expIntoLevel = GetExperienceIntoCurrentLevel(player);

        OnMoneyChanged?.Invoke(player.money);

        OnExperienceChangedDetailed?.Invoke(
            level,
            expIntoLevel,
            ExpPerLevel,
            level,
            expIntoLevel,
            ExpPerLevel
        );

        OnExperienceChanged?.Invoke(
            level,
            expIntoLevel,
            ExpPerLevel
        );
    }
}