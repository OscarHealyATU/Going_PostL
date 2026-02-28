using UnityEngine;

public class playerPosReset : MonoBehaviour
{
[SerializeField] private Transform respwanPoint;
void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           CharacterController charContrllr = other.GetComponent<CharacterController>();
            if (charContrllr != null) charContrllr.enabled = false;
            other.transform.position = respwanPoint.position;
            if (charContrllr != null) charContrllr.enabled = true;

           
           
        }
    }
}
