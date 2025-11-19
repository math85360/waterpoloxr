using UnityEngine;
using TMPro;
using WaterPolo.Players;

[ExecuteAlways]
public class PlayerNameDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro _textMeshPro;

    private WaterPoloPlayer _player;
    private Transform _cameraTransform;

    void OnEnable()
    {
        // Get the WaterPoloPlayer component
        _player = GetComponent<WaterPoloPlayer>();

        // Set the player name
        UpdateName();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Delay to avoid issues during serialization
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            _player = GetComponent<WaterPoloPlayer>();
            UpdateName();
        };
    }
#endif

    void LateUpdate()
    {
        if (_textMeshPro == null) return;

        // Find camera (Scene view camera in editor, Main camera at runtime)
        if (_cameraTransform == null || !_cameraTransform.gameObject.activeInHierarchy)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Use Scene view camera in editor
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.camera != null)
                {
                    _cameraTransform = sceneView.camera.transform;
                }
            }
            else
#endif
            {
                _cameraTransform = Camera.main?.transform;
            }
        }

        if (_cameraTransform == null) return;

        // Billboard effect - always face the camera
        // Make the text look at the camera position
        Vector3 directionToCamera = _cameraTransform.position - _textMeshPro.transform.position;
        if (directionToCamera != Vector3.zero)
        {
            _textMeshPro.transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    /// <summary>
    /// Update the displayed name from the WaterPoloPlayer component.
    /// </summary>
    public void UpdateName()
    {
        if (_textMeshPro == null || _player == null) return;

        _textMeshPro.text = _player.PlayerName;
    }

    /// <summary>
    /// Set a custom name to display.
    /// </summary>
    public void SetName(string name)
    {
        if (_textMeshPro == null) return;

        _textMeshPro.text = name;
    }


    /// <summary>
    /// Show or hide the name display.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_textMeshPro != null)
        {
            _textMeshPro.gameObject.SetActive(visible);
        }
    }
}
