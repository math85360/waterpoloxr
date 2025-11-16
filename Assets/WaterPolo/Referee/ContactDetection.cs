using UnityEngine;
using WaterPolo.Players;

namespace WaterPolo.Referee
{
    /// <summary>
    /// Contact zones relative to water surface.
    /// Affects visibility to referee and foul severity.
    /// </summary>
    public enum ContactZone
    {
        AboveWater,      // Clearly visible
        AtWaterLevel,    // Partially visible
        Underwater       // Hidden from referee
    }

    /// <summary>
    /// Types of physical contact in water polo.
    /// </summary>
    public enum ContactType
    {
        None,
        Push,
        Pull,
        Grab,
        Hold,
        Strike,
        Sink           // Forcing player underwater
    }

    /// <summary>
    /// Represents a contact event between two players.
    /// </summary>
    public class ContactEvent
    {
        public WaterPoloPlayer Initiator { get; private set; }
        public WaterPoloPlayer Receiver { get; private set; }
        public ContactType Type { get; private set; }
        public Vector3 ContactPoint { get; private set; }
        public float Force { get; private set; } // 0-1
        public ContactZone Zone { get; private set; }
        public float Duration { get; set; }  // Public setter for ContactDetection updates
        public float Timestamp { get; private set; }

        public ContactEvent(WaterPoloPlayer initiator, WaterPoloPlayer receiver,
            ContactType type, Vector3 point, float force, ContactZone zone)
        {
            Initiator = initiator;
            Receiver = receiver;
            Type = type;
            ContactPoint = point;
            Force = Mathf.Clamp01(force);
            Zone = zone;
            Duration = 0f;
            Timestamp = Time.time;
        }

        /// <summary>
        /// Calculate foul severity based on contact characteristics.
        /// </summary>
        public float CalculateSeverity()
        {
            float severity = Force;

            // Type multipliers
            switch (Type)
            {
                case ContactType.Strike:
                    severity *= 2f; // Very severe
                    break;
                case ContactType.Sink:
                    severity *= 1.8f;
                    break;
                case ContactType.Hold:
                    severity *= 1.3f;
                    break;
                case ContactType.Grab:
                    severity *= 1.2f;
                    break;
                case ContactType.Pull:
                    severity *= 1.1f;
                    break;
            }

            // Zone affects detection, not severity directly
            // But underwater fouls are more severe if detected (deliberate)
            if (Zone == ContactZone.Underwater)
            {
                severity *= 1.2f; // Deliberate hidden foul
            }

            // Duration increases severity
            severity += Duration * 0.1f;

            return Mathf.Clamp01(severity);
        }

        /// <summary>
        /// Check if referee can see this contact.
        /// </summary>
        public float GetVisibility(RefereeProfile profile)
        {
            float visibility = profile.visionAccuracy;

            // Zone affects visibility
            switch (Zone)
            {
                case ContactZone.AboveWater:
                    // Full visibility
                    break;

                case ContactZone.AtWaterLevel:
                    visibility *= 0.7f;
                    break;

                case ContactZone.Underwater:
                    visibility *= 0.3f; // Hard to see
                    break;
            }

            // Force makes contact more noticeable
            visibility += Force * 0.2f;

            return Mathf.Clamp01(visibility);
        }
    }

    /// <summary>
    /// Detects and analyzes physical contact between players.
    /// Provides data to RefereeSystem for foul calling.
    /// </summary>
    public class ContactDetection : MonoBehaviour
    {
        [Header("Water Level")]
        [SerializeField] private float _waterSurfaceHeight = 0f;
        [SerializeField] private float _waterLevelTolerance = 0.2f;

        [Header("Detection Settings")]
        [SerializeField] private float _contactForceThreshold = 0.1f;
        [SerializeField] private float _contactDurationThreshold = 0.5f; // Sustained contact

        [Header("References")]
        [SerializeField] private RefereeSystem _refereeSystem;

        private System.Collections.Generic.Dictionary<string, ContactEvent> _activeContacts =
            new System.Collections.Generic.Dictionary<string, ContactEvent>();

        #region Unity Lifecycle

        private void Awake()
        {
            if (_refereeSystem == null)
                _refereeSystem = FindObjectOfType<RefereeSystem>();
        }

        private void Update()
        {
            // Update active contact durations
            var contactsToRemove = new System.Collections.Generic.List<string>();

            foreach (var kvp in _activeContacts)
            {
                kvp.Value.Duration += Time.deltaTime;

                // Check if contact ended (players separated)
                if (kvp.Value.Initiator == null || kvp.Value.Receiver == null)
                {
                    contactsToRemove.Add(kvp.Key);
                    continue;
                }

                float distance = Vector3.Distance(
                    kvp.Value.Initiator.transform.position,
                    kvp.Value.Receiver.transform.position
                );

                if (distance > 1.5f)
                {
                    // Contact ended
                    EvaluateContact(kvp.Value);
                    contactsToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in contactsToRemove)
            {
                _activeContacts.Remove(key);
            }
        }

        #endregion

        #region Contact Detection

        /// <summary>
        /// Register a contact between two players.
        /// Called by collision detection or player interaction systems.
        /// </summary>
        public void RegisterContact(WaterPoloPlayer initiator, WaterPoloPlayer receiver,
            ContactType type, Vector3 contactPoint, float force)
        {
            if (initiator == null || receiver == null)
                return;

            // Same team contacts are usually not fouls (except in specific cases)
            if (initiator.TeamName == receiver.TeamName)
                return;

            // Determine contact zone
            ContactZone zone = DetermineContactZone(contactPoint);

            // Create or update contact event
            string contactKey = $"{initiator.GetInstanceID()}_{receiver.GetInstanceID()}";

            if (_activeContacts.ContainsKey(contactKey))
            {
                // Update existing contact
                ContactEvent existing = _activeContacts[contactKey];
                existing.Duration += Time.deltaTime;

                // If contact type escalated (e.g., push → strike), update
                if ((int)type > (int)existing.Type)
                {
                    ContactEvent updated = new ContactEvent(initiator, receiver, type,
                        contactPoint, Mathf.Max(force, existing.Force), zone);
                    updated.Duration = existing.Duration;
                    _activeContacts[contactKey] = updated;
                }
            }
            else
            {
                // New contact
                ContactEvent contact = new ContactEvent(initiator, receiver, type,
                    contactPoint, force, zone);
                _activeContacts[contactKey] = contact;
            }
        }

        private ContactZone DetermineContactZone(Vector3 position)
        {
            float heightAboveWater = position.y - _waterSurfaceHeight;

            if (heightAboveWater > _waterLevelTolerance)
                return ContactZone.AboveWater;

            if (heightAboveWater > -_waterLevelTolerance)
                return ContactZone.AtWaterLevel;

            return ContactZone.Underwater;
        }

        #endregion

        #region Foul Evaluation

        private void EvaluateContact(ContactEvent contact)
        {
            // Check if contact warrants foul
            if (contact.Force < _contactForceThreshold)
                return; // Too minor

            // Determine if this is a foul
            FoulType foulType = DetermineFoulType(contact);

            if (foulType == FoulType.None)
                return;

            // Create foul event
            FoulEvent foul = new FoulEvent(
                foulType,
                contact.Initiator,
                contact.Receiver,
                contact.ContactPoint,
                contact.CalculateSeverity()
            );

            // Special cases
            foul.isBrutality = (contact.Type == ContactType.Strike && contact.Force > 0.7f);
            foul.preventedGoalOpportunity = CheckGoalOpportunityPrevented(contact);

            // Report to referee
            if (_refereeSystem != null)
            {
                _refereeSystem.ReportFoul(foul);
            }

            Debug.Log($"Foul detected: {foulType} by {contact.Initiator.PlayerName} on {contact.Receiver.PlayerName} (severity: {foul.severity:F2})");
        }

        private FoulType DetermineFoulType(ContactEvent contact)
        {
            switch (contact.Type)
            {
                case ContactType.Strike:
                    return FoulType.Brutality;

                case ContactType.Sink:
                    return FoulType.Sinking;

                case ContactType.Hold:
                    if (contact.Duration > _contactDurationThreshold)
                        return FoulType.Holding;
                    break;

                case ContactType.Grab:
                    return FoulType.Holding;

                case ContactType.Pull:
                    if (contact.Force > 0.5f)
                        return FoulType.Holding;
                    break;

                case ContactType.Push:
                    if (contact.Force > 0.6f)
                        return FoulType.PushingOff;
                    break;
            }

            return FoulType.None;
        }

        private bool CheckGoalOpportunityPrevented(ContactEvent contact)
        {
            // Check if receiver was in shooting position
            if (contact.Receiver == null || !contact.Receiver.HasBall)
                return false;

            // Simple check: was receiver facing goal and close?
            // Full implementation would check shooting stance, distance, etc.

            // Placeholder for Phase 4
            return false;
        }

        #endregion

        #region Specific Foul Detection

        /// <summary>
        /// Detect wrist grabbing (specific tactic).
        /// </summary>
        public void DetectWristGrab(WaterPoloPlayer defender, WaterPoloPlayer attacker, Transform wrist)
        {
            if (defender == null || attacker == null || wrist == null)
                return;

            float distance = Vector3.Distance(defender.GetArmPosition(ArmSide.Right), wrist.position);

            if (distance < 0.15f) // Very close to wrist
            {
                // Likely grabbing wrist
                RegisterContact(defender, attacker, ContactType.Grab,
                    wrist.position, 0.6f);

                Debug.Log($"{defender.PlayerName} grabbing {attacker.PlayerName}'s wrist!");
            }
        }

        /// <summary>
        /// Detect simulation (attacker faking foul).
        /// </summary>
        public bool DetectSimulation(WaterPoloPlayer player, GameObject ball)
        {
            // Simulation detection:
            // 1. Player releases ball
            // 2. Player dives/sinks
            // 3. No significant contact detected

            // This would require animation analysis and contact history
            // Placeholder for Phase 4

            return false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set water surface height for zone detection.
        /// </summary>
        public void SetWaterSurfaceHeight(float height)
        {
            _waterSurfaceHeight = height;
        }

        #endregion
    }
}
