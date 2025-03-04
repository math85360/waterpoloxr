using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;

public class BallGrabAndThrow : MonoBehaviour
{
    public LayerMask grabbableLayer; // Définir les objets attrapables (ballon)
    public Transform handTransform; // La position de la main

    private GameObject heldBall = null; // Référence du ballon en main
    private Rigidbody heldBallRb = null; // Rigidbody du ballon


    public int velocityFrameCount = 8; // Nombre de frames stockées pour la vélocité
    public float throwForceMultiplier = 1.8f; // Multiplier pour ajuster la force du lancer
    public float spinMultiplier = 0.5f; // Multiplier pour l’effet de rotation

    // Boutons pour le grip et le trigger
    // public InputActionProperty gripButton; // Bouton pour attraper
    // public InputActionProperty triggerButton; // Bouton pour lancer


    private Queue<Vector3> handPositions = new Queue<Vector3>(); // Positions récentes de la main
    private Queue<Vector3> handRotations = new Queue<Vector3>(); // Rotations récentes de la main


    public bool isPrimaryHand = true;

    private OVRInput.Axis1D gripButton;
    private OVRInput.Axis1D triggerButton;

    private float triggerTime;

    void Start()
    {
        if (isPrimaryHand)
        {
            gripButton = OVRInput.Axis1D.PrimaryHandTrigger;
            triggerButton = OVRInput.Axis1D.PrimaryIndexTrigger;
        }
        else
        {
            gripButton = OVRInput.Axis1D.SecondaryHandTrigger;
            triggerButton = OVRInput.Axis1D.SecondaryIndexTrigger;
        }
        Debug.Log("gripButton: " + gripButton);
        Debug.Log("triggerButton: " + triggerButton);
    }

    void Update()
    {
        // Stocke la position précédente de la main
        if (heldBall != null)
        {

            // Suivi des positions et rotations récentes de la main
            TrackHandMovement();
            heldBall.transform.localPosition = Vector3.zero;
        }
        var gripState = OVRInput.Get(gripButton) > 0.0f;
        var triggerState = OVRInput.Get(triggerButton) > 0.0f;

        // Vérifie si l'on presse le bouton de grip pour attraper
        if (gripState && heldBall == null)
        {
            Debug.Log("Grip button pressed");
            TryGrabBall();
        }

        // var triggerDelay = Time.time - triggerTime
        // Vérifie si l'on relâche le trigger pour lancer
        if (heldBall != null && triggerState)
        {
            Debug.Log("Trigger button released");
            ReleaseBall();
        }
    }


    void TrackHandMovement()
    {
        if (handPositions.Count >= velocityFrameCount)
        {
            handPositions.Dequeue(); // Supprimer l’ancienne position
            handRotations.Dequeue(); // Supprimer l’ancienne rotation
        }

        handPositions.Enqueue(handTransform.position); // Ajouter la position actuelle
        handRotations.Enqueue(handTransform.eulerAngles); // Ajouter la rotation actuelle
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
        heldBall.transform.SetParent(handTransform);
        heldBallRb.isKinematic = true; // Désactiver la physique pendant la prise
        heldBall.transform.localPosition = Vector3.zero;
    }

    void ReleaseBall()
    {
        if (heldBall != null)
        {
            Debug.Log("Ball released");
            heldBallRb.isKinematic = false; // Réactiver la physique
            heldBall.transform.SetParent(null);


            // Calculer la vitesse et la rotation moyenne
            Vector3 throwVelocity = CalculateHandVelocity();
            Vector3 spinVelocity = CalculateHandSpin();

            // Appliquer la force et la rotation
            heldBallRb.linearVelocity = throwVelocity * throwForceMultiplier;
            heldBallRb.angularVelocity = spinVelocity * spinMultiplier;

            // Réinitialiser les variables
            heldBall = null;
            heldBallRb = null;
        }
    }



    Vector3 CalculateHandVelocity()
    {
        if (handPositions.Count < 2) return Vector3.zero;

        Vector3 totalVelocity = Vector3.zero;
        Vector3[] positionsArray = handPositions.ToArray();

        for (int i = 1; i < positionsArray.Length; i++)
        {
            Vector3 deltaPosition = positionsArray[i] - positionsArray[i - 1];
            totalVelocity += deltaPosition / Time.deltaTime;
        }

        return totalVelocity / (handPositions.Count - 1);
    }

    Vector3 CalculateHandSpin()
    {
        if (handRotations.Count < 2) return Vector3.zero;

        Vector3 totalSpin = Vector3.zero;
        Vector3[] rotationsArray = handRotations.ToArray();

        for (int i = 1; i < rotationsArray.Length; i++)
        {
            Vector3 deltaRotation = rotationsArray[i] - rotationsArray[i - 1];
            totalSpin += deltaRotation / Time.deltaTime;
        }

        return totalSpin / (handRotations.Count - 1);
    }
}
