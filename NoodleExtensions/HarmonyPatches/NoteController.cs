﻿using BS_Utils.Utilities;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Harmony;
using System.Linq;
using UnityEngine;
using static NoodleExtensions.Plugin;

namespace NoodleExtensions.HarmonyPatches
{
    [HarmonyPriority(Priority.Normal)]
    [HarmonyPatch(typeof(NoteController))]
    [HarmonyPatch("Init")]
    internal class NoteControllerInit
    {
        public static void Prefix(ref NoteController __instance, NoteData noteData, ref Vector3 moveStartPos, ref Vector3 moveEndPos, ref Vector3 jumpEndPos, ref float jumpGravity)
        {
            // CustomJSONData
            if (NoodleExtensionsActive && !MappingExtensionsActive && noteData is CustomNoteData customData)
            {
                dynamic dynData = customData.customData;
                float? _startRow = (float?)Trees.at(dynData, "_startRow");
                float? _startHeight = (float?)Trees.at(dynData, "_startHeight");
                // TODO: Precision Rotation
                float? _rot = (float?)Trees.at(dynData, "_rotation");

                float _globalJumpOffsetY = beatmapObjectSpawnController.GetField<float>("_globalJumpOffsetY");
                float _moveDistance = beatmapObjectSpawnController.GetField<float>("_moveDistance");
                float _jumpDistance = beatmapObjectSpawnController.GetField<float>("_jumpDistance");
                float _noteJumpMovementSpeed = beatmapObjectSpawnController.GetField<float>("_noteJumpMovementSpeed");

                Vector3 forward2 = beatmapObjectSpawnController.transform.forward;
                Vector3 a4 = beatmapObjectSpawnController.transform.position;
                a4 += forward2 * (_moveDistance + _jumpDistance * 0.5f);
                Vector3 a5 = a4 - forward2 * _moveDistance;
                Vector3 a6 = a4 - forward2 * (_moveDistance + _jumpDistance);

                Vector3 noteOffset2 = GetNoteOffset(noteData, _startRow, _startHeight);

                if (noteData.noteType == NoteType.Bomb)
                {
                    __instance.transform.SetPositionAndRotation(a4 + noteOffset2, Quaternion.identity);
                    moveStartPos = a4 + noteOffset2;
                    moveEndPos = a5 + noteOffset2;
                    jumpEndPos = a6 + noteOffset2;
                }
                else if (noteData.noteType.IsBasicNote())
                {
                    float? flipLineIndex = (float?)Trees.at(dynData, "flipLineIndex");
                    Vector3 noteOffset3 = GetNoteOffset(noteData, flipLineIndex ?? _startRow, _startHeight);
                    __instance.transform.SetPositionAndRotation(a4 + noteOffset3, Quaternion.identity);
                    moveStartPos = a4 + noteOffset3;
                    moveEndPos = a5 + noteOffset3;
                    jumpEndPos = a6 + noteOffset2;
                }

                float lineYPos = LineYPosForLineLayer(noteData, _startHeight);
                // Magic numbers below found with linear regression y=mx+b using existing HighestJumpPosYForLineLayer values
                float highestJump = _startHeight.HasValue ? ((0.875f * lineYPos) + 0.639583f) + _globalJumpOffsetY :
                    beatmapObjectSpawnController.HighestJumpPosYForLineLayer(noteData.noteLineLayer);
                jumpGravity = 2f * (highestJump - lineYPos) /
                    Mathf.Pow(_jumpDistance / _noteJumpMovementSpeed * 0.5f, 2f);
            }
        }
    }
}