using System;
using System.Linq;
using UnityEngine;

public static class PlayerService
{
    public static event Action<double> OnMoneyChanged;

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
        if (amount == 0) return;

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

    public static double GetMoney()
    {
        return Get().money;
    }

    // ----------------------------
    // Return Point (used by your existing return scripts)
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

        player.money = startingMoney;

        // clear return point
        player.returnValid = 0;
        player.returnX = 0f;
        player.returnY = 0f;
        player.returnZ = 0f;
        player.returnYaw = 0f;

        // clear resume point
        player.hasResumePoint = 0;
        player.savedScene = null;
        player.savedX = 0f;
        player.savedY = 0f;
        player.savedZ = 0f;
        player.savedYaw = 0f;

        db.Update(player);

        OnMoneyChanged?.Invoke(player.money);
    }
}