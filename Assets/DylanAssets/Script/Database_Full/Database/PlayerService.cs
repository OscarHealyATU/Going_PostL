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
        if (player == null) throw new Exception("No Player row exists.");
        return player;
    }

    public static void SetMoney(double newMoney)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();
        player.money = Math.Round(newMoney);
        db.Update(player);

        OnMoneyChanged?.Invoke(newMoney);
    }

    public static void AddMoney(double amount)
    {
        var player = Get();
        SetMoney(Math.Round(player.money + amount));
    }

    // ----------------------------
    // Return Point (Main <- Warehouse)
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
            position = default;
            yaw = 0f;
            return false;
        }

        position = new Vector3(
            (float)player.returnX,
            (float)player.returnY,
            (float)player.returnZ
        );

        yaw = (float)player.returnYaw;
        return true;
    }

    public static void ClearReturnPoint()
    {
        var db = DbBoot.Instance.Db;
        var player = Get();
        player.returnValid = 0;
        db.Update(player);
    }
}