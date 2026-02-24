using System;
using System.Linq;

public static class PlayerService
{
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
        player.money = newMoney;
        db.Update(player);
    }
}
