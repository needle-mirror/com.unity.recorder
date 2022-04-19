/**
 * Entry point for the packing/unpacking processes used in the output/input feature of BMD.
 */

// Big-endian by default, swaps bytes for little-endian.
// Expects 32-bit 4-component vectors.
#ifndef RGBLE12Bit
#define COMPONENT_R(vec) vec##.a
#define COMPONENT_G(vec) vec##.b
#define COMPONENT_B(vec) vec##.g
#define COMPONENT_A(vec) vec##.r
#else
#define COMPONENT_R(vec) vec##.r
#define COMPONENT_G(vec) vec##.g
#define COMPONENT_B(vec) vec##.b
#define COMPONENT_A(vec) vec##.a
#endif

#define TWO_TO_THE_4 16.0
#define TWO_TO_THE_8 256.0

// Remaps normalized 0-1 float to 0-255 int
int4 floatToInt8(float4 value)
{
    return int4(value * 255.0 + 0.5);
}

// Remaps normalized 0-1 float to 0-4095 int
int3 floatToInt12(float3 value)
{
    return int3(value * 4095.0 + 0.5);
}

// Remaps 0-255 int to 0-1 float
float4 int8ToNormalizedFloat(int4 value)
{
    return value / 255.0;
}

// Remaps 0-4095 int to 0-1 float
float3 int12ToNormalizedFloat(int3 value)
{
    return value / 4095.0;
}

float packFloatClamp(float value, float numBits)
{
    return fmod(value, numBits);
}

float packFloatLeftShift(float value, float numShift)
{
    return value * numShift;
}

float packFloatLeftShift(float value, float numBits, float numShift)
{
    return packFloatClamp(value, numBits) * numShift;
}

float packFloatRightShift(float value, float numShift)
{
    return value / numShift;
}

// Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#define FLT_EPSILON     1.192092896e-07

float3 PositivePow(float3 base, float3 power)
{
    return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

float avg(const float a, const float b)
{
    return a * .5f + b * .5f;
}

float2 getSamplingUV(float2 uv, float4 ts)
{
#if defined(BLACKMAGIC_DEINTERLACE_ODD)
    uv.y = (floor((uv.y * ts.w) / 2) * 2 + 0.5) * ts.y;
#elif defined(BLACKMAGIC_DEINTERLACE_EVEN)
    uv.y = (floor((uv.y * ts.w) / 2) * 2 + 0.5) * ts.y;
#endif

#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1 - uv.y;
#endif

    uv.x += ts.x/2.0;

    return uv;
}

// Full swing to studio swing assumes rgb between 0 and 1
float3 ClipSignal(float3 signal)
{
    // RGB -> R'G'B'
    signal = signal * (219.0 / 255.0) + (16.0 / 255.0);
    signal = clamp(signal, 0, 1);
    return signal;
}

float3 ClipYUVSignal(float3 signal)
{
    // RGB -> Y'CbCr
    signal.r = signal.r * (219.0 / 255.0) + (16.0 / 255.0);

    signal = clamp(signal, 0, 1);
    return signal;
}

// Studio swing to full swing
float3 UnclipSignal(float3 signal)
{
    // R'G'B' -> RGB
    signal = (signal - (16.0 / 255.0)) * (255.0 / 219.0);
    signal = clamp(signal, 0, 1);
    return signal;
}

float3 UnclipYUVSignal(float3 signal)
{
    // Y'CbCr-> RGB
    signal.r = (signal.r - (16.0 / 255.0)) * (255.0 / 219.0);
    signal = clamp(signal, 0, 1);
    return signal;
}

float3 GammaToLinearSpaceBMD(float3 c)
{
    float3 linearRGBLo = c / 12.92;
    float3 linearRGBHi = PositivePow((c + 0.055) / 1.055, float3(2.4, 2.4, 2.4));
    float3 linearRGB = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

float3 LinearToGammaSpaceBMD(float3 c)
{
    float3 sRGBLo = c * 12.92;
    float3 sRGBHi = (PositivePow(c, float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    float3 sRGB = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
}
