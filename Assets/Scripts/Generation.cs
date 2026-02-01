using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateTestCube();
    }

    void GenerateTestCube()
    {
        List<Vector3> vectors = new List<Vector3>();
        List<int> tris = new List<int>();

        Vector3[][] allFaces = { Voxel_Verts.GetFrontFace(0, 0, 0), 
                                 Voxel_Verts.GetBackFace(0, 0, 0), 
                                 Voxel_Verts.GetLeftFace(0, 0, 0), 
                                 Voxel_Verts.GetRightFace(0, 0, 0), 
                                 Voxel_Verts.GetTopFace(0, 0, 0), 
                                 Voxel_Verts.GetBottomFace(0, 0, 0) };

        foreach(Vector3[] v in allFaces)
        {
            foreach(Vector3 vector in v)
            {
                vectors.Add(vector);
            }
        }

        int[][] alltris = { Voxel_Tris.GetFrontTris(0, 0, 0),
                            Voxel_Tris.GetBackTris(0, 0, 0),
                            Voxel_Tris.GetLeftTris(0, 0, 0),
                            Voxel_Tris.GetRightTris(0, 0, 0),
                            Voxel_Tris.GetTopTris(0, 0, 0),
                            Voxel_Tris.GetBottomTris(0, 0, 0) };

        foreach (int[] ints in alltris)
        {
            foreach (int i in ints)
            {
                tris.Add(i);
            }
        }

        GameObject obj = new GameObject();
        MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = vectors.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
