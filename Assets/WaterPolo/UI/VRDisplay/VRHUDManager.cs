using UnityEngine;
using TMPro;
using WaterPolo.Core;

namespace WaterPolo.UI
{
    /// <summary>
    /// Manages VR HUD display.
    /// Can be toggled on/off for immersion or information.
    /// </summary>
    public class VRHUDManager : MonoBehaviour
    {
        [Header("Display Mode")]
        [SerializeField] private HUDMode _currentMode = HUDMode.Minimal;

        [Header("HUD Panels")]
        [SerializeField] private GameObject _minimalHUD;    // Shot clock only
        [SerializeField] private GameObject _standardHUD;   // Score + time
        [SerializeField] private GameObject _fullHUD;       // All info

        [Header("References")]
        [SerializeField] private Transform _vrCamera;
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private ScoreTable _scoreTable;

        [Header("Positioning")]
        [SerializeField] private float _hudDistance = 2f;
        [SerializeField] private Vector3 _hudOffset = new Vector3(0, -0.5f, 0);
        [SerializeField] private bool _followGaze = false;
        [SerializeField] private float _followSpeed = 3f;

        [Header("Visibility")]
        [SerializeField] private float _fadeDistance = 10f;  // Distance to fade out
        [SerializeField] private bool _autoHide = false;     // Hide during active play

        [Header("Input")]
        [SerializeField] private OVRInput.Button _toggleButton = OVRInput.Button.One;

        private CanvasGroup _currentCanvasGroup;
        private float _lastToggleTime = 0f;
        private const float TOGGLE_COOLDOWN = 0.5f;

        #region Unity Lifecycle

        private void Awake()
        {
            // Find VR camera if not assigned
            if (_vrCamera == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    _vrCamera = mainCam.transform;
            }

            // Find references
            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            if (_scoreTable == null)
                _scoreTable = FindObjectOfType<ScoreTable>();

            // Set initial mode
            SetHUDMode(_currentMode);
        }

        private void Update()
        {
            // Handle input
            HandleInput();

            // Update HUD position
            if (_followGaze && _vrCamera != null)
            {
                UpdateHUDPosition();
            }

            // Auto-hide logic
            if (_autoHide)
            {
                UpdateAutoHide();
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Toggle HUD mode with button press
            if (Time.time - _lastToggleTime > TOGGLE_COOLDOWN)
            {
                if (OVRInput.GetDown(_toggleButton))
                {
                    CycleHUDMode();
                    _lastToggleTime = Time.time;
                }
            }
        }

        #endregion

        #region HUD Mode Management

        /// <summary>
        /// Set HUD display mode.
        /// </summary>
        public void SetHUDMode(HUDMode mode)
        {
            _currentMode = mode;

            // Disable all
            if (_minimalHUD != null) _minimalHUD.SetActive(false);
            if (_standardHUD != null) _standardHUD.SetActive(false);
            if (_fullHUD != null) _fullHUD.SetActive(false);

            // Enable selected
            GameObject activeHUD = null;

            switch (mode)
            {
                case HUDMode.Hidden:
                    // All disabled
                    break;

                case HUDMode.Minimal:
                    if (_minimalHUD != null)
                    {
                        _minimalHUD.SetActive(true);
                        activeHUD = _minimalHUD;
                    }
                    break;

                case HUDMode.Standard:
                    if (_standardHUD != null)
                    {
                        _standardHUD.SetActive(true);
                        activeHUD = _standardHUD;
                    }
                    break;

                case HUDMode.Full:
                    if (_fullHUD != null)
                    {
                        _fullHUD.SetActive(true);
                        activeHUD = _fullHUD;
                    }
                    break;
            }

            // Get canvas group for fading
            if (activeHUD != null)
            {
                _currentCanvasGroup = activeHUD.GetComponent<CanvasGroup>();
            }

            Debug.Log($"VR HUD mode: {mode}");
        }

        /// <summary>
        /// Cycle through HUD modes.
        /// </summary>
        public void CycleHUDMode()
        {
            HUDMode nextMode = _currentMode;

            switch (_currentMode)
            {
                case HUDMode.Hidden:
                    nextMode = HUDMode.Minimal;
                    break;

                case HUDMode.Minimal:
                    nextMode = HUDMode.Standard;
                    break;

                case HUDMode.Standard:
                    nextMode = HUDMode.Full;
                    break;

                case HUDMode.Full:
                    nextMode = HUDMode.Hidden;
                    break;
            }

            SetHUDMode(nextMode);
        }

        #endregion

        #region HUD Positioning

        private void UpdateHUDPosition()
        {
            if (_vrCamera == null) return;

            GameObject activeHUD = GetActiveHUD();
            if (activeHUD == null) return;

            // Calculate target position
            Vector3 targetPosition = _vrCamera.position +
                (_vrCamera.forward * _hudDistance) +
                _hudOffset;

            // Smooth follow
            activeHUD.transform.position = Vector3.Lerp(
                activeHUD.transform.position,
                targetPosition,
                Time.deltaTime * _followSpeed
            );

            // Always face camera
            activeHUD.transform.LookAt(_vrCamera.position);
            activeHUD.transform.Rotate(0, 180f, 0); // Flip to face player
        }

        private GameObject GetActiveHUD()
        {
            switch (_currentMode)
            {
                case HUDMode.Minimal:
                    return _minimalHUD;
                case HUDMode.Standard:
                    return _standardHUD;
                case HUDMode.Full:
                    return _fullHUD;
                default:
                    return null;
            }
        }

        #endregion

        #region Auto-Hide

        private void UpdateAutoHide()
        {
            // Fade out HUD when far from action or during active play
            // Simplified for Phase 5

            if (_currentCanvasGroup == null) return;

            // Always visible for now (full implementation in future)
            _currentCanvasGroup.alpha = 1f;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show HUD temporarily (for important events).
        /// </summary>
        public void ShowHUDTemporarily(float duration)
        {
            if (_currentMode == HUDMode.Hidden)
            {
                SetHUDMode(HUDMode.Standard);
                Invoke(nameof(HideHUD), duration);
            }
        }

        private void HideHUD()
        {
            SetHUDMode(HUDMode.Hidden);
        }

        #endregion
    }

    /// <summary>
    /// HUD display modes for VR.
    /// </summary>
    public enum HUDMode
    {
        Hidden,     // No HUD (maximum immersion)
        Minimal,    // Shot clock only (peripheral)
        Standard,   // Score + time
        Full        // All information (stats, exclusions, etc.)
    }
}
