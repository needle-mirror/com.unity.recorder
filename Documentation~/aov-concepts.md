# Record AOVs

AOV recording is a process that extracts specific render pass data from the Scene that a Camera views. This mainly includes render passes related to the material, geometry, depth, motion, and lighting response of the GameObjects in the Scene.

You can use AOV outputs, for example, for further image compositing in an external dedicated tool.

>[!WARNING]
>The AOV Recorder only works with projects that use Unity's [HDRP (High Definition Render Pipeline)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest).

## Recording process and output format

You can record one or multiple AOVs via a single AOV Recorder. By design, an AOV Recorder output always takes the form of an image file sequence, each file corresponding to a recorded frame. When you record multiple AOVs, the Recorder allows you to get either a single file sequence with all AOVs bundled in each file or multiple file sequences each corresponding to a separate AOV type.

Depending on your needs:
* To generate a single image file sequence where each file contains all selected AOVs, use the EXR format with the "Multi-part file" option enabled.
* To generate one separate image file sequence per selected AOV, use either the EXR format with the "Multi-part file" option disabled, or the PNG format.

The PNG format is not typically an output type suitable for actual compositing, but you can still use it for quick output testing.

## AOV types available for recording

The AOV types you can record with the Recorder package are grouped in the following categories:

| AOV category | Description |
| :--- | :--- |
| Beauty | The final rendered view after post-process. This is a single AOV. |
| Material Properties | Common inherent material properties of the surfaces in view. This includes data from material values or material maps.<br />This category groups several AOVs that you can also record individually: Albedo, Alpha, Metal, Smoothness, and Specular. |
| Lighting | All lighting modes that contribute to the Beauty. These specific AOVs are for light decomposition purposes. The recorded data depends on the current lighting of the Scene and on the surface materials, and includes shadows.<br />This category groups several AOVs that you can also record individually: Direct Diffuse, Direct Specular, Emissive, Indirect Diffuse, Reflection, and Refraction. |
| Utility | Various data computed from the Scene.<br />This category groups several AOVs that you can also record individually: Ambient Occlusion, Depth, Motion Vectors, and Normal. |

For more details on each AOV and the meaning behind pixel values in the output images, see the [AOV Recorder properties](aov-recorder-properties.md#input).

## Use case examples

You can record AOV render passes to finalize your look in post-processing or composite real-time 3D images with live action plates. For example:

* Render background plates in Unity and composite them with characters rendered offline, or with actors filmed against a green screen.

* Record Motion Vectors to apply motion blur in a separate compositing tool.

* Record Depth render pass to apply depth of field in a separate compositing tool.

* Record Normal and Depth render passes in order to composite them for further re-lighting.

* Record Alpha and Depth render passes to isolate multiple alpha mattes according to the actual distances of objects in the Scene.

* Record all Lighting AOVs to fine-tune them separately in a compositing software and get your final expected image rendering.

## Record an AOV Image Sequence

To set up the Recorder for AOV recording:

* [Get started with the Recorder](get-started.md)
* [Set AOV Recorder properties](aov-recorder-properties.md)
