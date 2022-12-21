using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Fusion;

// reference https://www.youtube.com/watch?v=Ie0-oKN3Lq0
// need to modify current prefab to use

/// <summary>
/// Class that inherits from XRGrabInteractable that enables the grabbing of objects using 2 hands
/// </summary>
public class TwoHandGrabInteractable : XRGrabInteractable
{
    /// <summary>
    /// User-set boolean variable that determines whether a grabbed object should be scaled based on the distance between hands
    /// </summary>
    public bool scaleObject = true;

    /// <summary>
    /// Variable that set the minimum scale of the grabbed object
    /// </summary>
    public float minScale = 15f;
    
    /// <summary>
    /// Variable that set the maximum scale of the grabbed object
    /// </summary>
    public float maxScale = 50f;

    private Vector3 initialHandPosition1;
    private Vector3 initialHandPosition2;
    private Quaternion initialObjectRotation;
    private Vector3 initialObjectScale;
    private Vector3 initialObjectDirection;

    private Quaternion initialRotationOffset;
    private Transform currentTransform;
    private NetworkVariablesAndReferences networkVar;
    private bool networkVarSet;
    private Gameplay gameplay;
    private float timeToNextActivate = 0f;

    // Start is called before the first frame update
    void Start()
    {
        networkVar = GameObject.Find("Network Interaction Statuses").GetComponent<NetworkVariablesAndReferences>();
        gameplay = FindObjectOfType<Gameplay>();
        networkVarSet = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (timeToNextActivate > 0)
        {
            timeToNextActivate -= Time.deltaTime;
        }
    }

    private void updateTargetBoth()
    {
        Vector3 currentHandPosition1 = interactorsSelecting[0].transform.position;
        Vector3 currentHandPosition2 = interactorsSelecting[1].transform.position;

        Vector3 handDir1 = (initialHandPosition1 - initialHandPosition2).normalized;
        Vector3 handDir2 = (currentHandPosition1 - currentHandPosition2).normalized;

        Quaternion handRotation = Quaternion.FromToRotation(handDir1, handDir2);

        float currentGrabDistance = Vector3.Distance(currentHandPosition1, currentHandPosition2);
        float initialGrabDistance = Vector3.Distance(initialHandPosition1, initialHandPosition2);
        float percentage = (currentGrabDistance / initialGrabDistance);

        if (scaleObject)
        {
            Vector3 newScale = new Vector3(percentage * initialObjectScale.x, percentage * initialObjectScale.y, initialObjectScale.z);
            if (newScale.x >= minScale && newScale.x <= maxScale)
            {
                this.transform.localScale = newScale;
            }
        }
        this.transform.rotation = handRotation * initialObjectRotation;
        this.transform.position = (0.5f * (currentHandPosition1 + currentHandPosition2)) + (handRotation * (initialObjectDirection * percentage));
    }

    /// <summary>
    /// Override parent method. If the number of hands grabbing the object is 1, then do default behavior.
    /// If the number of hands grabbing the object is 2, then do math to determine position and scale
    /// </summary>
    /// <param name="updatePhase">XRInteractionUpdateOrder.UpdatePhase</param>
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (interactorsSelecting.Count == 1)
        {
            base.ProcessInteractable(updatePhase);
        }
        else if(interactorsSelecting.Count == 2 && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            updateTargetBoth();

        }
        
        currentTransform = this.transform;
    }

    protected override void Awake() 
    {
        base.Awake();
        selectMode = InteractableSelectMode.Multiple;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (interactorsSelecting.Count == 1)
        {
            Debug.Log("First hand grab");
            if (!networkVarSet)
            {
                networkVar.UpdatePlayerGrabbed();
                networkVarSet = true;
                NetworkRunner Runner = GetComponent<NetworkRunner>();
                if (Runner.IsSharedModeMasterClient)
                {
                    NetworkObject obj;
                    Runner.TryFindObject(networkVar.shadowBasketIDs[0], out obj);
                    Runner.Despawn(obj);
                }
                else
                {
                    NetworkObject obj;
                    Runner.TryFindObject(networkVar.shadowBasketIDs[1], out obj);
                    Runner.Despawn(obj);
                }
            }
        }
        if (interactorsSelecting.Count == 2)
        {
            Debug.Log("Second hand grab");
            initialHandPosition1 = interactorsSelecting[0].transform.position;
            initialHandPosition2 = interactorsSelecting[1].transform.position;
            initialObjectRotation = this.transform.rotation;
            initialObjectScale = this.transform.localScale;
            initialObjectDirection = this.transform.position - (initialHandPosition1 + initialHandPosition2) * 0.5f;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (interactorsSelecting.Count == 1)
        {
            Debug.Log("One hand exited");
        }
        if (interactorsSelecting.Count == 0)
        {
            Debug.Log("Both hands exited");
            this.transform.localScale = new Vector3(25,25,25);
            this.GetComponent<Rigidbody>().isKinematic = false;
        }
        this.transform.position = currentTransform.position;
        this.transform.rotation = currentTransform.rotation;
    }

  protected override void OnActivated(ActivateEventArgs args)
  {
    base.OnActivated(args);
    if (timeToNextActivate <= 0)
    {
        gameplay.CheckAndSendDeterrent();
        timeToNextActivate = 0.5f;
    }
    
  }

}
