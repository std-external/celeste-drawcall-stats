using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.DrawCallStats;

/// <summary>一帧的渲染侧统计。</summary>
public struct FrameStats {
    /// <summary>SpriteBatch.Draw / DrawString 提交的 sprite 数（= FNA PushSprite 调用数）。</summary>
    public int Draws;
    /// <summary>批提交次数（SpriteBatch.End 走的 FlushBatch + 2048 溢出）。</summary>
    public int Flushes;
    /// <summary>真正的 GPU 绘制命令数（GraphicsDevice.DrawIndexedPrimitives）。</summary>
    public int GpuDraws;
    /// <summary>提交过程中纹理发生变化（打断合批）的次数。</summary>
    public int TextureSwitches;
    /// <summary>本帧用到的不同纹理张数。</summary>
    public int DistinctTextures;
    /// <summary>场景实体数（Engine.Scene.Entities.Count）。</summary>
    public int Entities;
    /// <summary>本帧 GC gen0 回收次数。</summary>
    public int Gc0;
    /// <summary>帧耗时，毫秒（Engine.RawDeltaTime）。</summary>
    public float Milliseconds;

    /// <summary>每个 flush 平均装了多个 sprite；越大说明合批越有效。</summary>
    public float SpritesPerFlush => Flushes <= 0 ? Draws : (float) Draws / Flushes;
}
