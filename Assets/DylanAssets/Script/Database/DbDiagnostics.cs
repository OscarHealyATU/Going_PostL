using UnityEngine;

public class DbDiagnostics : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[DbDiagnostics] DbBoot.Instance null? {DbBoot.Instance == null}");

        var db = DbBoot.Instance.Db;

        // print where your DB is (add a public property in DbBoot if you don't have it)
        // Debug.Log($"[DbDiagnostics] DB path: {DbBoot.Instance.DbPath}");

        int typeCount = db.Table<VehicleType>().Count();
        Debug.Log($"[DbDiagnostics] VehicleType rows: {typeCount}");
    }
}