using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Blocky 5 x 7 letters built as a mesh, the same look as the "SAÍDA" and
    // "13" plates modelled in Blender. Real geometry, so walls hide it (the
    // legacy TextMesh shader draws through everything).
    public static class PixelLetters
    {
        // Two accent rows on top, then seven rows of the letter.
        static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>
        {
            { 'A', new[] { ".....", ".....", ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'C', new[] { ".....", ".....", ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." } },
            { 'D', new[] { ".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
            { 'E', new[] { ".....", ".....", "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
            { 'F', new[] { ".....", ".....", "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
            { 'G', new[] { ".....", ".....", ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." } },
            { 'I', new[] { ".....", ".....", ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { 'M', new[] { ".....", ".....", "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
            { 'N', new[] { ".....", ".....", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
            { 'O', new[] { ".....", ".....", ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'R', new[] { ".....", ".....", "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
            { 'S', new[] { ".....", ".....", ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
            { 'T', new[] { ".....", ".....", "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'U', new[] { ".....", ".....", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'Í', new[] { "...#.", "..#..", ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { 'Á', new[] { "...#.", "..#..", ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        };

        public const int Rows = 9;

        // Width of the widest line in pixels (5 per letter, 1 between).
        public static int Columns(string text)
        {
            int best = 0;
            foreach (var line in text.Split('\n')) best = Mathf.Max(best, line.Length * 6 - 1);
            return best;
        }

        // A mesh in the XY plane, centred, reading along +X, facing -Z
        // (visible from a camera on -Z looking +Z). Lines stack downward.
        public static Mesh Build(string text, float pixel, float lineGap = 2f)
        {
            var lines = text.Split('\n');
            int columns = Columns(text);
            float height = lines.Length * Rows + (lines.Length - 1) * lineGap;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l];
                float left = -(line.Length * 6 - 1) * .5f;
                float top = height * .5f - l * (Rows + lineGap);
                for (int c = 0; c < line.Length; c++)
                {
                    if (!glyphs.TryGetValue(char.ToUpperInvariant(line[c]), out var glyph)) continue;
                    for (int row = 0; row < Rows; row++)
                    for (int col = 0; col < 5; col++)
                    {
                        if (glyph[row][col] != '#') continue;
                        float x0 = (left + c * 6 + col) * pixel, x1 = x0 + pixel;
                        float y1 = (top - row) * pixel, y0 = y1 - pixel;
                        int start = vertices.Count;
                        vertices.Add(new Vector3(x0, y0, 0f));
                        vertices.Add(new Vector3(x0, y1, 0f));
                        vertices.Add(new Vector3(x1, y1, 0f));
                        vertices.Add(new Vector3(x1, y0, 0f));
                        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                    }
                }
            }
            var mesh = new Mesh { name = "Letreiro " + text.Replace('\n', ' ') };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
