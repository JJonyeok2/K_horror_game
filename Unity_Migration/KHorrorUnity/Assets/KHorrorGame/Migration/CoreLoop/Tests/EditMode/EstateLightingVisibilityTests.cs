using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateLightingVisibilityTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void DevelopmentLightingProfileKeepsSceneDarkButNavigable()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var profile = FindLightingProfile();
            Assert.IsTrue(ReadBool(profile, "fogEnabled"), "Fog should stay enabled for horror atmosphere.");
            Assert.That(ReadFloat(profile, "fogDensity"), Is.InRange(0.012f, 0.018f), "Fog should obscure distance without making the route unreadable.");
            Assert.GreaterOrEqual(ReadFloat(profile, "moonIntensity"), 0.55f, "Moonlight needs enough fill for non-flashlight silhouettes.");
            Assert.GreaterOrEqual(ReadFloat(profile, "flashlightIntensity"), 15.5f, "Flashlight should be strong enough for close prop inspection.");
            Assert.GreaterOrEqual(ReadFloat(profile, "flashlightRange"), 38f, "Flashlight should reach across forest and hanok rooms.");
            Assert.GreaterOrEqual(ReadFloat(profile, "flashlightSpotAngle"), 58f, "Flashlight cone should not feel like a pinhole.");
            Assert.GreaterOrEqual(ReadFloat(profile, "exposureCompensation"), 0.6f, "Post exposure should not crush dark surfaces to black.");
            Assert.LessOrEqual(ReadFloat(profile, "vignetteIntensity"), 0.14f, "Vignette should add tension without hiding the route.");
        }

        [Test]
        public void PlayableZonesHaveReadableLightAnchors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            AssertLight("ApproachPathLowFill", 2.4f, 44f);
            AssertLight("GateWetLamp_Left", 2.6f, 12f);
            AssertLight("CourtyardMoonBounce", 1.8f, 38f);
            AssertLight("MainHouseInteriorReadabilityFill", 1.2f, 10f);
            AssertLight("RearRouteLanternPool_Second", 1.35f, 7.5f);
            AssertLight("DeepShrineLanternGlow", 1.6f, 9.5f);
        }

        [Test]
        public void PostProcessDoesNotCrushPlayableVisibility()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var volumeObject = GameObject.Find("GlobalPostProcessVolume");
            Assert.IsNotNull(volumeObject, "Global post process volume should exist.");
            var volume = volumeObject.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component => component != null && component.GetType().Name == "Volume");
            Assert.IsNotNull(volume, "Global post process volume should exist.");
            var sharedProfile = ReadObjectProperty(volume, "sharedProfile");
            Assert.IsNotNull(sharedProfile, "Global post process volume should reference a profile.");

            var color = FindVolumeComponent(sharedProfile, "ColorAdjustments");
            Assert.GreaterOrEqual(ReadVolumeFloat(color, "postExposure"), 0.65f, "Post exposure should keep PBR surfaces readable.");
            Assert.LessOrEqual(ReadVolumeFloat(color, "contrast"), 4.5f, "Contrast should not crush shadows.");
            Assert.That(ReadVolumeFloat(color, "saturation"), Is.InRange(-12f, 0f), "Saturation should stay muted but not monochrome.");

            var vignette = FindVolumeComponent(sharedProfile, "Vignette");
            Assert.LessOrEqual(ReadVolumeFloat(vignette, "intensity"), 0.14f, "Vignette should not hide route edges.");

            var filmGrain = FindVolumeComponent(sharedProfile, "FilmGrain");
            Assert.LessOrEqual(ReadVolumeFloat(filmGrain, "intensity"), 0.1f, "Film grain should stay subtle for playtesting visibility.");
        }

        private static UnityEngine.Object FindLightingProfile()
        {
            var rigObject = GameObject.Find("LightingRig");
            Assert.IsNotNull(rigObject, "LightingRig should exist.");

            var rig = rigObject.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component => component != null && component.GetType().Name == "KHorrorLightingRig");
            Assert.IsNotNull(rig, "LightingRig should have KHorrorLightingRig.");

            var serialized = new SerializedObject(rig);
            var property = serialized.FindProperty("profile");
            Assert.IsNotNull(property, "LightingRig should serialize a profile reference.");
            var profile = property.objectReferenceValue;
            Assert.IsNotNull(profile, "LightingRig should reference KHorrorLightingProfile.");
            return profile;
        }

        private static void AssertLight(string name, float minimumIntensity, float minimumRange)
        {
            var target = GameObject.Find(name);
            Assert.IsNotNull(target, name + " should exist.");

            var light = target.GetComponent<Light>();
            Assert.IsNotNull(light, name + " should have a Light component.");
            Assert.GreaterOrEqual(light.intensity, minimumIntensity, name + " is too dim.");
            Assert.GreaterOrEqual(light.range, minimumRange, name + " does not cover its route segment.");
            Assert.IsTrue(light.enabled, name + " should be enabled.");
        }

        private static bool ReadBool(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.IsNotNull(property, propertyName + " should exist.");
            return property.boolValue;
        }

        private static float ReadFloat(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.IsNotNull(property, propertyName + " should exist.");
            return property.floatValue;
        }

        private static object ReadObjectProperty(object target, string propertyName)
        {
            if (target is UnityEngine.Object unityObject)
            {
                var serializedProperty = new SerializedObject(unityObject).FindProperty(propertyName);
                if (serializedProperty != null)
                {
                    return serializedProperty.objectReferenceValue;
                }
            }

            var property = target.GetType().GetProperty(propertyName);
            Assert.IsNotNull(property, target.GetType().Name + "." + propertyName + " should exist.");
            return property.GetValue(target);
        }

        private static object FindVolumeComponent(object profile, string typeName)
        {
            var componentsProperty = profile.GetType().GetProperty("components");
            var componentsField = profile.GetType().GetField("components");
            var components = componentsProperty != null ? componentsProperty.GetValue(profile) : componentsField?.GetValue(profile);
            Assert.IsNotNull(components, "Volume profile should expose components.");

            var component = ((System.Collections.IEnumerable)components)
                .Cast<object>()
                .FirstOrDefault(item => item != null && item.GetType().Name == typeName);
            Assert.IsNotNull(component, typeName + " should be configured.");
            return component;
        }

        private static float ReadVolumeFloat(object component, string fieldName)
        {
            var field = component.GetType().GetField(fieldName);
            Assert.IsNotNull(field, component.GetType().Name + "." + fieldName + " should exist.");
            var parameter = field.GetValue(component);
            Assert.IsNotNull(parameter, component.GetType().Name + "." + fieldName + " should have a parameter value.");
            var valueProperty = parameter.GetType().GetProperty("value");
            Assert.IsNotNull(valueProperty, component.GetType().Name + "." + fieldName + " should expose a value.");
            return (float)valueProperty.GetValue(parameter);
        }
    }
}
