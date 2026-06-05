using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MagicAnimatorSetup : EditorWindow
{
    private GameObject targetGameObject;
    private GameObject fbxAsset;
    private bool isSinglePiece = false;
    private List<string> detectedActions = new List<string>();
    private Dictionary<string, bool> actionSelection = new Dictionary<string, bool>();

    [MenuItem("Tools/Magic Animator Setup")]
    public static void ShowWindow()
    {
        GetWindow<MagicAnimatorSetup>("Magic Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Generic Multi-Piece Animator Setup", EditorStyles.boldLabel);
        
        targetGameObject = (GameObject)EditorGUILayout.ObjectField("Scene GameObject", targetGameObject, typeof(GameObject), true);
        fbxAsset = (GameObject)EditorGUILayout.ObjectField("FBX Asset", fbxAsset, typeof(GameObject), false);
        
        GUILayout.Space(5);
        isSinglePiece = EditorGUILayout.ToggleLeft(" Is Single-Piece Model (No children)", isSinglePiece);
        GUILayout.Space(5);

        if (GUILayout.Button("1. Scan FBX for Actions", GUILayout.Height(30)))
        {
            ScanActions();
        }

        if (detectedActions.Count > 0)
        {
            GUILayout.Label("Select Actions to Merge:", EditorStyles.boldLabel);
            foreach (var action in detectedActions)
            {
                if (!actionSelection.ContainsKey(action)) 
                {
                    actionSelection[action] = true;
                }
                actionSelection[action] = EditorGUILayout.ToggleLeft(action, actionSelection[action]);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("2. Merge & Build Animator!", GUILayout.Height(40)))
            {
                BuildAnimator();
            }
        }
    }

    void ScanActions()
    {
        if (fbxAsset == null) { Debug.LogError("Please assign the FBX Asset first!"); return; }
        
        string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        HashSet<string> actions = new HashSet<string>();
        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                int pipeIndex = clip.name.IndexOf('|');
                if (pipeIndex >= 0 && pipeIndex < clip.name.Length - 1)
                {
                    string actionName = clip.name.Substring(pipeIndex + 1);
                    actions.Add(actionName);
                }
            }
        }
        
        detectedActions = actions.ToList();
        actionSelection.Clear();
        foreach (var action in detectedActions)
        {
            // Auto-check it unless it looks like a blender duplicate/junk (ends in .001, .002, etc)
            actionSelection[action] = !System.Text.RegularExpressions.Regex.IsMatch(action, @"\.\d{3}$");
        }
        
        Debug.Log("Found actions: " + string.Join(", ", detectedActions));
    }

    void BuildAnimator()
    {
        if (targetGameObject == null || fbxAsset == null) { Debug.LogError("Missing Scene GameObject or FBX Asset!"); return; }
        if (detectedActions.Count == 0) { Debug.LogError("No actions scanned!"); return; }

        string prefabName = fbxAsset.name;
        string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        // 1. Build hierarchy paths
        Dictionary<string, string> nameToPath = new Dictionary<string, string>();
        BuildPathLookup(fbxAsset.transform, "", nameToPath);

        // 2. Ensure merged folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Merged")) AssetDatabase.CreateFolder("Assets/Animations", "Merged");

        // 3. Create Animator Controller
        string controllerPath = $"Assets/Animations/{prefabName}Animator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }
        
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Clear old states
        var states = rootStateMachine.states;
        foreach (var childState in states)
        {
            rootStateMachine.RemoveState(childState.state);
        }

        // Setup Idle state
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        rootStateMachine.defaultState = idleState;

        // 4. Merge each action
        foreach (string actionName in detectedActions)
        {
            if (!actionSelection.ContainsKey(actionName) || !actionSelection[actionName]) 
                continue;

            AnimationClip mergedClip = MergeAction(allAssets, actionName, $"{prefabName}_{actionName}", nameToPath);
            
            AnimatorState actionState = rootStateMachine.AddState($"{prefabName}_{actionName}");
            actionState.motion = mergedClip;

            controller.AddParameter(actionName, AnimatorControllerParameterType.Trigger);

            AnimatorStateTransition toAction = rootStateMachine.AddAnyStateTransition(actionState);
            toAction.hasExitTime = false;
            toAction.canTransitionToSelf = false;
            toAction.duration = 0f;
            toAction.AddCondition(AnimatorConditionMode.If, 0, actionName);

            AnimatorStateTransition fromAction = actionState.AddTransition(idleState);
            fromAction.hasExitTime = true;
            fromAction.duration = 0f;
        }

        // 5. Apply to Scene Object
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targetGameObject);
        foreach (var childAnim in targetGameObject.GetComponentsInChildren<Animator>(true))
        {
            if (childAnim.gameObject != targetGameObject)
            {
                DestroyImmediate(childAnim);
            }
        }

        Animator anim = targetGameObject.GetComponent<Animator>();
        if (anim == null) anim = targetGameObject.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully built Animator for {prefabName}!");
    }

    AnimationClip MergeAction(Object[] allAssets, string actionSuffix, string outputName, Dictionary<string, string> nameToPath)
    {
        AnimationClip mergedClip = new AnimationClip();
        mergedClip.name = outputName;

        List<AnimationClip> actionClips = new List<AnimationClip>();
        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name.EndsWith("|" + actionSuffix))
            {
                actionClips.Add(clip);
            }
        }

        foreach (var sourceClip in actionClips)
        {
            int pipeIndex = sourceClip.name.IndexOf('|');
            string pieceName = sourceClip.name.Substring(0, pipeIndex);

            // CRITICAL: Blender names duplicates with .001. So "Left Leg.001" must become "Left Leg"
            int dotIndex = pieceName.LastIndexOf(".00");
            if (dotIndex > 0)
            {
                pieceName = pieceName.Substring(0, dotIndex);
            }

            string realPath = pieceName;
            if (isSinglePiece)
            {
                realPath = "";
            }
            else if (nameToPath.ContainsKey(pieceName))
            {
                realPath = nameToPath[pieceName];
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                if (string.IsNullOrEmpty(realPath) && !isSinglePiece)
                {
                    if (binding.propertyName.StartsWith("m_LocalPosition") || 
                        binding.propertyName.StartsWith("m_LocalRotation") || 
                        binding.propertyName.StartsWith("localEulerAnglesRaw"))
                    {
                        continue;
                    }
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                EditorCurveBinding newBinding = binding;
                newBinding.path = realPath;
                AnimationUtility.SetEditorCurve(mergedClip, newBinding, curve);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                EditorCurveBinding newBinding = binding;
                newBinding.path = realPath;
                AnimationUtility.SetObjectReferenceCurve(mergedClip, newBinding, keyframes);
            }
        }

        string outPath = $"Assets/Animations/Merged/{outputName}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mergedClip, existing);
            return existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mergedClip, outPath);
            return mergedClip;
        }
    }

    void BuildPathLookup(Transform parent, string currentPath, Dictionary<string, string> lookup)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            string childPath = string.IsNullOrEmpty(currentPath) ? child.name : currentPath + "/" + child.name;

            if (!lookup.ContainsKey(child.name))
            {
                lookup[child.name] = childPath;
            }

            BuildPathLookup(child, childPath, lookup);
        }
    }
}
