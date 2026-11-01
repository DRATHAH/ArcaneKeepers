using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play mode: loads sceneName (async, like SceneTransition), records the profiler, and lists the
// biggest items in the slowest frame (the activation hitch).
public static class ProfileSceneActivation
{
    public static async Task<string> Run(string sceneName = "LevelSelect")
    {
        ProfilerDriver.ClearAllFrames();
        ProfilerDriver.enabled = true;
        UnityEngine.Profiling.Profiler.enabled = true;
        await Awaitable.NextFrameAsync();
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) await Awaitable.NextFrameAsync();
        op.allowSceneActivation = true;
        while (!op.isDone) await Awaitable.NextFrameAsync();
        for (int i = 0; i < 4; i++) await Awaitable.NextFrameAsync();
        ProfilerDriver.enabled = false;

        int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
        int worst = -1; float worstMs = 0;
        for (int f = first; f <= last; f++)
        {
            using var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false);
            if (v == null || !v.valid) continue;
            if (v.frameTimeMs > worstMs) { worstMs = v.frameTimeMs; worst = f; }
        }
        var sb = new StringBuilder($"worst frame {worst} {worstMs:0}ms\n");
        using (var v = ProfilerDriver.GetHierarchyFrameDataView(worst, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false))
        {
            var items = new List<(string path, float ms, float self)>();
            void Walk(int id, string path, int depth)
            {
                var kids = new List<int>();
                v.GetItemChildren(id, kids);
                foreach (int k in kids)
                {
                    float ms = v.GetItemColumnDataAsFloat(k, HierarchyFrameDataView.columnTotalTime);
                    if (ms < 8) continue;
                    string p = path + "/" + v.GetItemName(k);
                    items.Add((p, ms, v.GetItemColumnDataAsFloat(k, HierarchyFrameDataView.columnSelfTime)));
                    if (depth < 9) Walk(k, p, depth + 1);
                }
            }
            Walk(v.GetRootItemID(), "", 0);
            foreach (var it in items.Take(70)) sb.Append($"{it.ms,6:0} self {it.self,5:0}  {it.path}\n");
        }
        return sb.ToString();
    }
}
