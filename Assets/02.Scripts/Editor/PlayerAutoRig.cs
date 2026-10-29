using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 뼈대가 없는 플레이어 모델(Player.fbx)에 Humanoid 뼈대와 스킨 가중치를 만들어 주는 에디터 도구
// - 모델은 T자 자세로 서서 +Z를 보고, 발이 y=0에 있다고 가정한다. (뼈 위치는 아래 Joint 표에 비율로 적어 둔다.)
// - 정점마다 가까운 뼈 구간에 거리 기반으로 가중치를 주는 간단한 방식이라, 옷자락이 많은 부분은 보정이 필요할 수 있다.
// - 결과: PlayerSkin.asset(스킨 메시), PlayerAvatar.asset(Humanoid 아바타), PlayerModel.prefab(Animator가 붙은 모델)
public static class PlayerAutoRig
{
    private const string SourcePath = "Assets/03.Prefabs/Player/Player.fbx";
    private const string OutputFolder = "Assets/03.Prefabs/Player";
    private const string ControllerPath = "Assets/07.Animations/Soldier/SoldierAnimatorController.controller";

    private const float TargetHeight = 1.8f;        // 원래 군인 모델의 키에 맞춘다.
    private const float WeightFalloffPower = 4f;    // 클수록 뼈 경계가 또렷해진다.
    private const float WeightDistanceBias = 0.02f; // 뼈 위에 있는 정점의 가중치가 무한대가 되지 않게 한다.
    private const float MinWeightRatio = 0.08f;     // 가장 큰 가중치의 이 비율보다 작은 뼈는 무시한다.
    private const float LegPenaltyForUpperBody = 0.15f; // 소매 천이 다리에 붙지 않도록 상체 바깥쪽 정점의 다리 거리에 더한다.
    private const float ArmPenaltyForLowerBody = 0.15f; // 코트 자락이 팔에 붙지 않도록 하체 정점의 팔 거리에 더한다.

    private class Joint
    {
        public string Name;
        public string Parent;
        public HumanBodyBones Human;
        public Vector3 Head;    // 모델 높이 1.0 기준 위치(오른쪽이 +X)
        public Vector3 Tail;    // 이 뼈가 가리키는 끝 위치. 가중치 계산에만 쓴다.
        public Transform Transform;
    }

    private static List<Joint> BuildJoints()
    {
        const float Z = 0.03f;      // 몸통 중심의 앞뒤 위치
        var list = new List<Joint>
        {
            new Joint { Name = "Hips", Parent = null, Human = HumanBodyBones.Hips, Head = new Vector3(0f, 0.53f, Z), Tail = new Vector3(0f, 0.60f, Z) },
            new Joint { Name = "Spine", Parent = "Hips", Human = HumanBodyBones.Spine, Head = new Vector3(0f, 0.60f, Z), Tail = new Vector3(0f, 0.66f, Z) },
            new Joint { Name = "Chest", Parent = "Spine", Human = HumanBodyBones.Chest, Head = new Vector3(0f, 0.66f, Z), Tail = new Vector3(0f, 0.755f, Z) },
            new Joint { Name = "Neck", Parent = "Chest", Human = HumanBodyBones.Neck, Head = new Vector3(0f, 0.755f, Z), Tail = new Vector3(0f, 0.79f, Z) },
            new Joint { Name = "Head", Parent = "Neck", Human = HumanBodyBones.Head, Head = new Vector3(0f, 0.79f, Z), Tail = new Vector3(0f, 1.0f, Z) },
        };

        AddSide(list, "Right", 1f, Z);
        AddSide(list, "Left", -1f, Z);
        return list;
    }

    // 팔과 다리는 좌우가 대칭이라 한 번에 만든다. (side: 오른쪽 +1, 왼쪽 -1)
    private static void AddSide(List<Joint> list, string side, float sign, float z)
    {
        bool right = sign > 0f;
        HumanBodyBones Pick(HumanBodyBones r, HumanBodyBones l) => right ? r : l;

        list.Add(new Joint { Name = side + "Shoulder", Parent = "Chest", Human = Pick(HumanBodyBones.RightShoulder, HumanBodyBones.LeftShoulder), Head = new Vector3(sign * 0.04f, 0.715f, z), Tail = new Vector3(sign * 0.11f, 0.715f, z) });
        list.Add(new Joint { Name = side + "UpperArm", Parent = side + "Shoulder", Human = Pick(HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm), Head = new Vector3(sign * 0.11f, 0.715f, z), Tail = new Vector3(sign * 0.21f, 0.70f, z) });
        list.Add(new Joint { Name = side + "LowerArm", Parent = side + "UpperArm", Human = Pick(HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm), Head = new Vector3(sign * 0.21f, 0.70f, z), Tail = new Vector3(sign * 0.31f, 0.683f, z) });
        list.Add(new Joint { Name = side + "Hand", Parent = side + "LowerArm", Human = Pick(HumanBodyBones.RightHand, HumanBodyBones.LeftHand), Head = new Vector3(sign * 0.31f, 0.683f, z), Tail = new Vector3(sign * 0.376f, 0.67f, z) });

        list.Add(new Joint { Name = side + "UpperLeg", Parent = "Hips", Human = Pick(HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg), Head = new Vector3(sign * 0.05f, 0.53f, z), Tail = new Vector3(sign * 0.05f, 0.30f, z) });
        list.Add(new Joint { Name = side + "LowerLeg", Parent = side + "UpperLeg", Human = Pick(HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg), Head = new Vector3(sign * 0.05f, 0.30f, z), Tail = new Vector3(sign * 0.05f, 0.065f, z) });
        list.Add(new Joint { Name = side + "Foot", Parent = side + "LowerLeg", Human = Pick(HumanBodyBones.RightFoot, HumanBodyBones.LeftFoot), Head = new Vector3(sign * 0.05f, 0.065f, z), Tail = new Vector3(sign * 0.05f, 0.02f, 0.13f) });
    }

    [MenuItem("Tools/Player/Auto Rig")]
    public static void Run()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null)
        {
            Debug.LogError($"[PlayerAutoRig] {SourcePath}를 찾을 수 없습니다.");
            return;
        }

        // 1. 원본 메시를 가져와 Y가 위인 월드 좌표계 정점으로 바꾼다. (FBX 노드의 회전을 정점에 반영)
        GameObject temp = Object.Instantiate(source);
        MeshFilter sourceFilter = temp.GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = temp.GetComponentInChildren<MeshRenderer>();
        Mesh sourceMesh = sourceFilter.sharedMesh;
        Material material = sourceRenderer.sharedMaterial;
        Matrix4x4 toWorld = temp.transform.worldToLocalMatrix * sourceFilter.transform.localToWorldMatrix;

        Vector3[] vertices = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector4[] tangents = sourceMesh.tangents;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = toWorld.MultiplyPoint3x4(vertices[i]);
            minY = Mathf.Min(minY, vertices[i].y);
            maxY = Mathf.Max(maxY, vertices[i].y);
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = toWorld.MultiplyVector(normals[i]).normalized;
        }

        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 t = toWorld.MultiplyVector(tangents[i]).normalized;
            tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
        }

        // 발이 y=0에 오고 키가 1이 되도록 맞춘 뒤(뼈 위치 표와 같은 기준), 목표 키로 키운다.
        float height = maxY - minY;
        float modelScale = TargetHeight / height;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            v.y -= minY;
            vertices[i] = v * modelScale;
        }

        Object.DestroyImmediate(temp);

        // 2. 뼈 오브젝트 계층을 만든다. (뼈 위치는 높이 1 기준 표에 목표 키를 곱한다.)
        List<Joint> joints = BuildJoints();
        GameObject root = new GameObject("Model");
        Dictionary<string, Joint> byName = new Dictionary<string, Joint>();
        foreach (Joint joint in joints)
        {
            byName[joint.Name] = joint;
            GameObject boneObject = new GameObject(joint.Name);
            joint.Transform = boneObject.transform;
            joint.Transform.SetParent(joint.Parent == null ? root.transform : byName[joint.Parent].Transform, false);
            joint.Transform.position = joint.Head * TargetHeight;
        }

        // 3. 정점마다 가까운 뼈 구간에 거리 기반 가중치를 준다.
        int boneCount = joints.Count;
        BoneWeight[] weights = new BoneWeight[vertices.Length];
        float[] distances = new float[boneCount];
        float[] raw = new float[boneCount];
        for (int vi = 0; vi < vertices.Length; vi++)
        {
            Vector3 v = vertices[vi];
            float normalizedY = v.y / TargetHeight;
            float absX = Mathf.Abs(v.x) / TargetHeight;
            float best = 0f;

            for (int bi = 0; bi < boneCount; bi++)
            {
                Joint joint = joints[bi];
                float d = DistanceToSegment(v, joint.Head * TargetHeight, joint.Tail * TargetHeight);

                // 상체 바깥쪽의 소매 천은 다리가 아니라 팔을 따르고, 하체의 코트 자락은 팔이 아니라 다리를 따르게 한다.
                bool isLeg = joint.Name.EndsWith("Leg") || joint.Name.EndsWith("Foot");
                bool isArm = joint.Name.EndsWith("Arm") || joint.Name.EndsWith("Hand") || joint.Name.EndsWith("Shoulder");
                if (isLeg && normalizedY > 0.45f && absX > 0.13f) d += LegPenaltyForUpperBody * TargetHeight;
                if (isArm && normalizedY < 0.42f) d += ArmPenaltyForLowerBody * TargetHeight;

                distances[bi] = d;
                raw[bi] = 1f / Mathf.Pow(d + WeightDistanceBias * TargetHeight, WeightFalloffPower);
                best = Mathf.Max(best, raw[bi]);
            }

            weights[vi] = PickTopFourWeights(raw, best * MinWeightRatio);
        }

        // 4. 스킨 메시를 만든다. (정점이 6만 개를 넘으므로 32비트 인덱스를 쓴다.)
        Mesh skinMesh = new Mesh { name = "PlayerSkin", indexFormat = IndexFormat.UInt32 };
        skinMesh.vertices = vertices;
        if (normals.Length == vertices.Length) skinMesh.normals = normals;
        if (tangents.Length == vertices.Length) skinMesh.tangents = tangents;
        skinMesh.uv = sourceMesh.uv;
        skinMesh.triangles = sourceMesh.triangles;
        skinMesh.boneWeights = weights;

        Transform[] bones = new Transform[boneCount];
        Matrix4x4[] bindPoses = new Matrix4x4[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            bones[i] = joints[i].Transform;
            bindPoses[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
        }

        skinMesh.bindposes = bindPoses;
        skinMesh.RecalculateBounds();

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        SkinnedMeshRenderer skinned = body.AddComponent<SkinnedMeshRenderer>();
        skinned.sharedMesh = skinMesh;
        skinned.sharedMaterial = material;
        skinned.bones = bones;
        skinned.rootBone = byName["Hips"].Transform;
        skinned.localBounds = skinMesh.bounds;

        // 5. Humanoid 아바타를 만든다.
        Avatar avatar = BuildAvatar(root, joints);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            Debug.LogError("[PlayerAutoRig] Humanoid 아바타를 만들지 못했습니다. 뼈 위치를 확인하세요.");
            Object.DestroyImmediate(root);
            return;
        }

        avatar.name = "PlayerAvatar";

        Animator animator = root.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        animator.applyRootMotion = false;

        // 6. 에셋으로 저장한다.
        AssetDatabase.CreateAsset(skinMesh, $"{OutputFolder}/PlayerSkin.asset");
        AssetDatabase.CreateAsset(avatar, $"{OutputFolder}/PlayerAvatar.asset");
        PrefabUtility.SaveAsPrefabAsset(root, $"{OutputFolder}/PlayerModel.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerAutoRig] 완료: 정점 {vertices.Length}, 뼈 {boneCount}개, 크기 배율 {modelScale:F2}");
    }

    private static Avatar BuildAvatar(GameObject root, List<Joint> joints)
    {
        var humanBones = new List<HumanBone>();
        foreach (Joint joint in joints)
        {
            humanBones.Add(new HumanBone
            {
                boneName = joint.Name,
                humanName = HumanName(joint.Human),
                limit = new HumanLimit { useDefaultValues = true },
            });
        }

        var skeleton = new List<SkeletonBone>
        {
            new SkeletonBone { name = root.name, position = Vector3.zero, rotation = Quaternion.identity, scale = Vector3.one },
        };

        foreach (Joint joint in joints)
        {
            skeleton.Add(new SkeletonBone
            {
                name = joint.Name,
                position = joint.Transform.localPosition,
                rotation = joint.Transform.localRotation,
                scale = joint.Transform.localScale,
            });
        }

        HumanDescription description = new HumanDescription
        {
            human = humanBones.ToArray(),
            skeleton = skeleton.ToArray(),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false,
        };

        return AvatarBuilder.BuildHumanAvatar(root, description);
    }

    // HumanBodyBones 이름은 아바타 설정의 사람 뼈 이름과 띄어쓰기만 다르다. (예: LeftUpperArm -> "Left Upper Arm")
    private static string HumanName(HumanBodyBones bone)
    {
        foreach (string name in HumanTrait.BoneName)
        {
            if (name.Replace(" ", string.Empty) == bone.ToString())
            {
                return name;
            }
        }

        return bone.ToString();
    }

    private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSquared) : 0f;
        return Vector3.Distance(point, a + (ab * t));
    }

    // 가중치가 큰 뼈 네 개만 골라 합이 1이 되게 만든다.
    private static BoneWeight PickTopFourWeights(float[] raw, float minWeight)
    {
        int i0 = -1, i1 = -1, i2 = -1, i3 = -1;
        float w0 = 0f, w1 = 0f, w2 = 0f, w3 = 0f;

        for (int i = 0; i < raw.Length; i++)
        {
            float w = raw[i];
            if (w < minWeight) continue;

            if (w > w0) { i3 = i2; w3 = w2; i2 = i1; w2 = w1; i1 = i0; w1 = w0; i0 = i; w0 = w; }
            else if (w > w1) { i3 = i2; w3 = w2; i2 = i1; w2 = w1; i1 = i; w1 = w; }
            else if (w > w2) { i3 = i2; w3 = w2; i2 = i; w2 = w; }
            else if (w > w3) { i3 = i; w3 = w; }
        }

        float sum = w0 + w1 + w2 + w3;
        BoneWeight result = new BoneWeight();
        result.boneIndex0 = Mathf.Max(i0, 0); result.weight0 = w0 / sum;
        if (i1 >= 0) { result.boneIndex1 = i1; result.weight1 = w1 / sum; }
        if (i2 >= 0) { result.boneIndex2 = i2; result.weight2 = w2 / sum; }
        if (i3 >= 0) { result.boneIndex3 = i3; result.weight3 = w3 / sum; }
        return result;
    }
}
