using System;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractEnum state;
    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;
    private const float PLAYER_FORCE = 100f;
    private const float LERP_SPEED = 10f;
    private bool showingOutline;
    private bool haveInteracted;
    private Outline outline;

    public enum InteractEnum
    {
        Interactable,
        PickUp,
        Grabbable
    }
    private void Awake()
    {
        switch (state)
        {
            case InteractEnum.Interactable:
                outline = GetComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                outline.OutlineColor = Color.white;
                outline.OutlineWidth = 5f;
                outline.enabled = false;
                break;
            
            case InteractEnum.PickUp:
            case InteractEnum.Grabbable:
                objectRigidbody = GetComponent<Rigidbody>();
                break;
        }
    }
    

    public void ShowOutline(bool show)
    {
        outline.enabled = show;
    }

    public InteractEnum GetState()
    {
        return state;
    }

    public virtual void Interacted()
    {
        print("test");
    }
    public void Grab(Transform playerGrabPointTransform)
    {
        objectGrabPointTransform = playerGrabPointTransform;
        if (state == InteractEnum.PickUp)
        {
            objectRigidbody.useGravity = false;
        }
    }

    public void Drop()
    {
        objectGrabPointTransform = null;
        if (state == InteractEnum.PickUp)
        {
            objectRigidbody.useGravity = true;
        }
    }

    private void FixedUpdate()
    {
        if (state == InteractEnum.Interactable) return;
       
        if (objectGrabPointTransform != null)
        {
            if (state == InteractEnum.PickUp)
            {
                Vector3 newPosition = Vector3.Lerp(transform.position, objectGrabPointTransform.position,
                    Time.deltaTime * LERP_SPEED);
                objectRigidbody.MovePosition(newPosition);
            }
            else
            {
                Vector3 forceDirection = objectGrabPointTransform.position - transform.position;
                if (forceDirection.magnitude > 2f)
                {
                    Drop();
                }
                else
                {
                    forceDirection = Vector3.Normalize(forceDirection);
                }

                objectRigidbody.AddForce(forceDirection * PLAYER_FORCE, ForceMode.Force);
            }
        }
    }
    
}