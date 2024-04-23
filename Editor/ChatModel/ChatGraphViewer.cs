using UnityEngine;
using UnityEditor;
using System;
using Unity.Sentis;
using System.Collections.Generic;
namespace Kurisu.UniChat.Editor.ChatModel
{
    public class ChatGraphViewer : EditorWindow
    {
        private Texture2D image;
        private ChatGraph.Edge[] edges;
        private Vector2[] data = new Vector2[0];
        public Action<ChatGraph.Edge?> OnSelectEdge;
        private string graphPath;
        private int? selectId;
        private static readonly Dictionary<string, ChatGraphViewer> map = new();
        public static ChatGraphViewer CreateWindow(string graphPath)
        {
            if (!map.TryGetValue(graphPath, out ChatGraphViewer window))
            {
                window = GetWindow<ChatGraphViewer>("ChatGraph Viewer");
                window.maxSize = window.minSize = new Vector2(552, 552);
            }
            window.LoadGraph(graphPath);
            return window;
        }
        private void OnEnable()
        {
            image = Resources.Load<Texture2D>("graph-point");
        }
        private void OnDisable()
        {
            OnSelectEdge = null;
            if (map.TryGetValue(graphPath, out var viewer) && viewer == this)
                map.Remove(graphPath);
        }
        private void OnGUI()
        {
            DrawGraph();
        }
        public void LoadGraph(string graphPath)
        {
            this.graphPath = graphPath;
            using var dataBase = new ChatDataBase(graphPath);
            int count = dataBase.edges.Count;
            edges = dataBase.edges.ToArray();
            Array.Resize(ref data, count);
            using var tensor = dataBase.AllocateTensors()[0];
            using var allocator = new TensorCachingAllocator();
            using var ops = WorkerFactory.CreateOps(BackendType.GPUCompute, allocator);
            //Using a simplest dimensionality reduction method
            using var projection = ops.RandomNormal(new TensorShape(dataBase.dim, 2), 0, 1, 0);
            using var transformed_vectors = ops.MatMul2D(tensor, projection, false, false);
            using var meanPool_vectors = ops.L2Norm(transformed_vectors);
            meanPool_vectors.MakeReadable();
            for (int i = 0; i < count; i++)
            {
                data[i] = new(meanPool_vectors[i, 0], meanPool_vectors[i, 1]);
            }
        }

        private void DrawGraph()
        {
            const int graphSize = 512;
            const int graphPadding = 20;
            Rect graphRect = new(graphPadding, graphPadding, graphSize, graphSize);
            EditorGUI.DrawRect(graphRect, Color.gray);
            bool selectEdge = false;
            for (int i = 0; i < data.Length; i++)
            {
                Vector2 point = data[i];
                Vector2 graphPoint = new(
                    Mathf.Lerp(graphRect.x, graphRect.xMax, (point.x + 1) * 0.5f),
                    Mathf.Lerp(graphRect.yMax, graphRect.y, (point.y + 1) * 0.5f)
                );

                Rect pointRect;
                if (selectId == i)
                {
                    pointRect = new(graphPoint.x - 10, graphPoint.y - 10, 20, 20);
                }
                else
                {
                    pointRect = new(graphPoint.x - 5, graphPoint.y - 5, 10, 10);
                }
                GUI.DrawTexture(pointRect, image, ScaleMode.ScaleToFit);
                if (Event.current.type == EventType.MouseDown && pointRect.Contains(Event.current.mousePosition))
                {
                    selectEdge = true;
                    selectId = i;
                    OnSelectEdge?.Invoke(edges[i]);
                    Repaint();
                }
            }
            if (!selectEdge && Event.current.type == EventType.MouseDown)
            {
                selectId = null;
                OnSelectEdge?.Invoke(null);
                Repaint();
            }
        }
    }
}
