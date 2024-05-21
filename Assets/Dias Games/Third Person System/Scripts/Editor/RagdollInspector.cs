using System;
using UnityEngine;
using UnityEditor;

namespace DiasGames.Components.Inspector
{
    [CustomEditor(typeof(Ragdoll))]
    public class RagdollInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Create Ragdoll"))
                CreateRagdoll();
        }

        private void CreateRagdoll()
        {
            var ragdollType = Type.GetType("UnityEditor.RagdollBuilder, UnityEditor");
            var windowsOpened = Resources.FindObjectsOfTypeAll(ragdollType);

            // Open Ragdoll window 
            if (windowsOpened == null || windowsOpened.Length == 0)
            {
                EditorApplication.ExecuteMenuItem("GameObject/3D Object/Ragdoll...");
                windowsOpened = Resources.FindObjectsOfTypeAll(ragdollType);
            }

            if (windowsOpened != null && windowsOpened.Length > 0)
            {
                ScriptableWizard ragdollWindow = windowsOpened[0] as ScriptableWizard;

                SetRagdollBoneValue(ragdollWindow, "pelvis", HumanBodyBones.Hips);
                SetRagdollBoneValue(ragdollWindow, "leftHips", HumanBodyBones.LeftUpperLeg);
                SetRagdollBoneValue(ragdollWindow, "leftKnee", HumanBodyBones.LeftLowerLeg);
                SetRagdollBoneValue(ragdollWindow, "leftFoot", HumanBodyBones.LeftFoot);
                SetRagdollBoneValue(ragdollWindow, "rightHips", HumanBodyBones.RightUpperLeg);
                SetRagdollBoneValue(ragdollWindow, "rightKnee", HumanBodyBones.RightLowerLeg);
                SetRagdollBoneValue(ragdollWindow, "rightFoot", HumanBodyBones.RightFoot);

                SetRagdollBoneValue(ragdollWindow, "leftArm", HumanBodyBones.LeftUpperArm);
                SetRagdollBoneValue(ragdollWindow, "leftElbow", HumanBodyBones.LeftLowerArm);
                SetRagdollBoneValue(ragdollWindow, "rightArm", HumanBodyBones.RightUpperArm);
                SetRagdollBoneValue(ragdollWindow, "rightElbow", HumanBodyBones.RightLowerArm);

                SetRagdollBoneValue(ragdollWindow, "middleSpine", HumanBodyBones.Spine);
                SetRagdollBoneValue(ragdollWindow, "head", HumanBodyBones.Head);
            }
        }

        private void SetRagdollBoneValue(ScriptableWizard window, string fieldName, HumanBodyBones bone)
        {
            Animator animator = (serializedObject.targetObject as MonoBehaviour).GetComponent<Animator>();

            if (animator == null) return;

            var field = window.GetType().GetField(fieldName);
            field.SetValue(window, animator.GetBoneTransform(bone));
            animator.GetBoneTransform(bone).gameObject.layer = 13;
        }
    }
}