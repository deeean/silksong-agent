using System;
using HarmonyLib;
using UnityEngine;

namespace SilksongAgent.Patches;

[HarmonyPatch(typeof(AudioSource))]
public class AudioSourcePatch
{
    [HarmonyPatch("Play", new Type[] { })]
    [HarmonyPrefix]
    static bool PrefixPlay() => false;

    [HarmonyPatch("Play", new Type[] { typeof(ulong) })]
    [HarmonyPrefix]
    static bool PrefixPlayDelayed() => false;

    [HarmonyPatch("PlayOneShot", new Type[] { typeof(AudioClip) })]
    [HarmonyPrefix]
    static bool PrefixPlayOneShotClip() => false;

    [HarmonyPatch("PlayOneShot", new Type[] { typeof(AudioClip), typeof(float) })]
    [HarmonyPrefix]
    static bool PrefixPlayOneShotClipVolume() => false;

    [HarmonyPatch("PlayClipAtPoint", new Type[] { typeof(AudioClip), typeof(Vector3) })]
    [HarmonyPrefix]
    static bool PrefixPlayClipAtPointPosition() => false;

    [HarmonyPatch("PlayClipAtPoint", new Type[] { typeof(AudioClip), typeof(Vector3), typeof(float) })]
    [HarmonyPrefix]
    static bool PrefixPlayClipAtPointPositionVolume() => false;
}
