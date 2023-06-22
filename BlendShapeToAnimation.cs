using UnityEngine;
using UnityEditor;

public class BlendShapeToAnimation : EditorWindow
{
    private SkinnedMeshRenderer targetRenderer;
    private string animationFileName = "NewAnimation";
    private int ignoreCountTop = 0;
    private int ignoreCountBottom = 0;
    private bool ignoreZeroValues = false;
    private string outputFolderPath = "Assets/Animations";

    [MenuItem("Window/BlendShape Animation Generator")]
    private static void OpenWindow()
    {
        var window = GetWindow<BlendShapeToAnimation>();
        window.titleContent = new GUIContent("BlendShape Animation");
        window.Show();
    }

    private void OnGUI()
    {
        targetRenderer = EditorGUILayout.ObjectField("Target Renderer", targetRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
        animationFileName = EditorGUILayout.TextField("Animation File Name", animationFileName);

        ignoreCountTop = EditorGUILayout.IntField("Ignore Count (Top)", ignoreCountTop);
        ignoreCountBottom = EditorGUILayout.IntField("Ignore Count (Bottom)", ignoreCountBottom);
        ignoreZeroValues = EditorGUILayout.Toggle("Ignore Zero Values", ignoreZeroValues);
        outputFolderPath = EditorGUILayout.TextField("Save Folder Path", outputFolderPath);
        if (GUILayout.Button("Generate Animation"))
        {
            GenerateAnimation();
        }
    }

    private void GenerateAnimation()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("ëŒè€ÇÃSkinMesRendererÇéwíËÇµÇƒÇÀ");
            return;
        }

        var blendShapeCount = targetRenderer.sharedMesh.blendShapeCount;

        var animationClip = new AnimationClip();
        animationClip.name = animationFileName;

        var curveBindings = new EditorCurveBinding[blendShapeCount];

        for (int i = 0; i < blendShapeCount; i++)
        {
            if (i < ignoreCountTop || i >= blendShapeCount - ignoreCountBottom)
            {
                continue;
            }

            var blendShapeName = targetRenderer.sharedMesh.GetBlendShapeName(i);
            var blendShapeValue = targetRenderer.GetBlendShapeWeight(i);

            if (ignoreZeroValues && Mathf.Approximately(blendShapeValue, 0f))
            {
                continue;
            }

            var curveBinding = new EditorCurveBinding
            {
                path = targetRenderer.gameObject.name,
                propertyName = "blendShapes." + blendShapeName,
                type = typeof(SkinnedMeshRenderer)
            };

            var keyframe = new Keyframe(0f, blendShapeValue);

            AnimationUtility.SetEditorCurve(animationClip, curveBinding, new AnimationCurve(keyframe));

            curveBindings[i] = curveBinding;
        }

        
        var existingBindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);
        foreach (var existingBinding in existingBindings)
        {
            if (existingBinding.type == typeof(SkinnedMeshRenderer) && existingBinding.propertyName.StartsWith("blendShapes."))
            {
                bool curveExists = false;
                foreach (var curveBinding in curveBindings)
                {
                    if (curveBinding.Equals(existingBinding))
                    {
                        curveExists = true;
                        break;
                    }
                }

                if (!curveExists)
                {
                    AnimationUtility.SetEditorCurve(animationClip, existingBinding, null);
                }
            }
        }

        if (!string.IsNullOrEmpty(outputFolderPath))
        {
            var fileName = animationFileName + ".anim";
            var filePath = System.IO.Path.Combine(outputFolderPath, fileName);
            AssetDatabase.CreateAsset(animationClip, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}