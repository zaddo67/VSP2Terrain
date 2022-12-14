using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using UnityEngine.SceneManagement;
using AwesomeTechnologies.VegetationSystem;
using System.Linq;

public class VegetationStudioToTerrain : EditorWindow
{

    static int MinWidth = 475;
    Color defColor;
    PersistentVegetationStorage m_storage;
    float m_scaleFactor = 1f;
    Dictionary<string, int> treeXref = new Dictionary<string, int>();
    Dictionary<string, int> detailXref = new Dictionary<string, int>();
    List<TreeInstance> m_trees = new List<TreeInstance>();

    [MenuItem("Tools/Utilities/Vegetation Studio to Terrain")]
    static void Init()
    {
        VegetationStudioToTerrain window = (VegetationStudioToTerrain)EditorWindow.GetWindow(typeof(VegetationStudioToTerrain));
        window.minSize = new Vector2(MinWidth, 475);
    }

    private void OnGUI()
    {
        defColor = GUI.color;

        m_storage = (PersistentVegetationStorage)EditorGUILayout.ObjectField("Persistent Storage", m_storage, typeof(PersistentVegetationStorage), true);
        m_scaleFactor = EditorGUILayout.FloatField("Scale Factor", m_scaleFactor);
        if (GUILayout.Button("Convert", GUILayout.Width(80), GUILayout.Height(40)))
            ConvertVS();
    }

    private void ConvertVS()
    {
        PersistentVegetationStoragePackage storagePackage = m_storage.PersistentVegetationStoragePackage;
        if (storagePackage == null)
        {
            Debug.LogError($"Vegetation Storage Package not found!");
            return;
        }


        VegetationSystemPro vspro = m_storage.gameObject.GetComponent<VegetationSystemPro>();
        if (vspro == null)
        {
            Debug.LogError($"Vegetation System Pro not found!");
            return;
        }

        Terrain terrain = FindTerrain(m_storage.gameObject.scene);
        if (terrain == null)
        {
            Debug.LogError($"Terrain not found!");
            return;
        }

        var vegetation = GetVegetation(vspro);

        // Load Prototypes into terrain
        SetTerrainVegetation(terrain, vegetation);

        // Convert VSP vegetation into terrain vegetation
        ConvertStoragePackage(terrain, storagePackage);
        DumpTrees(terrain);

        terrain.Flush();
    }

    private void DumpTrees(Terrain terrain)
    {
        var instances = terrain.terrainData.treeInstances;

        foreach (var tree in instances)
        {
            Vector3 p1 = tree.position;
            Debug.Log($"Tree[{tree.prototypeIndex}] [{p1.x:0.00},{p1.y:0.00},{p1.z:0.00}] Scale[{tree.widthScale:0.00},{tree.heightScale:0.00}]");

        }
    }

    private void ConvertStoragePackage(Terrain terrain, PersistentVegetationStoragePackage storagePackage)
    {
        //terrain.terrainData.treeInstances = new TreeInstance[0];
        m_trees.Clear();

        foreach (var cell in storagePackage.PersistentVegetationCellList)
        {
            foreach (var info in cell.PersistentVegetationInfoList)
            {
                if (IsTree(info.VegetationItemID)) LoadTrees(terrain, info);
            }
        }

        terrain.terrainData.treeInstances = m_trees.ToArray();
    }

    private void LoadTrees(Terrain terrain, PersistentVegetationInfo info)
    {
        int treeIndex = treeXref[info.VegetationItemID];

        foreach (var tree in info.VegetationItemList)
        {
            TreeInstance instance = new TreeInstance();
            instance.prototypeIndex = treeIndex;
            instance.position = ToTerrainPosition(terrain, tree.Position);

            instance.heightScale = tree.Scale.y / m_scaleFactor;
            instance.widthScale = tree.Scale.x / m_scaleFactor;
            instance.rotation = tree.Rotation.eulerAngles.y;
            //terrain.AddTreeInstance(instance);
            m_trees.Add(instance);

            Vector3 p1 = instance.position;
            Vector3 p2 = tree.Position;
            Vector3 p3 = tree.Scale / m_scaleFactor;

            Debug.Log($"Place Tree[{treeIndex}] [{p1.x:0.00},{p1.y:0.00},{p1.z:0.00}] Original[{p2.x:0.00},{p2.y:0.00},{p2.z:0.00}] Scale[{p3.x:0.00},{p3.y:0.00},{p3.z:0.00}]");
        }
    }


    private Vector3 ToTerrainPosition(Terrain terrain, Vector3 worldPos)
    {
        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = (worldPos.x / terrain.terrainData.size.x); // * terrain.terrainData.alphamapWidth;
        vecRet.z = (worldPos.z / terrain.terrainData.size.z); // * terrain.terrainData.alphamapHeight;
        vecRet.y = worldPos.y / terrain.terrainData.size.y;
        return vecRet;
    }

    private bool IsTree(string id)
    {
        return treeXref.ContainsKey(id);
    }

    private bool IsDetail(string id)
    {
        return detailXref.ContainsKey(id);
    }


    private void SetTerrainVegetation(Terrain terrain, List<VegetationItemInfoPro> vegetation)
    {

        treeXref.Clear();
        detailXref.Clear();

        List<TreePrototype> terrainTrees = terrain.terrainData.treePrototypes.ToList();
        List<DetailPrototype> terrainDetails = terrain.terrainData.detailPrototypes.ToList();

        foreach (var item in vegetation)
        {
            if (item.VegetationType == VegetationType.Tree)
            {

                GameObject treePrefab = item.VegetationPrefab;
                var prefabRef = treePrefab.GetComponent<PrefabReference>();
                if (prefabRef != null)
                {
                    if (prefabRef.PrefebReferencePrefab != null) treePrefab = prefabRef.PrefebReferencePrefab;
                }

                // Ensure tree does not already exist
                if (terrainTrees.Any(t => t.prefab == treePrefab))
                {
                    for (int i = 0; i < terrainTrees.Count; i++)
                    {
                        if (terrainTrees[i].prefab == treePrefab)
                        {
                            treeXref.Add(item.VegetationItemID, i);
                        }
                    }
                    continue;
                }

                TreePrototype newTree = new TreePrototype();
                newTree.prefab = treePrefab;

                terrainTrees.Add(newTree);
                treeXref.Add(item.VegetationItemID, terrainTrees.Count - 1);
                Debug.Log($"Terrain Tree Added: {item.VegetationPrefab}: {item.VegetationItemID}");


            }

            if (item.VegetationType != VegetationType.Tree)
            {
                // Ensure tree does not already exist
                if (terrainDetails.Any(t => t.prototype == item.VegetationPrefab))
                {
                    for (int i = 0; i < terrainDetails.Count; i++)
                    {
                        if (terrainDetails[i].prototype == item.VegetationPrefab)
                        {
                            detailXref.Add(item.VegetationItemID, i);
                        }
                    }
                    continue;
                }

                DetailPrototype newDetail = new DetailPrototype();
                newDetail.prototype = item.VegetationPrefab;
                newDetail.prototypeTexture = null;
                newDetail.usePrototypeMesh = true;
                newDetail.useInstancing = true;
                newDetail.renderMode = DetailRenderMode.VertexLit;

                terrainDetails.Add(newDetail);
                detailXref.Add(item.VegetationItemID, terrainDetails.Count - 1);
                Debug.Log($"Terrain Detail Added: {item.VegetationPrefab}: {item.VegetationItemID}");
            }
        }


        // Update Terrain
        terrain.terrainData.treePrototypes = terrainTrees.ToArray();
        terrain.terrainData.detailPrototypes = terrainDetails.ToArray();

    }

    private List<VegetationItemInfoPro> GetVegetation(VegetationSystemPro vspro)
    {
        List<VegetationItemInfoPro> result = new List<VegetationItemInfoPro>();

        foreach (var biome in vspro.VegetationPackageProList)
        {
            foreach (var v in biome.VegetationInfoList)
            {
                result.Add(v);
            }
        }
        return result;
    }

    private Terrain FindTerrain(Scene scene)
    {
        Terrain terrain = null;
        var gameObjects = scene.GetRootGameObjects();

        foreach (var obj in gameObjects)
        {
            terrain = FindTerrain(obj);
            if (terrain != null) break;
        }

        return terrain;
    }

    private Terrain FindTerrain(GameObject obj)
    {
        Terrain terrain = obj.GetComponent<Terrain>();
        if (terrain == null)
        {
            terrain = obj.GetComponentInChildren<Terrain>();
        }
        return terrain;
    }
}
