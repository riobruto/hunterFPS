using UnityEngine;

public class SoldierAgentSkin : MonoBehaviour
{
    [SerializeField] private GameObject _bodyParts;

    [SerializeField] private Material[] _skinMaterials;
    [SerializeField] private SkinnedMeshRenderer _face;
    private void Start() {
        Randomize();
    }

    private void Randomize() {
        _face.material = _skinMaterials[Random.Range(0, _skinMaterials.Length)];
    }
}
