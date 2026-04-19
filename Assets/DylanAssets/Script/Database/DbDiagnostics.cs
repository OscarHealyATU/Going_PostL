using UnityEngine;

public class DbDiagnostics : MonoBehaviour
{
    void Start()
    {
        //debug.Log($"[DbDiagnostics] DbBoot.Instance null? {DbBoot.Instance == null}");

        var db = DbBoot.Instance.Db;

        // print where your DB is (add a public property in DbBoot if you don't have it)
        // //debug.Log($"[DbDiagnostics] DB path: {DbBoot.Instance.DbPath}");

        int typeCount = db.Table<VehicleType>().Count();
        //debug.Log($"[DbDiagnostics] VehicleType rows: {typeCount}");
    }
}