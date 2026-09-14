using UnityEngine;
using UnityEditor;

namespace MystifyFX {

    [CustomEditor(typeof(MystifyEffectProfile))]
    public class MystifyEffectProfileEditor : Editor {

        SerializedProperty cullMode, wrapMode, textureSource, grabPass, renderOnTop;
        SerializedProperty alwaysOn;
        SerializedProperty maskTexture;
        SerializedProperty globalOpacity, alphaCutoff;
        SerializedProperty useStopMotion, stopMotionInterval;
        SerializedProperty scaleFactor, zoomFactor;
        SerializedProperty sourceOffset, sourceScale;
        SerializedProperty uvOffset, uvScale;
        SerializedProperty vertexDistortion, vertexDistortionSpeed;
        SerializedProperty blurIntensity, lutEffect, lutTexture, lutIntensity;
        SerializedProperty tintGradient, tintPos1, tintPos2, tintApplyAlphaToAll;
        SerializedProperty noiseSize, noiseAmount;
        SerializedProperty distortion, distortionAmplitude, distortionFrequency, distortionFalloff, distortionAnimationSpeed, distortionRimPower;
        SerializedProperty pixelate;
        SerializedProperty scanLinesIntensity, scanLinesScreenSpace, scanLinesSize, scanLinesAnimationSpeed, scanLinesColor, scanLinesMaxDistance, scanLinesRotation;
        SerializedProperty hazeIntensity, hazeThreshold, hazeMode, hazeGradient, hazeScale, hazeSpeed;
        SerializedProperty frostIntensity, frostSpread, frostColor;
        SerializedProperty crystalize, crystalizeSpread, crystalizeAnimationSpeed, crystalizeScale;
        SerializedProperty intersection, intersectionColor, intersectionNoise, intersectionThickness;
        SerializedProperty rimPower, rimColor, rimIntensity, rimTexture, rimTextureOpacity, rimTextureScale, chromaticAberrationAmount, chromaticAberrationEdgePower;
        SerializedProperty rain, glassTinyDrops, glassTinyDropsGridSize, glassLargeDrops, glassLargeDropsGridSize, glassLargeDropsSpeed, glassWaterDropIntensity, rainfall, rainWind, rainSpeed;
        SerializedProperty fakeLight, fakeLightIntensity, fakeLightFog, fakeLightGradient, fakeLightFalloff, fakeLightSpeed;
        SerializedProperty hitIntensity, hitColor, hitRadius, hitSpeed, hitNoiseScale, hitNoiseAmount;
        SerializedProperty hexaGrid, hexaGridScale, hexaGridColor, hexaGridRimPower, hexaGridNoiseStrength, hexaGridNoiseColor, hexaGridNoiseThreshold, hexaGridNoiseScale, hexaGridSweepSpeed, hexaGridSweepAmount, hexaGridSweepFrequency;
        SerializedProperty patternIntensity, patternScale, patternMaskScale, patternTexture, patternColor, patternMask, patternMaskThreshold;
        SerializedProperty brightness, contrast, vibrance;

        GUIStyle sectionGroupStyle;
        GUIStyle resetButtonStyle;

        void OnEnable () {
            try {
                cullMode = serializedObject.FindProperty("cullMode");
                wrapMode = serializedObject.FindProperty("wrapMode");
                textureSource = serializedObject.FindProperty("textureSource");
                grabPass = serializedObject.FindProperty("grabPass");
                renderOnTop = serializedObject.FindProperty("renderOnTop");
                alwaysOn = serializedObject.FindProperty("alwaysOn");
                maskTexture = serializedObject.FindProperty("maskTexture");
                globalOpacity = serializedObject.FindProperty("globalOpacity");
                alphaCutoff = serializedObject.FindProperty("alphaCutoff");
                scaleFactor = serializedObject.FindProperty("scaleFactor");
                zoomFactor = serializedObject.FindProperty("zoomFactor");
                sourceOffset = serializedObject.FindProperty("sourceOffset");
                sourceScale = serializedObject.FindProperty("sourceScale");
                uvOffset = serializedObject.FindProperty("uvOffset");
                uvScale = serializedObject.FindProperty("uvScale");
                vertexDistortion = serializedObject.FindProperty("vertexDistortion");
                vertexDistortionSpeed = serializedObject.FindProperty("vertexDistortionSpeed");
                useStopMotion = serializedObject.FindProperty("useStopMotion");
                stopMotionInterval = serializedObject.FindProperty("stopMotionInterval");
                blurIntensity = serializedObject.FindProperty("blurIntensity");
                brightness = serializedObject.FindProperty("brightness");
                contrast = serializedObject.FindProperty("contrast");
                vibrance = serializedObject.FindProperty("vibrance");
                lutEffect = serializedObject.FindProperty("lutEffect");
                lutTexture = serializedObject.FindProperty("lutTexture");
                lutIntensity = serializedObject.FindProperty("lutIntensity");
                tintGradient = serializedObject.FindProperty("tintGradient");
                tintPos1 = serializedObject.FindProperty("tintPos1");
                tintPos2 = serializedObject.FindProperty("tintPos2");
                tintApplyAlphaToAll = serializedObject.FindProperty("tintApplyAlphaToAll");
                noiseSize = serializedObject.FindProperty("noiseSize");
                noiseAmount = serializedObject.FindProperty("noiseAmount");
                distortion = serializedObject.FindProperty("distortion");
                distortionAmplitude = serializedObject.FindProperty("distortionAmplitude");
                distortionFrequency = serializedObject.FindProperty("distortionFrequency");
                distortionFalloff = serializedObject.FindProperty("distortionFalloff");
                distortionRimPower = serializedObject.FindProperty("distortionRimPower");
                distortionAnimationSpeed = serializedObject.FindProperty("distortionAnimationSpeed");
                pixelate = serializedObject.FindProperty("pixelate");
                scanLinesIntensity = serializedObject.FindProperty("scanLinesIntensity");
                scanLinesScreenSpace = serializedObject.FindProperty("scanLinesScreenSpace");
                scanLinesSize = serializedObject.FindProperty("scanLinesSize");
                scanLinesAnimationSpeed = serializedObject.FindProperty("scanLinesAnimationSpeed");
                scanLinesColor = serializedObject.FindProperty("scanLinesColor");
                scanLinesMaxDistance = serializedObject.FindProperty("scanLinesMaxDistance");
                scanLinesRotation = serializedObject.FindProperty("scanLinesRotation");
                hazeIntensity = serializedObject.FindProperty("hazeIntensity");
                hazeThreshold = serializedObject.FindProperty("hazeThreshold");
                hazeMode = serializedObject.FindProperty("hazeMode");
                hazeGradient = serializedObject.FindProperty("hazeGradient");
                hazeScale = serializedObject.FindProperty("hazeScale");
                hazeSpeed = serializedObject.FindProperty("hazeSpeed");
                frostIntensity = serializedObject.FindProperty("frostIntensity");
                frostSpread = serializedObject.FindProperty("frostSpread");
                frostColor = serializedObject.FindProperty("frostColor");
                crystalize = serializedObject.FindProperty("crystalize");
                crystalizeSpread = serializedObject.FindProperty("crystalizeSpread");
                crystalizeAnimationSpeed = serializedObject.FindProperty("crystalizeAnimationSpeed");
                crystalizeScale = serializedObject.FindProperty("crystalizeScale");
                intersection = serializedObject.FindProperty("intersection");
                intersectionColor = serializedObject.FindProperty("intersectionColor");
                intersectionNoise = serializedObject.FindProperty("intersectionNoise");
                intersectionThickness = serializedObject.FindProperty("intersectionThickness");
                rimPower = serializedObject.FindProperty("rimPower");
                rimColor = serializedObject.FindProperty("rimColor");
                rimIntensity = serializedObject.FindProperty("rimIntensity");
                rimTexture = serializedObject.FindProperty("rimTexture");
                rimTextureOpacity = serializedObject.FindProperty("rimTextureOpacity");
                rimTextureScale = serializedObject.FindProperty("rimTextureScale");
                chromaticAberrationAmount = serializedObject.FindProperty("chromaticAberrationAmount");
                chromaticAberrationEdgePower = serializedObject.FindProperty("chromaticAberrationEdgePower");
                rain = serializedObject.FindProperty("rain");
                glassTinyDrops = serializedObject.FindProperty("glassTinyDrops");
                glassTinyDropsGridSize = serializedObject.FindProperty("glassTinyDropsGridSize");
                glassLargeDrops = serializedObject.FindProperty("glassLargeDrops");
                glassLargeDropsGridSize = serializedObject.FindProperty("glassLargeDropsGridSize");
                glassLargeDropsSpeed = serializedObject.FindProperty("glassLargeDropsSpeed");
                glassWaterDropIntensity = serializedObject.FindProperty("glassWaterDropIntensity");
                rainfall = serializedObject.FindProperty("rainfall");
                rainWind = serializedObject.FindProperty("rainWind");
                rainSpeed = serializedObject.FindProperty("rainSpeed");
                fakeLight = serializedObject.FindProperty("fakeLight");
                fakeLightIntensity = serializedObject.FindProperty("fakeLightIntensity");
                fakeLightGradient = serializedObject.FindProperty("fakeLightGradient");
                fakeLightFalloff = serializedObject.FindProperty("fakeLightFalloff");
                fakeLightFog = serializedObject.FindProperty("fakeLightFog");
                fakeLightSpeed = serializedObject.FindProperty("fakeLightSpeed");
                hitIntensity = serializedObject.FindProperty("hitIntensity");
                hitColor = serializedObject.FindProperty("hitColor");
                hitRadius = serializedObject.FindProperty("hitRadius");
                hitSpeed = serializedObject.FindProperty("hitSpeed");
                hitNoiseScale = serializedObject.FindProperty("hitNoiseScale");
                hitNoiseAmount = serializedObject.FindProperty("hitNoiseAmount");
                hexaGrid = serializedObject.FindProperty("hexaGrid");
                hexaGridScale = serializedObject.FindProperty("hexaGridScale");
                hexaGridColor = serializedObject.FindProperty("hexaGridColor");
                hexaGridRimPower = serializedObject.FindProperty("hexaGridRimPower");
                hexaGridNoiseStrength = serializedObject.FindProperty("hexaGridNoiseStrength");
                hexaGridNoiseColor = serializedObject.FindProperty("hexaGridNoiseColor");
                hexaGridNoiseThreshold = serializedObject.FindProperty("hexaGridNoiseThreshold");
                hexaGridNoiseScale = serializedObject.FindProperty("hexaGridNoiseScale");
                hexaGridSweepSpeed = serializedObject.FindProperty("hexaGridSweepSpeed");
                hexaGridSweepAmount = serializedObject.FindProperty("hexaGridSweepAmount");
                hexaGridSweepFrequency = serializedObject.FindProperty("hexaGridSweepFrequency");
                patternIntensity = serializedObject.FindProperty("patternIntensity");
                patternScale = serializedObject.FindProperty("patternScale");
                patternMaskScale = serializedObject.FindProperty("patternMaskScale");
                patternTexture = serializedObject.FindProperty("patternTexture");
                patternColor = serializedObject.FindProperty("patternColor");
                patternMask = serializedObject.FindProperty("patternMask");
                patternMaskThreshold = serializedObject.FindProperty("patternMaskThreshold");
            }
            catch {
            }
        }

        public override void OnInspectorGUI () {

            Color titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            GUIStyle skurikenModuleTitleStyle = "ShurikenModuleTitle";
            sectionGroupStyle = new GUIStyle(skurikenModuleTitleStyle);
            sectionGroupStyle.normal.textColor = titleColor;
            sectionGroupStyle.contentOffset = new Vector2(5f, -2f);
            sectionGroupStyle.fixedHeight = 22;
            sectionGroupStyle.fontStyle = FontStyle.Bold;
            sectionGroupStyle.padding.right = 50;

            if (resetButtonStyle == null) {
                resetButtonStyle = new GUIStyle(EditorStyles.miniButton);
                resetButtonStyle.fixedWidth = 45;
                resetButtonStyle.fixedHeight = 16;
                resetButtonStyle.fontSize = 9;
                resetButtonStyle.normal.textColor = titleColor;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Rendering Options", sectionGroupStyle);
            EditorGUILayout.PropertyField(cullMode);
            EditorGUILayout.PropertyField(renderOnTop);
            EditorGUILayout.PropertyField(grabPass);
            EditorGUILayout.PropertyField(alwaysOn);
            EditorGUILayout.PropertyField(textureSource);
            EditorGUILayout.PropertyField(maskTexture, new GUIContent("Mask Texture"));
            EditorGUILayout.PropertyField(alphaCutoff, new GUIContent("Alpha Cutoff"));
            EditorGUILayout.PropertyField(globalOpacity, new GUIContent("Global Opacity"));
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            Rect headerRect = GUILayoutUtility.GetRect(GUIContent.none, sectionGroupStyle, GUILayout.Height(22));
            if (Event.current.type == EventType.Repaint) {
                sectionGroupStyle.Draw(headerRect, "Object", false, false, false, false);
            }
            Rect buttonRect = headerRect;
            buttonRect.xMin = buttonRect.xMax - 50;
            buttonRect.y += 1;
            if (GUI.Button(buttonRect, "RESET", resetButtonStyle)) {
                scaleFactor.floatValue = 1f;
                uvOffset.vector2Value = Vector2.zero;
                uvScale.vector2Value = Vector2.one;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(scaleFactor, new GUIContent("Scale"));
            EditorGUILayout.PropertyField(uvOffset, new GUIContent("UV Offset"));
            EditorGUILayout.PropertyField(uvScale, new GUIContent("UV Scale"));
            EditorGUILayout.PropertyField(useStopMotion, new GUIContent("Use Stop Motion"));
            if (useStopMotion.boolValue) {
                EditorGUILayout.PropertyField(stopMotionInterval, new GUIContent("Time Interval"));
            }

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            headerRect = GUILayoutUtility.GetRect(GUIContent.none, sectionGroupStyle, GUILayout.Height(22));
            if (Event.current.type == EventType.Repaint) {
                sectionGroupStyle.Draw(headerRect, "Background", false, false, false, false);
            }
            buttonRect = headerRect;
            buttonRect.xMin = buttonRect.xMax - 50;
            buttonRect.y += 1;
            if (GUI.Button(buttonRect, "RESET", resetButtonStyle)) {
                zoomFactor.floatValue = 0f;
                sourceOffset.vector2Value = Vector2.zero;
                sourceScale.vector2Value = Vector2.one;
                wrapMode.enumValueIndex = 0;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(zoomFactor, new GUIContent("Zoom"));
            EditorGUILayout.PropertyField(sourceOffset, new GUIContent("Source Offset"));
            EditorGUILayout.PropertyField(sourceScale, new GUIContent("Source Scale"));
            EditorGUILayout.PropertyField(wrapMode);

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            headerRect = GUILayoutUtility.GetRect(GUIContent.none, sectionGroupStyle, GUILayout.Height(22));
            if (Event.current.type == EventType.Repaint) {
                sectionGroupStyle.Draw(headerRect, "2D Effects", false, false, false, false);
            }
            buttonRect = headerRect;
            buttonRect.xMin = buttonRect.xMax - 50;
            buttonRect.y += 1;
            if (GUI.Button(buttonRect, "RESET", resetButtonStyle)) {
                blurIntensity.floatValue = 0f;
                lutEffect.enumValueIndex = 0;
                brightness.floatValue = 1f;
                contrast.floatValue = 1f;
                vibrance.floatValue = 0f;
                noiseAmount.floatValue = 0f;
                distortion.boolValue = false;
                distortionFrequency.floatValue = 3f;
                distortionAnimationSpeed.floatValue = 1f;
                distortionFalloff.floatValue = 5f;
                distortionRimPower.floatValue = 0f;
                pixelate.intValue = 0;
                rain.boolValue = false;
                scanLinesIntensity.floatValue = 0f;
                scanLinesSize.intValue = 4;
                crystalize.floatValue = 0f;
                frostIntensity.floatValue = 0f;
                hazeIntensity.floatValue = 0f;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(blurIntensity, new GUIContent("Blur Intensity"));
            if (blurIntensity.floatValue > 0) {
                EditorGUILayout.HelpBox("Master blur amount can be controlled in the render feature.", MessageType.Info);
                if (GUILayout.Button("Go to URP asset", EditorStyles.miniButton)) {
                    var pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                    if (pipe != null) {
                        Selection.activeObject = pipe;
                    }
                }
                EditorGUILayout.Separator();
            }

            EditorGUILayout.PropertyField(lutEffect, new GUIContent("Color Effect"));
            if ((ColorEffect)lutEffect.enumValueIndex == ColorEffect.LUT) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lutTexture, new GUIContent("LUT Texture"));
                EditorGUILayout.PropertyField(lutIntensity, new GUIContent("Intensity"));
                EditorGUI.indentLevel--;
            }
            else if ((ColorEffect)lutEffect.enumValueIndex == ColorEffect.DirectionalTint) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(tintGradient, new GUIContent("Gradient"));
                EditorGUILayout.PropertyField(tintPos1, new GUIContent("Start Pos"));
                EditorGUILayout.PropertyField(tintPos2, new GUIContent("End Pos"));
                EditorGUILayout.PropertyField(tintApplyAlphaToAll, new GUIContent("Apply Alpha To Effects"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(brightness);
            EditorGUILayout.PropertyField(contrast);
            EditorGUILayout.PropertyField(vibrance);

            EditorGUILayout.PropertyField(noiseAmount, new GUIContent("Noise Amount"));
            if (noiseAmount.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(noiseSize, new GUIContent("Noise Size"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(distortion, new GUIContent("Background Distortion"));
            if (distortion.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(distortionAmplitude, new GUIContent("Amplitude"));
                EditorGUILayout.PropertyField(distortionFrequency, new GUIContent("Frequency"));
                EditorGUILayout.PropertyField(distortionAnimationSpeed, new GUIContent("Animation Speed"));
                EditorGUILayout.PropertyField(distortionFalloff, new GUIContent("Falloff"));
                EditorGUILayout.PropertyField(distortionRimPower, new GUIContent("Rim Power"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(pixelate);

            EditorGUILayout.PropertyField(rain, new GUIContent("Rain"));
            if (rain.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(glassTinyDrops, new GUIContent("Glass Tiny Drops"));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(glassTinyDropsGridSize, new GUIContent("Grid Size"));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(glassLargeDrops, new GUIContent("Glass Large Drops"));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(glassLargeDropsGridSize, new GUIContent("Grid Size"));
                EditorGUILayout.PropertyField(glassLargeDropsSpeed, new GUIContent("Speed"));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(glassWaterDropIntensity, new GUIContent("Water Drop Intensity"));
                EditorGUILayout.PropertyField(rainfall, new GUIContent("Rainfall"));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(rainSpeed, new GUIContent("Speed"));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(rainWind, new GUIContent("Wind Direction"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(scanLinesIntensity, new GUIContent("Scan Lines"));
            if (scanLinesIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(scanLinesScreenSpace, new GUIContent("Screen Space"));
                EditorGUILayout.PropertyField(scanLinesSize, new GUIContent("Size"));
                EditorGUILayout.PropertyField(scanLinesRotation, new GUIContent("Rotation"));
                EditorGUILayout.PropertyField(scanLinesAnimationSpeed, new GUIContent("Animation Speed"));
                EditorGUILayout.PropertyField(scanLinesColor, new GUIContent("Color"));
                EditorGUILayout.PropertyField(scanLinesMaxDistance, new GUIContent("Max Distance"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(crystalize, new GUIContent("Crystalize"));
            if (crystalize.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(crystalizeScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(crystalizeSpread, new GUIContent("Spread"));
                EditorGUILayout.PropertyField(crystalizeAnimationSpeed, new GUIContent("Animation Speed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(frostIntensity);
            if (frostIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(frostSpread, new GUIContent("Spread"));
                EditorGUILayout.PropertyField(frostColor, new GUIContent("Color"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(hazeIntensity);
            if (hazeIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hazeThreshold, new GUIContent("Threshold"));
                EditorGUILayout.PropertyField(hazeMode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(hazeGradient, new GUIContent("Colors"));
                EditorGUILayout.PropertyField(hazeScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(hazeSpeed, new GUIContent("Animation Speed"));
                EditorGUI.indentLevel--;
            }
/* // under development
            EditorGUILayout.PropertyField(patternIntensity, new GUIContent("Pattern"));
            if (patternIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(patternTexture, new GUIContent("Texture"));
                EditorGUILayout.PropertyField(patternScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(patternColor, new GUIContent("Tint Color"));
                EditorGUILayout.PropertyField(patternMask, new GUIContent("Mask"));
                if (patternMask.objectReferenceValue != null) {
                    EditorGUILayout.PropertyField(patternMaskScale, new GUIContent("Mask Scale"));
                    EditorGUILayout.PropertyField(patternMaskThreshold, new GUIContent("Mask Threshold"));
                }
                EditorGUI.indentLevel--;
            }
*/

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            headerRect = GUILayoutUtility.GetRect(GUIContent.none, sectionGroupStyle, GUILayout.Height(22));
            if (Event.current.type == EventType.Repaint) {
                sectionGroupStyle.Draw(headerRect, "3D Effects", false, false, false, false);
            }
            buttonRect = headerRect;
            buttonRect.xMin = buttonRect.xMax - 50;
            buttonRect.y += 1;
            if (GUI.Button(buttonRect, "RESET", resetButtonStyle)) {
                vertexDistortion.floatValue = 0f;
                intersection.boolValue = false;
                rimIntensity.floatValue = 0f;
                chromaticAberrationAmount.floatValue = 0f;
                chromaticAberrationEdgePower.floatValue = 0f;
                fakeLight.boolValue = false;
                hexaGrid.boolValue = false;
                hexaGridScale.floatValue = 1f;
                hitIntensity.floatValue = 0f;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(vertexDistortion, new GUIContent("Vertex Distortion"));
            if (vertexDistortion.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(vertexDistortionSpeed, new GUIContent("Animation Speed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(intersection, new GUIContent("Intersection"));
            if (intersection.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(intersectionThickness, new GUIContent("Thickness"));
                EditorGUILayout.PropertyField(intersectionColor, new GUIContent("Color"));
                EditorGUILayout.PropertyField(intersectionNoise, new GUIContent("Noise"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(rimIntensity, new GUIContent("Rim"));
            if (rimIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(rimPower, new GUIContent("Power"));
                EditorGUILayout.PropertyField(rimColor, new GUIContent("Color"));
                EditorGUILayout.PropertyField(rimTexture, new GUIContent("Texture (R)"));
                if (rimTexture.objectReferenceValue != null) {
                    EditorGUILayout.PropertyField(rimTextureOpacity, new GUIContent("Texture Opacity"));
                    EditorGUILayout.PropertyField(rimTextureScale, new GUIContent("Texture Scale"));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(chromaticAberrationAmount, new GUIContent("Chromatic Aberration"));
            if (chromaticAberrationAmount.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(chromaticAberrationEdgePower, new GUIContent("Edge Power", "0 = entire object, >0 = edges only (like rim)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(fakeLight, new GUIContent("Spectral Light"));
            if (fakeLight.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fakeLightIntensity, new GUIContent("Light Intensity"));
                EditorGUILayout.PropertyField(fakeLightFog, new GUIContent("Fog Intensity"));
                EditorGUILayout.PropertyField(fakeLightGradient, new GUIContent("Gradient"));
                EditorGUILayout.PropertyField(fakeLightFalloff, new GUIContent("Falloff"));
                EditorGUILayout.PropertyField(fakeLightSpeed, new GUIContent("Speed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(hexaGrid, new GUIContent("Hexa Grid"));
            if (hexaGrid.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hexaGridColor, new GUIContent("Color"));
                EditorGUILayout.PropertyField(hexaGridScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(hexaGridRimPower, new GUIContent("Rim Power"));
                EditorGUILayout.PropertyField(hexaGridNoiseStrength, new GUIContent("Noise Strength"));
                if (hexaGridNoiseStrength.floatValue > 0) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hexaGridNoiseColor, new GUIContent("Color"));
                    EditorGUILayout.PropertyField(hexaGridNoiseThreshold, new GUIContent("Threshold"));
                    EditorGUILayout.PropertyField(hexaGridNoiseScale, new GUIContent("Scale"));
                EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(hexaGridSweepAmount, new GUIContent("Sweep Amount"));
                if (hexaGridSweepAmount.floatValue > 0) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hexaGridSweepSpeed, new GUIContent("Speed"));
                    EditorGUILayout.PropertyField(hexaGridSweepFrequency, new GUIContent("Frequency"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(hitIntensity, new GUIContent("Hit Effect (Defaults)", "These are default values. Use the HitFX() method to perform a hit effect with custom values."));
            if (hitIntensity.floatValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("These are default values. Use the HitFX() method to perform a hit effect with custom values.", MessageType.Info);
                EditorGUILayout.PropertyField(hitColor, new GUIContent("Color"));
                EditorGUILayout.PropertyField(hitRadius, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(hitSpeed, new GUIContent("Speed"));
                EditorGUILayout.PropertyField(hitNoiseAmount, new GUIContent("Noise Amount"));
                if (hitNoiseAmount.floatValue > 0) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hitNoiseScale, new GUIContent("Noise Scale"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}           