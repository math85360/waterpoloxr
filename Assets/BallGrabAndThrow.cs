using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;

public class BallGrabAndThrow : MonoBehaviour
{
    [System.Serializable]
    public struct ControllerData
    {
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public Vector3 position;
        public Quaternion rotation;
    }
    public LayerMask grabbableLayer; // Définir les objets attrapables (ballon)
    public Transform handTransform; // La position de la main

    private GameObject heldBall = null; // Référence du ballon en main
    private Rigidbody heldBallRb = null; // Rigidbody du ballon


    // Boutons pour le grip et le trigger
    // public InputActionProperty gripButton; // Bouton pour attraper
    // public InputActionProperty triggerButton; // Bouton pour lancer

    public bool isPrimaryHand = true;

    private OVRInput.Axis1D gripButton;
    private OVRInput.Axis1D triggerButton;
    private OVRInput.Controller controller;

    public ControllerData currentControllerData;
    public ControllerData lastControllerData;

    private enum State
    {
        Idle,
        Grabbed,
        ReadyToThrow,
        Thrown
    }

    private State currentState = State.Idle;

    void Start()
    {
        if (isPrimaryHand)
        {
            gripButton = OVRInput.Axis1D.PrimaryHandTrigger;
            triggerButton = OVRInput.Axis1D.PrimaryIndexTrigger;
            controller = OVRInput.Controller.LTouch;
        }
        else
        {
            gripButton = OVRInput.Axis1D.SecondaryHandTrigger;
            triggerButton = OVRInput.Axis1D.SecondaryIndexTrigger;
            controller = OVRInput.Controller.RTouch;
        }
        Debug.Log("gripButton: " + gripButton);
        Debug.Log("triggerButton: " + triggerButton);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Grabbed:
                HandleGrabbedState();
                break;
            case State.ReadyToThrow:
                HandleReadyToThrowState();
                break;
            case State.Thrown:
                HandleThrownState();
                break;
        }
    }

    void HandleIdleState()
    {
        var gripState = OVRInput.Get(gripButton) > 0.0f;
        if (gripState)
        {
            Debug.Log("Grip button pressed");
            TryGrabBall();
            currentState = State.Grabbed;
        }
    }

    void HandleGrabbedState()
    {
        heldBall.transform.localPosition = Vector3.zero;

        var triggerState = OVRInput.Get(triggerButton) > 0.0f;
        UpdateControllerData();
        if (triggerState)
        {
            Debug.Log("Trigger button pressed");
            currentState = State.ReadyToThrow;
        }
    }

    void HandleReadyToThrowState()
    {
        var triggerState = OVRInput.Get(triggerButton) > 0.0f;
        if (!triggerState)
        {
            Debug.Log("Trigger button released");
            ReleaseBall();
            currentState = State.Thrown;
        }
        else
        {
            UpdateControllerData();
        }
    }

    void UpdateControllerData()
    {
        // Quaternion worldRotation = handTransform.rotation;
        // Quaternion localRotation = Quaternion.Inverse(worldRotation) * heldBall.transform.rotation;
        heldBall.transform.localPosition = Vector3.zero;
        Vector3 linearVelocity = heldBall.transform.TransformDirection(OVRInput.GetLocalControllerVelocity(controller));
        Vector3 angularVelocity = heldBall.transform.InverseTransformDirection(OVRInput.GetLocalControllerAngularVelocity(controller));
        Vector3 position = handTransform.localToWorldMatrix.MultiplyPoint(OVRInput.GetLocalControllerPosition(controller));
        // handTransform.transform.ToTrackingSpacePose
        Quaternion rotation = OVRInput.GetLocalControllerRotation(controller);
        lastControllerData.linearVelocity = linearVelocity;
        lastControllerData.angularVelocity = angularVelocity;
        lastControllerData.position = position;
        lastControllerData.rotation = rotation;
        currentControllerData.linearVelocity = linearVelocity;
        currentControllerData.angularVelocity = angularVelocity;
        currentControllerData.position = position;
        currentControllerData.rotation = rotation;
    }

    void HandleThrownState()
    {
        // Vérifier si le gripButton est toujours enfoncé
        var gripState = OVRInput.Get(gripButton) > 0.0f;
        if (!gripState)
        {
            // Reset to idle after throwing only if gripButton is released
            currentState = State.Idle;
        }
    }

    void TryGrabBall()
    {
        // Find the ball in the scene
        GameObject ball = GameObject.Find("Ball");
        if (ball != null)
        {
            Debug.Log("Ball found");
            heldBall = ball;
        }
        // Vérifier s'il y a un ballon proche à attraper
        Collider[] colliders = Physics.OverlapSphere(handTransform.position, 0.1f, grabbableLayer);
        if (colliders.Length > 0)
        {
            Debug.Log("Ball found");
            heldBall = colliders[0].gameObject;

        }
        GrabBall();
    }

    void GrabBall()
    {
        heldBallRb = heldBall.GetComponent<Rigidbody>();
        // Fixer le ballon à la main
        heldBallRb.isKinematic = true; // Désactiver la physique pendant la prise
        heldBall.transform.SetParent(handTransform, false);
        heldBall.transform.localPosition = Vector3.zero;
    }

    void ReleaseBall()
    {
        if (heldBall != null)
        {
            Debug.Log("Ball released");
            heldBallRb.isKinematic = false;
            // heldBall.transform.position  = handTransform.position+   OVRInput.GetLocalControllerPosition(controller);
            // heldBallRb.AddForce()
            // heldBallRb.AddRelativeForce(velocity, ForceMode.Impulse);
            // heldBallRb.AddRelativeTorque(angularVelocity, ForceMode.Impulse);
            heldBall.transform.SetParent(null);
            heldBallRb.linearVelocity = lastControllerData.linearVelocity;
            heldBallRb.angularVelocity = lastControllerData.angularVelocity;
            // heldBallRb.position = lastControllerData.position;
            // heldBallRb.rotation = lastControllerData.rotation;
            // Réinitialiser les variables
            heldBall = null;
            heldBallRb = null;
        }
    }

}
