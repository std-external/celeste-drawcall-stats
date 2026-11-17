using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.DrawCallStats;

/// <summary>左上角的小面板。挂在每个 Scene 的 RendererList 上（Monocle 没有 Debug 组件，Renderer 就是等价物）。</summary>
public sealed class StatsHud : Renderer {
    private bool visible = true;

    public override void Update(Scene scene) {
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

    public override void Render(Scene scene) {
        // 先把本帧的数定格，再画面板：面板自己那几笔不算进显示值
        StatsHooks.Snapshot();

        if (!visible || !(DrawCallStatsModule.Settings?.Enabled ?? true))
            return;

        FrameStats f = StatsHooks.Current;
        string verdict = f.Flushes <= 0 ? "-" : f.SpritesPerFlush >= 20f ? "batch ok" : "batch poor";
        string text =
            $"draw {f.Draws}  flush {f.Flushes}  gpu {f.GpuDraws}\n" +
            $"texSwitch {f.TextureSwitches}  tex {f.DistinctTextures}\n" +
            $"ent {f.Entities}  gc0 +{f.Gc0}  {f.Milliseconds:0.0}ms\n" +
            $"{f.SpritesPerFlush:0.0} sprite/flush ({verdict})";

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        Draw.SpriteBatch.DrawString(Draw.DefaultFont, text, new Vector2(8f, 8f), Color.White, 0f,
            Vector2.Zero, 2f, SpriteEffects.None, 0f);
        Draw.SpriteBatch.End();
    }
}
