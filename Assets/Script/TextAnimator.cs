using System;
using System.Collections;
using TMPro;
using UnityEngine;
namespace FaizProject.Game.Animal_Memories
{

public class TextAnimator : MonoBehaviour
{
    public TMP_Text tmpText;
    [TextArea] public string fullText = "ANIMALS MEMORY";
    public float letterDelay = 0.05f;
    public float popScale = 1.5f;
    public float popDuration = 0.25f;

    private void Start()
    {
        tmpText.text = fullText;
        InvokeRepeating(nameof(LetterPopper),1f,2f);
    }

    void LetterPopper()
    {
        if (gameObject.activeInHierarchy)
        {

            StartCoroutine(AnimateLetters());
        }
    }
    

    IEnumerator AnimateLetters()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        for (int i = 0; i < fullText.Length; i++)
        {
            // Skip invisible characters (like spaces)
            if (!textInfo.characterInfo[i].isVisible)
            {
                yield return new WaitForSeconds(letterDelay);
                continue;
            }

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
            Vector3[] originalVerts = new Vector3[4];
            for (int j = 0; j < 4; j++)
                originalVerts[j] = sourceVertices[vertexIndex + j];

            Vector3 center = (originalVerts[0] + originalVerts[2]) / 2f;

            StartCoroutine(PopLetter(i, vertexIndex, center, originalVerts, materialIndex));

            yield return new WaitForSeconds(letterDelay);
        }
    }

    IEnumerator PopLetter(int charIndex, int vertexIndex, Vector3 center, Vector3[] originalVerts, int meshIndex)
    {
        float time = 0f;
        TMP_TextInfo textInfo = tmpText.textInfo;
        Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

        while (time < popDuration)
        {
            float t = time / popDuration;
            float scale = Mathf.Lerp(popScale, 1f, t * t); // EaseOut curve

            for (int j = 0; j < 4; j++)
            {
                Vector3 offset = originalVerts[j] - center;
                vertices[vertexIndex + j] = center + offset * scale;
            }

            tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        // Final reset to original
        for (int j = 0; j < 4; j++)
            vertices[vertexIndex + j] = originalVerts[j];

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
}
