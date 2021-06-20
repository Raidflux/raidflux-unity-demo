using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour
{
    public Projectile projectilePrefab;
    
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    
    private float rotationX = 0;

    private Canvas playerUI;

    private Camera playerCamera;
    private CharacterController characterController;
    
    Vector3 moveDirection = Vector3.zero;

    public LayerMask aimLayerMask;
    

    [HideInInspector]
    public bool canMove = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        playerUI = GetComponentInChildren<Canvas>();
    }

    void Start()
    {
        if (!IsLocalPlayer)
        {
            characterController.enabled = false;
            playerCamera.enabled = false;
            playerUI.enabled = false;
        }

    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            float range = 100000f;
            
            Vector3 direction = ((playerCamera.transform.position + (playerCamera.transform.forward * range)) - transform.position).normalized;

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hitInfo, range, aimLayerMask))
            {
                direction = (hitInfo.point - transform.position).normalized;
            }
            
            FireProjectileServerRpc(direction);
        }
        
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    void FireProjectileServerRpc(Vector3 direction)
    {
        Debug.Log("Fire from server");
        FireProjectile(direction);
    }

    void FireProjectile(Vector3 direction)
    {
        Debug.Log("Fired from server");

        Projectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.transform.LookAt(direction);
        projectile.GetComponent<NetworkObject>().Spawn(null, true);
        projectile.Launch(gameObject, direction);
    }

}
