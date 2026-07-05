void Halftone_float(float2 UV, float4 ToneColor, float CellCount, float Threshold, float Progress, 
    UnityTexture2D MainTex, float AspectRatio, out float4 Color)
{
    float2 uv = UV;
    uv.x *= AspectRatio;

    // 切分成网格
    float2 uvf = frac(uv * CellCount);
    float2 uvi = floor(uv * CellCount);
    float2 expandRange = float2(AspectRatio, 1.0) * CellCount;

    // ★ 关键：在网格中心采样纹理
    float2 mosaicUV = (uvi + 0.5) / expandRange;
    float4 texCol = SAMPLE_TEXTURE2D(MainTex, MainTex.samplerstate, mosaicUV);
    float gray = dot(texCol.rgb, float3(0.299, 0.587, 0.114));

    // 暗部用小圆，亮部用大圆
    float circleMin = lerp(0.2, 0.4, step(Threshold, gray));
    float circleMax = circleMin + lerp(0.05, 0.1, step(Threshold, gray));

    // Progress 控制转场进度
    circleMin *= Progress;
    circleMax *= Progress;

    float d = distance(uvf, float2(0.5, 0.5));
    float v = 1.0 - smoothstep(circleMin, circleMax, d);

    float4 finalColor = lerp(texCol, ToneColor, v * ToneColor.a);
    Color = finalColor;
}