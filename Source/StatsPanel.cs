using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.DrawCallStats;

/// <summary>
/// 左上角的小面板。直接画在 backbuffer 上（<c>Engine.Draw</c> 的收尾），
/// 不挂在 Scene 的 RendererList 上：<see cref="Level"/> 和一部分 Oui 场景重写了
/// <c>Scene.Render()</c> 且不调 base，因此 RendererList 里挂的东西在关卡里根本不会被调用。
/// </summary>
public static class StatsPanel {
    private static bool visible = true;

    public static bool Visible => visible;

    /// <summary>热键：F8 切显隐，F9 导日志。用 ButtonBinding，不抢游戏输入。</summary>
    public static void HandleHotkeys() {
        DrawCallStatsSettings settings = DrawCallStatsModule.Settings;
        if (settings == null)
            return;

        if (settings.ToggleHud.Pressed) {
            visible = !visible;
            settings.ToggleHud.ConsumePress();
        }

        if (settings.DumpLog.Pressed) {
            StatsHooks.Dump();
            settings.DumpLog.ConsumePress();
        }
    }

    public static void Render() {
        if (!visible || !(DrawCallStatsModule.Settings?.Enabled ?? true))
            return;

        FrameStats f = StatsHooks.Current;
        string verdict = f.Flushes <= 0 ? "-" : f.SpritesPerFlush >= 20f ? "batch ok" : "batch poor";
        string text =
            $"draw {f.Draws}  flush {f.Flushes}  gpu {f.GpuDraws}\n" +
            $"texSwitch {f.TextureSwitches}  tex {f.DistinctTextures}\n" +
            $"ent {f.Entities}  gc0 +{f.Gc0}  {f.Milliseconds:0.0}ms\n" +
            $"{f.SpritesPerFlush:0.0} sprite/flush ({verdict})";

        GraphicsDevice device = Engine.Instance.GraphicsDevice;
        device.SetRenderTarget(null);
        device.Viewport = Engine.Viewport;

        float pixel = Engine.ViewWidth / 320f;
        float scale = pixel / 2f;
        Vector2 size = Monocle.Draw.DefaultFont.MeasureString(text) * scale;
        Vector2 position = new Vector2(pixel * 2f, pixel * 2f);

        Monocle.Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        Monocle.Draw.Rect(position - new Vector2(4f, 4f), size.X + 8f, size.Y + 8f, Color.Black * 0.7f);
        Monocle.Draw.SpriteBatch.DrawString(Monocle.Draw.DefaultFont, text, position, Color.White, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0f);
        Monocle.Draw.SpriteBatch.End();
    }
}
