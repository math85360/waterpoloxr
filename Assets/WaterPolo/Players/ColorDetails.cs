using UnityEngine;
using WaterPolo.Players;

[ExecuteAlways]
public class ColorDetails : MonoBehaviour
{
    [Header("Hat Materials")]
    [SerializeField] private Material _homeHatMaterial;
    [SerializeField] private Material _awayHatMaterial;
    [SerializeField] private Material _homeGoalkeeperHatMaterial;
    [SerializeField] private Material _awayGoalkeeperHatMaterial;

    [Header("Swimsuit Materials")]
    [SerializeField] private Material _homeSwimSuitMaterial;
    [SerializeField] private Material _awaySwimSuitMaterial;

    [Header("Mesh Reference")]
    [Tooltip("The SkinnedMeshRenderer to apply materials to (drag Ch36 mesh here)")]
    [SerializeField] SkinnedMeshRenderer _meshRenderer;

    [Header("Material Indices")]
    [Tooltip("Index of the hat material in the renderer's materials array")]
    [SerializeField] private int _hatMaterialIndex = 0;
    [Tooltip("Index of the swimsuit material in the renderer's materials array")]
    [SerializeField] private int _swimSuitMaterialIndex = 1;

    private WaterPoloPlayer _player;

    void OnEnable()
    {
        Initialize();
        ApplyMaterials();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Delay to avoid issues during serialization
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            Initialize();
            ApplyMaterials();
        };
    }
#endif

    private void Initialize()
    {
        // Get the WaterPoloPlayer component (could be AIPlayer or VRPlayer)
        if (_player == null)
        {
            _player = GetComponent<WaterPoloPlayer>();
        }
    }

    /// <summary>
    /// Apply materials based on team name and role.
    /// </summary>
    public void ApplyMaterials()
    {
        if (_meshRenderer == null || _player == null) return;

        Material[] materials = _meshRenderer.materials;
        bool isHome = _player.TeamName.ToLower() == "home";
        bool isGoalkeeper = _player.Role == PlayerRole.Goalkeeper;

        // Apply hat material
        if (_hatMaterialIndex >= 0 && _hatMaterialIndex < materials.Length)
        {
            if (isGoalkeeper)
            {
                materials[_hatMaterialIndex] = isHome ? _homeGoalkeeperHatMaterial : _awayGoalkeeperHatMaterial;
            }
            else
            {
                materials[_hatMaterialIndex] = isHome ? _homeHatMaterial : _awayHatMaterial;
            }
        }

        // Apply swimsuit material
        if (_swimSuitMaterialIndex >= 0 && _swimSuitMaterialIndex < materials.Length)
        {
            materials[_swimSuitMaterialIndex] = isHome ? _homeSwimSuitMaterial : _awaySwimSuitMaterial;
        }

        _meshRenderer.materials = materials;

        Debug.Log($"[ColorDetails] Applied materials for {_player.PlayerName}: Team={_player.TeamName}, Role={_player.Role}");
    }

    /// <summary>
    /// Refresh materials at runtime (useful if team or role changes).
    /// </summary>
    public void RefreshMaterials()
    {
        if (_player == null)
        {
            _player = GetComponent<WaterPoloPlayer>();
        }
        ApplyMaterials();
    }
}
