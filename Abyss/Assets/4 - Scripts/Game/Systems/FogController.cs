using Meryuhi.Rendering;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class FogController : MonoBehaviour
{
    private Transform cameraTransform;
    private float checkInterval = 0.5f;
    private float distance;
    private float distanceAmplifier=10f;
    private Volume volume;
    private VolumeProfile profile;
    private FullScreenFog fog;
    [SerializeField]
    [Tooltip("Start line for fog around 500 optimal")]
    private float startLine = 500f;

    void Start()
    {
        volume = GetComponent<Volume>();
        profile = volume.profile;
        cameraTransform =this.transform;
        StartCoroutine(AdjutsFogLoop());
    }

    IEnumerator AdjutsFogLoop()
    {
        while (true)
        {
            Vector3 pos = cameraTransform.position;
            pos.y = 0f;
            distance = pos.magnitude;
            if (profile.TryGet(out fog))
            {
                fog.startLine.value = startLine-distance*distanceAmplifier;
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
}
