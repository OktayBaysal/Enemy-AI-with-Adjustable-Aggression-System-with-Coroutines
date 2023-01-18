using UnityEngine;

public class PrototypePlayerHitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy Detection Range")) 
        {
            other.transform.parent.gameObject.GetComponent<BasicEnemyScript>().DetectionHandler();
        }
    }
}
