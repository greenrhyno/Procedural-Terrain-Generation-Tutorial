using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail, bool useFlatShading)
    {
        AnimationCurve _heightCurve = new AnimationCurve(heightCurve.keys);

        int simplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0); // assumes heightMap is SQUARE
        int meshSize = borderedSize - 2*simplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        // int verticesPerLine = (meshSize - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(borderedSize, useFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                int vertexIdx = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2(
                    (x - simplificationIncrement) / (float) meshSize,
                    (y - simplificationIncrement) / (float) meshSize
                );
                Vector3 vertexPosition = new Vector3(
                    topLeftX + percent.x * meshSizeUnsimplified, 
                    _heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier,
                    topLeftZ - percent.y * meshSizeUnsimplified
                );

                meshData.AddVertex(vertexPosition, percent, vertexIdx);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + simplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + simplificationIncrement];
                    int d = vertexIndicesMap[x + simplificationIncrement, y + simplificationIncrement];
                    
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIdx++;

            }
        }
        
        meshData.FinalizeMesh();

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;
    bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];

        this.useFlatShading = useFlatShading;
    }

    public void FinalizeMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    public void AddVertex(Vector3 vertexPos, Vector2 uv, int vertexIdx)
    {
        // if vertex is a border vertex
        if (vertexIdx < 0)
        {
            borderVertices[-vertexIdx - 1] = vertexPos;
        } 
        else
        {
            vertices[vertexIdx] = vertexPos;
            uvs[vertexIdx] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIdx = i * 3;
            int vertexIdxA = triangles[normalTriangleIdx];
            int vertexIdxB = triangles[normalTriangleIdx+1];
            int vertexIdxC = triangles[normalTriangleIdx+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIdxA, vertexIdxB, vertexIdxC);

            vertexNormals[vertexIdxA] += triangleNormal;
            vertexNormals[vertexIdxB] += triangleNormal;
            vertexNormals[vertexIdxC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIdx = i * 3;
            int vertexIdxA = borderTriangles[normalTriangleIdx];
            int vertexIdxB = borderTriangles[normalTriangleIdx + 1];
            int vertexIdxC = borderTriangles[normalTriangleIdx + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIdxA, vertexIdxB, vertexIdxC);

            if (vertexIdxA >= 0) vertexNormals[vertexIdxA] += triangleNormal;
            if (vertexIdxB >= 0) vertexNormals[vertexIdxB] += triangleNormal;
            if (vertexIdxC >= 0) vertexNormals[vertexIdxC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++) vertexNormals[i] = vertexNormals[i].normalized;
        
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int idxA, int idxB, int idxC)
    {
        Vector3 pointA = (idxA < 0) ? borderVertices[-idxA-1] : vertices[idxA];
        Vector3 pointB = (idxB < 0) ? borderVertices[-idxB - 1] : vertices[idxB];
        Vector3 pointC = (idxC < 0) ? borderVertices[-idxC - 1] : vertices[idxC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }
        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading) mesh.RecalculateNormals();
        else mesh.normals = bakedNormals;
        return mesh;
    }
}