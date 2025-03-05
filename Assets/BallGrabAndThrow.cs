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
        // Simplification des calculs de vélocité pour qu'ils fonctionnent sur le dispositif réel
        lastControllerData = currentControllerData;

        // Obtenir directement les vélocités dans l'espace monde
        Vector3 linearVelocity = OVRInput.GetLocalControllerVelocity(controller);
        Vector3 angularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);

        // Conserver la position et rotation du contrôleur
        Vector3 position = handTransform.position;
        Quaternion rotation = handTransform.rotation;

        // Mettre à jour les données actuelles du contrôleur
        currentControllerData.linearVelocity = linearVelocity;
        currentControllerData.angularVelocity = angularVelocity;
        currentControllerData.position = position;
        currentControllerData.rotation = rotation;

        // Maintenir le ballon à la position de la main
        heldBall.transform.position = handTransform.position;
        heldBall.transform.rotation = handTransform.rotation;
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
            heldBall.transform.SetParent(null);

            // Appliquer les vélocités dans l'espace monde avec un facteur de force
            float throwForce = 2.0f; // Ajustez cette valeur selon la force de lancer souhaitée

            // Transformer les vélocités locales en vélocités mondiales
            Vector3 worldLinearVelocity = handTransform.TransformDirection(lastControllerData.linearVelocity) * throwForce;
            Vector3 worldAngularVelocity = handTransform.TransformDirection(lastControllerData.angularVelocity);

            // Appliquer les vélocités
            heldBallRb.linearVelocity = worldLinearVelocity;
            heldBallRb.angularVelocity = worldAngularVelocity;

            // Réinitialiser les variables
            heldBall = null;
            heldBallRb = null;
        }
    }

}
