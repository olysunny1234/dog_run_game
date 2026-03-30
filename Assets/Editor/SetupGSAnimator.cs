using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupGSAnimator
{
    [MenuItem("Tools/Build GS Controller")]
    public static void BuildController()
    {
        string savePath = "Assets/Resources/Models/Dog/AC_GS_Gameplay.controller";

        // Create fresh controller
        var ac = AnimatorController.CreateAnimatorControllerAtPath(savePath);

        // Add parameters
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        ac.AddParameter("TurnDir", AnimatorControllerParameterType.Float);

        var sm = ac.layers[0].stateMachine;

        // Load clips
        AnimationClip idleClip  = FindClip("Assets/Resources/Models/Dog/A_Idle_Breathing.fbx");
        AnimationClip walkClip  = FindClip("Assets/Resources/Models/Dog/A_Walk_Turn_Right.fbx");
        AnimationClip runClip   = FindClip("Assets/Resources/Models/Dog/A_Run_Loop.fbx");
        AnimationClip runLeftClip  = FindClip("Assets/Resources/Models/Dog/A_Run_Lean_Left.fbx");
        AnimationClip runRightClip = FindClip("Assets/Resources/Models/Dog/A_Run_Lean_Right.fbx");
        AnimationClip jumpClip  = FindClip("Assets/Resources/Models/Dog/A_Idle_Playing.fbx");

        // --- States ---
        var idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        var walkState = sm.AddState("Walk", new Vector3(300, 80, 0));
        // Blend Walk_Turn_Left + Walk_Turn_Right so turns cancel out = straight walk
        // TurnDir controls which direction the walk leans
        AnimationClip walkLeftClip = FindClip("Assets/Resources/Models/Dog/A_Walk_Turn_Left.fbx");
        if (walkClip != null && walkLeftClip != null)
        {
            var walkBt = new BlendTree();
            walkBt.name = "WalkBlend";
            walkBt.blendParameter = "TurnDir";
            walkBt.blendType = BlendTreeType.Simple1D;
            walkBt.useAutomaticThresholds = false;
            walkBt.AddChild(walkLeftClip, -1f);
            // Add both clips at 0 threshold for a 50/50 blend when going straight
            walkBt.AddChild(walkLeftClip, -0.01f);
            walkBt.AddChild(walkClip, 0.01f);
            walkBt.AddChild(walkClip, 1f);
            AssetDatabase.AddObjectToAsset(walkBt, ac);
            walkState.motion = walkBt;
        }
        else
        {
            walkState.motion = walkClip;
        }

        // Run state uses a blend tree for lean left/right
        var runState = sm.AddState("Run", new Vector3(300, 160, 0));
        if (runClip != null && runLeftClip != null && runRightClip != null)
        {
            var bt = new BlendTree();
            bt.name = "RunBlend";
            bt.blendParameter = "TurnDir";
            bt.blendType = BlendTreeType.Simple1D;
            bt.useAutomaticThresholds = false;
            bt.AddChild(runLeftClip, -1f);
            bt.AddChild(runClip, 0f);
            bt.AddChild(runRightClip, 1f);
            AssetDatabase.AddObjectToAsset(bt, ac);
            runState.motion = bt;
        }
        else
        {
            runState.motion = runClip;
        }

        var jumpState = sm.AddState("Jump", new Vector3(550, 80, 0));
        jumpState.motion = jumpClip;
        jumpState.speed = 2.0f; // play jump animation faster for snappier feel

        // --- Transitions ---
        // Idle -> Walk (Speed > 0.1)
        var t1 = idleState.AddTransition(walkState);
        t1.hasExitTime = false;
        t1.duration = 0.15f;
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // Walk -> Idle (Speed < 0.1)
        var t2 = walkState.AddTransition(idleState);
        t2.hasExitTime = false;
        t2.duration = 0.2f;
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Walk -> Run (Speed > 0.55)
        var t3 = walkState.AddTransition(runState);
        t3.hasExitTime = false;
        t3.duration = 0.15f;
        t3.AddCondition(AnimatorConditionMode.Greater, 0.55f, "Speed");

        // Run -> Walk (Speed < 0.5)
        var t4 = runState.AddTransition(walkState);
        t4.hasExitTime = false;
        t4.duration = 0.2f;
        t4.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");

        // AnyState -> Jump (IsJumping = true)
        var t5 = sm.AddAnyStateTransition(jumpState);
        t5.hasExitTime = false;
        t5.duration = 0.1f;
        t5.canTransitionToSelf = false;
        t5.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");

        // Jump -> Idle (IsJumping = false)
        var t6 = jumpState.AddTransition(idleState);
        t6.hasExitTime = false;
        t6.duration = 0.15f;
        t6.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("GS Gameplay Controller built! States: Idle/Walk/Run(BlendTree)/Jump. Clips: "
            + "Idle=" + (idleClip != null)
            + " Walk=" + (walkClip != null)
            + " Run=" + (runClip != null)
            + " Jump=" + (jumpClip != null));
    }

    [MenuItem("Tools/Fix GS Animation Loops")]
    public static void FixLoops()
    {
        string[] loopClips = new string[]
        {
            "Assets/Resources/Models/Dog/A_Idle_Breathing.fbx",
            "Assets/Resources/Models/Dog/A_Walk_Turn_Right.fbx",
            "Assets/Resources/Models/Dog/A_Walk_Turn_Left.fbx",
            "Assets/Resources/Models/Dog/A_Run_Loop.fbx",
            "Assets/Resources/Models/Dog/A_Run_Lean_Left.fbx",
            "Assets/Resources/Models/Dog/A_Run_Lean_Right.fbx",
        };
        foreach (string path in loopClips)
            SetClipLooping(path, true);
        SetClipLooping("Assets/Resources/Models/Dog/A_Idle_Playing.fbx", false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All GS animation loop settings fixed!");
    }

    static void SetClipLooping(string fbxPath, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static AnimationClip FindClip(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            var clip = a as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
