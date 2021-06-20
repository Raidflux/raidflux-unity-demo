using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public float speed = 10.0f;
    public float lifeTime = 10.0f;
    public LayerMask layerMask;
    
    private NetworkVariable<Vector3> direction = new NetworkVariable<Vector3>(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    
    private NetworkVariable<GameObject> owner = new NetworkVariable<GameObject>(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    
    private Vector3 lastPos;
        
    // Start is called before the first frame update
    void Start()
    {
        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (direction.Value != Vector3.zero)
        {
            transform.position += (direction.Value * (speed * Time.deltaTime));
            transform.LookAt(transform.position + direction.Value);
        }

        if (NetworkManager.Singleton.IsServer && gameObject && gameObject.activeInHierarchy)
        {
            Debug.DrawLine(lastPos, transform.position, Color.red, 5, false);
            if (Physics.Linecast(lastPos, transform.position, out RaycastHit hitInfo, layerMask) && hitInfo.transform.gameObject != owner.Value)
            {
                Debug.Log(hitInfo.transform.gameObject.name);
                StopAllCoroutines();
                Destroy(gameObject);
            }

            lastPos = transform.position;
        }
    }
    
    public void Launch(GameObject owner, Vector3 direction)
    {
        this.owner.Value = owner;
        this.direction.Value = direction;
        if (NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(Timeout());
        }
    }

    private IEnumerator Timeout()
    {
        yield return new WaitForSeconds(lifeTime);
        Debug.Log("Destroy projectile");
        Destroy(gameObject);
    }
}
