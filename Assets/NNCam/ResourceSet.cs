using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

public enum Architecture { MobileNetV1, ResNet50 }

[CreateAssetMenu(fileName = "NNCam",
                 menuName = "ScriptableObjects/NNCam Resource Set")]
public sealed class ResourceSet : ScriptableObject
{
    public NNModel model;
    public Architecture architecture;
    public ComputeShader preprocess;
    public Shader postprocess;
}

} // namespace NNCam
