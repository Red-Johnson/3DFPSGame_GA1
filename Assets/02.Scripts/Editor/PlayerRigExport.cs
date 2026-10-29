using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

// PlayerAutoRig가 만든 PlayerModel.prefab을 뼈대가 들어 있는 FBX로 내보내고,
// 그 FBX의 Rig를 Humanoid(Create From This Model)로 설정한다. (Configure Avatar 창에서 뼈 매핑을 볼 수 있게 된다.)
public static class PlayerRigExport
{
    private const string PrefabPath = "Assets/03.Prefabs/Player/PlayerModel.prefab";
    private const string FbxPath = "Assets/03.Prefabs/Player/PlayerRigged.fbx";
    private const string MaterialPath = "Assets/03.Prefabs/Player/tripo_mat_98dd9a69.mat";

    [MenuItem("Tools/Player/Export Rigged FBX")]
    public static void Run()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PlayerRigExport] {PrefabPath}를 찾을 수 없습니다. 먼저 Tools/Player/Auto Rig를 실행하세요.");
            return;
        }

        // 내보내기 전용 복제본: Animator의 컨트롤러와 아바타는 FBX가 새로 만들므로 내보내기에 필요 없다.
        GameObject copy = Object.Instantiate(prefab);
        copy.name = "PlayerRigged";
        Object.DestroyImmediate(copy.GetComponent<Animator>());

        string exported = ModelExporter.ExportObject(FbxPath, copy);
        Object.DestroyImmediate(copy);
        AssetDatabase.Refresh();

        if (string.IsNullOrEmpty(exported))
        {
            Debug.LogError("[PlayerRigExport] FBX 내보내기에 실패했습니다.");
            return;
        }

        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.SaveAndReimport();

        // 내보낸 FBX가 새로 만든 머티리얼 대신, 이미 설정해 둔 머티리얼(양면 렌더링)을 쓰게 연결한다.
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        GameObject exportedModel = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        SkinnedMeshRenderer renderer = exportedModel != null ? exportedModel.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        if (material != null && renderer != null && renderer.sharedMaterial != null)
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), renderer.sharedMaterial.name), material);
            importer.SaveAndReimport();
        }

        Debug.Log($"[PlayerRigExport] 완료: {FbxPath}");
    }
}
