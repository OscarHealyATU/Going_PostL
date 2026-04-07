using UnityEngine;

public class VehicleSpawnPoint : MonoBehaviour
{
    [SerializeField] private float checkRadius = 2.5f;
    [SerializeField] private LayerMask vehicleLayerMask = ~0;

    public bool IsOccupied()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius, vehicleLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<VehicleLink>() != null)
                return true;

            if (hits[i].attachedRigidbody != null && hits[i].attachedRigidbody.GetComponent<VehicleLink>() != null)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
#endif
}