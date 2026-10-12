using System.Collections;
using UnityEngine;

public class RandomBlendShape : MonoBehaviour
{
    public SkinnedMeshRenderer smr;
    public string[] shapeNames = { "Mr.Beast", "Badstuff", "Goodstuff" };
    public float interval = 5f;   // time between picks
    public float blendTime = 1f;  // seconds for 0->100 (and again for 100->0)

    IEnumerator Start()
    {
        if (!smr) smr = GetComponent<SkinnedMeshRenderer>();

        while (true)
        {
            string pick = shapeNames[Random.Range(0, shapeNames.Length)];
            int idx = smr.sharedMesh.GetBlendShapeIndex(pick);

            if (idx >= 0)
            {
                yield return Blend(idx, 0f, 100f);
                yield return Blend(idx, 100f, 0f);
            }
            else Debug.LogWarning($"Blend shape '{pick}' not found");

            // keep the 5s rhythm; if blending takes longer, the next pick follows right after
            yield return new WaitForSeconds(Mathf.Max(0f, interval - blendTime * 2f));
        }
    }

    IEnumerator Blend(int idx, float from, float to)
    {
        float t = 0f;
        while (t < blendTime)
        {
            t += Time.deltaTime;
            smr.SetBlendShapeWeight(idx, Mathf.Lerp(from, to, t / blendTime));
            yield return null;
        }
        smr.SetBlendShapeWeight(idx, to);
    }
}
