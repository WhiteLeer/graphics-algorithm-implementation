#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SceneViewVisibilityFix
{
    [MenuItem("Tools/All Effects/Fix Scene View Visibility")]
    public static void FixSceneViewVisibility()
    {
        SceneVisibilityManager.instance.ShowAll();
        SceneVisibilityManager.instance.EnableAllPicking();

        GameObject target = GameObject.Find("Player_Girl_Test");
        if (target == null)
            target = GameObject.Find("Player_Girl");

        if (target != null)
        {
            SceneVisibilityManager.instance.Show(target, true);
            SceneVisibilityManager.instance.EnablePicking(target, true);

            SkinnedMeshRenderer[] skinnedMeshes = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer smr in skinnedMeshes)
            {
                if (smr == null)
                    continue;

                SceneVisibilityManager.instance.Show(smr.gameObject, true);
                SceneVisibilityManager.instance.EnablePicking(smr.gameObject, true);
                smr.forceRenderingOff = false;
                smr.enabled = true;
            }

            Selection.activeGameObject = target;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("[AllEffects] Scene view visibility restored.");
    }
}
#endif
