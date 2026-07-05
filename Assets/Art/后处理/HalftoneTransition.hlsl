void Halftone_float(float2 UV, float4 ToneColor, float CellCount, float Threshold, float Progress,
    float4 TexColor, float AspectRatio, UnityTexture2D PaperTex, out float4 Color)
{
    float2 uv = UV;
    uv.x *= AspectRatio;

    float2 uvf = frac(uv * CellCount);
    float2 uvi = floor(uv * CellCount);
    float2 expandRange = float2(AspectRatio, 1.0) * CellCount;

    float2 mosaicUV = (uvi + 0.5) / expandRange;
    float gray = dot(TexColor.rgb, float3(0.299, 0.587, 0.114));

    float circleMin = lerp(0.0, 0.4, step(Threshold, gray));
    float circleMax = circleMin + lerp(0.05, 0.1, step(Threshold, gray));

    circleMin *= Progress;
    circleMax *= Progress;

    float d = distance(uvf, float2(0.5, 0.5));
    float v = 1.0 - smoothstep(circleMin, circleMax, d);

    // 采样纸纹，用原始 UV（也可以用屏幕 UV，看你想要纸纹怎么贴）
    float4 paperColor = SAMPLE_TEXTURE2D(PaperTex, PaperTex.samplerstate, UV);
    // 纸纹跟色调相乘，做出肌理感
    float4 paperTone = ToneColor * paperColor;

    // 半调子区域 = 纸纹色调，原图区域 = 原图
    float4 finalColor = lerp(TexColor, paperTone, v * ToneColor.a);
    Color = finalColor;
}