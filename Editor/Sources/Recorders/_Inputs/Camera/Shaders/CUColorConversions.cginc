/**
 * consolidations of basic color functionalities required by the
 * processes used in the output/input feature of BMD.
 */

// --- Input Float Path ---
float3 RGB_INPUT(float3 rgb, bool isFullRange)
{
    if (!isFullRange)
    {
        rgb = UnclipSignal(rgb);
    }
#ifdef BT601
#ifdef WORKING_SPACE_CONVERSION
    // 601 -> 709
    rgb = float3(
        /*r*/ 1.0865 * rgb.r - 0.072433 * rgb.g - 0.014067 * rgb.b,
        /*g*/ 0.0966428 * rgb.r + 0.844833 * rgb.g + 0.0585243 * rgb.b,
        /*b*/ -0.0141401 * rgb.r - 0.0277599 * rgb.g + 1.0419 * rgb.b
    );
#endif

#elif defined (BT2020)
#ifdef WORKING_SPACE_CONVERSION
    // BT 2407 2.2 (RGB 2020-> RGB 709)
    rgb = float3(
        /*r*/ 1.6605 * rgb.r - .5876 * rgb.g - .0728 * rgb.b,
        /*g*/ -.1246 * rgb.r + 1.1329 * rgb.g - .0083 * rgb.b,
        /*b*/ -.0182 * rgb.r + -.1006 * rgb.g + 1.1187 * rgb.b
    );
#endif
#endif

    rgb = GammaToLinearSpaceBMD(rgb);
#ifdef INPUT_IS_SRGB
    return LinearToGammaSpace(rgb);
#else
    return rgb;
#endif
}

// --- Input Float Path ---
float3 YUV2RGB(float3 yuv)
{
    float y = yuv.x, u = yuv.y, v = yuv.z;
    float3 rgb = float3(0,0,0);

#ifdef BT601

#ifdef WORKING_SPACE_CONVERSION

    rgb = float3(
        /*r*/ y + 1.402 * v,
        /*g*/ y - (0.299 * 1.402 / 0.587) * u - (0.114 * 1.772 / 0.587) * v,
        /*b*/ y + 1.772 * u
    );
#else
    rgb = float3(
        /*r*/ y + 1.5748 * v,
        /*g*/ y - .1873 * u - .4681 * v,
        /*b*/ y + 1.8556 * u
    );
#endif

#elif defined (BT709) // 709

    // 709 With unclipping
    rgb = float3(
        /*r*/ y + 1.5748 * v,
        /*g*/ y - (0.2126 * 1.5748 / 0.7152) * v - (0.0722 * 1.8556 / 0.7152) * u,
        /*b*/ y + 2.12798 * u
    );

#else // 2020

    // bt.2020 YUV to 2020 RGB
    rgb = float3(
        /*r*/ y + 1.4747 * v,
        /*g*/ y - (0.2627 * 1.4747 / 0.6780) * u - (0.0593 * 1.8814 / 0.6780) * v,
        /*b*/ y + 1.8814 * u
    );

    #ifdef WORKING_SPACE_CONVERSION
    // BT 2407 2.2 (2020->709)
    rgb = float3(
        /*r*/ 1.6605 * rgb.r - .5876 * rgb.g - .0728 * rgb.b,
        /*g*/ -.1246 * rgb.r + 1.1329 * rgb.g - .0083 * rgb.b,
        /*b*/ -.0182 * rgb.r + -.1006 * rgb.g + 1.1187 * rgb.b
    );
    #endif

#endif

    // Assumption in 709 is that RGB is Gamma at this stage.
    rgb = GammaToLinearSpaceBMD(rgb);
#ifdef INPUT_IS_SRGB
    return LinearToGammaSpace(rgb);
#else
    return rgb;
#endif
}

// Input conversion
// Adobe-flavored HDTV Rec.709 (2.2 gamma, 16-235 limit)
half3 YUV2RGB_8BITS(half3 yuv)
{
    half3 rgb = half3(0,0,0);

#ifdef BT709
    const half K_B = 0.0722;
    const half K_R = 0.2126;

    // Unclip Signal
    const half y = (yuv.x - 16.0 / 255.0) * 255.0 / 219.0;
    const half u = (yuv.y - .5) * 255.0 / 112.0;
    const half v = (yuv.z - .5) * 255.0 / 112.0;

    rgb = half3(
        /*r*/ y + v * (1 - K_R),
        /*g*/ y - v * K_R / (1 - K_R) - u * K_B / (1 - K_B),
        /*b*/ y + u * (1 - K_B)
    );

    // Assumption in 709 is that RGB is Gamma at this stage.
    rgb = GammaToLinearSpaceBMD(rgb);
#ifdef INPUT_IS_SRGB
    rgb = LinearToGammaSpace(rgb);
#endif

#else
    rgb = YUV2RGB(float3(yuv.r, yuv.g - .5, yuv.b - .5));
#endif

    return rgb;
}

// --- Output Float Path ---
float3 RGB_OUTPUT(float3 rgb, bool isFullRange)
{
#ifdef INPUT_IS_SRGB
    rgb = GammaToLinearSpace(rgb);
#endif
    rgb = LinearToGammaSpaceBMD(rgb);

#ifdef BT601
    rgb = float3(
        /*r*/ .9135 * rgb.r + .0785804 * rgb.g + .00791956 * rgb.b,
        /*g*/ -.105163 * rgb.r + 1.17244 * rgb.g - .0672766 * rgb.b,
        /*b*/ .00959559 * rgb.r + .0323044 * rgb.g + .9581 * rgb.b
    );
#ifdef WORKING_SPACE_CONVERSION

#endif // ELSE ; leave as is

#elif defined (BT2020)
#ifdef WORKING_SPACE_CONVERSION

    // BT 2087 M2 (2020->709)
    rgb = float3(
        /*r*/ .6274 * rgb.r + .3293 * rgb.g + .0433 * rgb.b,
        /*g*/ .0691 * rgb.r + .9195 * rgb.g + .0114 * rgb.b,
        /*b*/ .0164 * rgb.r + .088 * rgb.g + .8956 * rgb.b
    );

#endif // ELSE ; leave as is.
#endif

    if (isFullRange)
    {
        return rgb;
    }
    else
    {
        return ClipSignal(rgb);
    }
}

// --- Output Float Path ---
float3 RGB2YUV(float3 rgb)
{
    float3 yuv = float3(0.5, 0.5, 0.5);

#ifdef INPUT_IS_SRGB
    rgb = GammaToLinearSpace(rgb);
#endif
    rgb = LinearToGammaSpaceBMD(rgb);

#ifdef BT601
#ifdef WORKING_SPACE_CONVERSION

    yuv = float3(
        /*y*/ .299 * rgb.r + .587 * rgb.g + .114 * rgb.b,
        /*u*/ -.168736 * rgb.r + -.331264 * rgb.g + .5 * rgb.b + .5,
        /*v*/ .5 * rgb.r + -.418688 * rgb.g - .081312 * rgb.b + .5
    );

#else
    yuv = float3(
        /*y*/ .299 * rgb.r + .587 * rgb.g + .114 * rgb.b,
        /*u*/ -.168736 * rgb.r + -.331264 * rgb.g + .5 * rgb.b + .5,
        /*v*/ .5 * rgb.r + -.418688 * rgb.g - .081312 * rgb.b + .5
    );
#endif

#elif defined (BT709)
    // RGB -> YUV
    const float K_B = 0.0722;
    const float K_R = 0.2126;
    const float y = dot(float3(K_R, 1 - K_B - K_R, K_B), rgb);
    yuv = float3(
        /*y*/ y,
        /*u*/ ((rgb.b - y) / (1 - K_B) * 112 + 128) / 255,
        /*v*/ ((rgb.r - y) / (1 - K_R) * 112 + 128) / 255
    );

#else // BT2020

#ifdef WORKING_SPACE_CONVERSION
    // BT.2087 M3 (BT709 -> BT2020)
    yuv = float3(
        /*y*/ 0.2627 * rgb.r + 0.6780 * rgb.g + 0.0593 * rgb.b,
        /*u*/ -0.2627/1.8814 * rgb.r + -0.6780/1.8814 * rgb.g + 0.5 * rgb.b + .5,
        /*v*/ 0.5 * rgb.r + -0.6780/1.4746 * rgb.g + -0.0593/1.4746 * rgb.b + .5
    );
#else
    // YCbCr -> RGB 2020:2020 (BT2100.2 Table 6)
    float y = 0.2627 * rgb.r + 0.6780 * rgb.g + 0.0593 * rgb.b;
    yuv = float3(
        /*y*/ y,
        /*u*/ (rgb.b - y) / 1.8814 + .5,
        /*v*/ (rgb.r - y) / 1.4747 + .5
    );
#endif

#endif

#if defined (YUV10Bit)
    // clipping
    yuv = ClipYUVSignal(yuv);
#endif
    return yuv;
}

// Adobe-flavored HDTV Rec.709 (2.2 gamma, 16-235 limit)
half3 RGB2YUV_8BITS(half3 rgb)
{
    half3 yuv = half3(0.5, 0.5, 0.5);

#ifdef BT709 // 601 and 2020 use float path
    #ifdef INPUT_IS_SRGB
    #else
    rgb = GammaToLinearSpace(rgb);
    #endif
    rgb = LinearToGammaSpaceBMD(rgb);

    const half K_B = 0.0722;
    const half K_R = 0.2126;
    const half y = dot(half3(K_R, 1 - K_B - K_R, K_B), rgb);
    yuv = half3(
        /*y*/ y,
        /*u*/ ((rgb.b - y) / (1 - K_B) * 112 + 128) / 255,
        /*v*/ ((rgb.r - y) / (1 - K_R) * 112 + 128) / 255
    );

    // 8bit clipping
    yuv = ClipYUVSignal(yuv);

#else
    yuv = RGB2YUV(rgb);
#endif

    return yuv;
}
