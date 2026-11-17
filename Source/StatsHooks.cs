using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Monocle;
using Logger = Celeste.Mod.Logger;

namespace Celeste.Mod.DrawCallStats;

/// <summary>
/// 计数用的 detour。Celeste 跑在 FNA 上，FNA 没有被 HookGen 处理过（没有 On.Microsoft.Xna.*），
/// 所以这三个用 MonoMod.RuntimeDetour 直接挂，并给每个 detour 写明 id，方便和别的 mod 冲突时定位。
/// </summary>
public static class StatsHooks {
    private const string LogTag = "DrawCallStats";

    // FNA SpriteBatch.PushSprite 的签名（含私有 texture 参数），sprite 提交的唯一漏斗
    private delegate void PushSpriteOrig(SpriteBatch self, Texture2D texture,
        float sourceX, float sourceY, float sourceW, float sourceH,
        float destinationX, float destinationY, float destinationW, float destinationH,
        Color color, float originX, float originY, float rotationSin, float rotationCos,
        float depth, byte effects);

    private delegate void PushSpriteHook(PushSpriteOrig orig, SpriteBatch self, Texture2D texture,
        float sourceX, float sourceY, float sourceW, float sourceH,
        float destinationX, float destinationY, float destinationW, float destinationH,
        Color color, float originX, float originY, float rotationSin, float rotationCos,
        float depth, byte effects);

    private delegate void FlushBatchOrig(SpriteBatch self);

    private delegate void FlushBatchHook(FlushBatchOrig orig, SpriteBatch self);

    private delegate void DrawIndexedOrig(GraphicsDevice self, PrimitiveType primitiveType, int baseVertex,
        int minVertexIndex, int numVertices, int startIndex, int primitiveCount);

    private delegate void DrawIndexedHook(DrawIndexedOrig orig, GraphicsDevice self, PrimitiveType primitiveType,
        int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount);

    private static Hook pushHook;
    private static Hook flushHook;
    private static Hook gpuHook;

    private static readonly HashSet<Texture2D> textures = new();
    private static Texture2D lastTexture;

    private static int draws;
    private static int flushes;
    private static int gpuDraws;
    private static int textureSwitches;
    private static int gc0AtFrameStart;

    private static readonly Queue<FrameStats> recent = new();
    private static FrameStats current;
    private static long frameIndex;

    public static FrameStats Current => current;
    public static long FrameIndex => frameIndex;

    public static void Load() {
        MethodInfo push = typeof(SpriteBatch).GetMethod("PushSprite", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("FNA SpriteBatch.PushSprite");
        MethodInfo flush = typeof(SpriteBatch).GetMethod("FlushBatch", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("FNA SpriteBatch.FlushBatch");
        MethodInfo gpu = typeof(GraphicsDevice).GetMethod("DrawIndexedPrimitives",
            new[] { typeof(PrimitiveType), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) })
            ?? throw new MissingMethodException("FNA GraphicsDevice.DrawIndexedPrimitives");

        pushHook = new Hook(push, ((PushSpriteHook) OnPushSprite).Method, new DetourConfig("DrawCallStats/PushSprite"));
        flushHook = new Hook(flush, ((FlushBatchHook) OnFlushBatch).Method, new DetourConfig("DrawCallStats/FlushBatch"));
        gpuHook = new Hook(gpu, ((DrawIndexedHook) OnDrawIndexed).Method, new DetourConfig("DrawCallStats/DrawIndexedPrimitives"));

        Logger.Log(LogLevel.Info, LogTag, "hooks installed (PushSprite / FlushBatch / DrawIndexedPrimitives)");
    }

    public static void Unload() {
        pushHook?.Dispose();
        flushHook?.Dispose();
        gpuHook?.Dispose();
        pushHook = flushHook = gpuHook = null;
    }

    /// <summary>每帧渲染开始时清零。</summary>
    public static void BeginFrame() {
        draws = flushes = gpuDraws = textureSwitches = 0;
        textures.Clear();
        lastTexture = null;
        gc0AtFrameStart = GC.CollectionCount(0);
    }

    /// <summary>在画自己的面板之前把这一帧的数存下来。</summary>
    public static void Snapshot() {
        current = new FrameStats {
            Draws = draws,
            Flushes = flushes,
            GpuDraws = gpuDraws,
            TextureSwitches = textureSwitches,
            DistinctTextures = textures.Count,
            Entities = Engine.Scene?.Entities?.Count ?? 0,
            Gc0 = GC.CollectionCount(0) - gc0AtFrameStart,
            Milliseconds = Engine.RawDeltaTime * 1000f
        };
        frameIndex++;

        int cap = Math.Max(60, DrawCallStatsModule.Settings?.SampleFrames ?? 600);
        recent.Enqueue(current);
        while (recent.Count > cap)
            recent.Dequeue();

        if ((DrawCallStatsModule.Settings?.AutoDumpLog ?? false) && recent.Count >= cap && frameIndex % cap == 0)
            Dump();
    }

    /// <summary>把缓冲区里的统计写成 log.txt 行。</summary>
    public static void Dump() {
        if (recent.Count == 0) {
            Logger.Log(LogLevel.Info, LogTag, "no samples yet");
            return;
        }

        Logger.Log(LogLevel.Info, LogTag,
            $"dump: {recent.Count} frames, columns = idx;ms;draw;flush;gpuDraw;texSwitch;tex;entity;gc0");

        int i = 0;
        foreach (FrameStats f in recent) {
            Logger.Log(LogLevel.Info, LogTag,
                $"f{i++};{f.Milliseconds:0.00};{f.Draws};{f.Flushes};{f.GpuDraws};{f.TextureSwitches};{f.DistinctTextures};{f.Entities};{f.Gc0}");
        }

        Logger.Log(LogLevel.Info, LogTag, "dump end");
    }

    private static void OnPushSprite(PushSpriteOrig orig, SpriteBatch self, Texture2D texture,
        float sourceX, float sourceY, float sourceW, float sourceH,
        float destinationX, float destinationY, float destinationW, float destinationH,
        Color color, float originX, float originY, float rotationSin, float rotationCos,
        float depth, byte effects) {
        draws++;
        if (!ReferenceEquals(texture, lastTexture)) {
            if (lastTexture != null)
                textureSwitches++;
            lastTexture = texture;
            if (texture != null)
                textures.Add(texture);
        }

        orig(self, texture, sourceX, sourceY, sourceW, sourceH, destinationX, destinationY, destinationW,
            destinationH, color, originX, originY, rotationSin, rotationCos, depth, effects);
    }

    private static void OnFlushBatch(FlushBatchOrig orig, SpriteBatch self) {
        flushes++;
        orig(self);
    }

    private static void OnDrawIndexed(DrawIndexedOrig orig, GraphicsDevice self, PrimitiveType primitiveType,
        int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount) {
        gpuDraws++;
        orig(self, primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);
    }
}
