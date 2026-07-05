using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TransitionTextJumpEffect : BaseMeshEffect
{
    [SerializeField] private float characterInterval = 0.06f;
    [SerializeField] private float jumpDuration = 0.22f;
    [SerializeField] private float jumpAmount = 18f;

    private readonly List<float> characterRevealTimes = new List<float>();
    private Text targetText;
    private float playStartTime;

    protected override void Awake()
    {
        base.Awake();
        targetText = GetComponent<Text>();
    }

    public IEnumerator Play(string message)
    {
        yield return Play(message, characterInterval, jumpDuration, jumpAmount);
    }

    public IEnumerator Play(string message, float revealInterval, float revealJumpDuration, float revealJumpAmount)
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }

        characterInterval = Mathf.Max(0.01f, revealInterval);
        jumpDuration = Mathf.Max(0.01f, revealJumpDuration);
        jumpAmount = revealJumpAmount;

        characterRevealTimes.Clear();
        playStartTime = Time.unscaledTime;
        targetText.text = string.Empty;
        SetTextAlpha(1f);

        for (int i = 0; i < message.Length; i++)
        {
            characterRevealTimes.Add(Time.unscaledTime);
            targetText.text = message.Substring(0, i + 1);
            float revealTime = Time.unscaledTime;
            while (Time.unscaledTime - revealTime < characterInterval)
            {
                targetText.SetVerticesDirty();
                yield return null;
            }
        }

        float lastRevealTime = Time.unscaledTime;
        while (Time.unscaledTime - lastRevealTime < jumpDuration)
        {
            targetText.SetVerticesDirty();
            yield return null;
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || characterRevealTimes.Count == 0)
        {
            return;
        }

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);
        int characterCount = Mathf.Min(characterRevealTimes.Count, vertices.Count / 6);
        for (int i = 0; i < characterCount; i++)
        {
            float age = Time.unscaledTime - characterRevealTimes[i];
            if (age < 0f || age > jumpDuration)
            {
                continue;
            }

            float jump = Mathf.Sin(age / jumpDuration * Mathf.PI) * jumpAmount;
            Vector3 offset = new Vector3(0f, jump, 0f);
            int start = i * 6;
            for (int j = 0; j < 6; j++)
            {
                UIVertex vertex = vertices[start + j];
                vertex.position += offset;
                vertices[start + j] = vertex;
            }
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }

    private void SetTextAlpha(float alpha)
    {
        Color color = targetText.color;
        color.a = alpha;
        targetText.color = color;
    }
}
