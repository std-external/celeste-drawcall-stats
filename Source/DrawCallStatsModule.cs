using System;
using Monocle;

namespace Celeste.Mod.DrawCallStats;

public sealed class DrawCallStatsModule : EverestModule {
    public static DrawCallStatsModule Instance { get; private set; }

    public static DrawCallStatsSettings Settings => (DrawCallStatsSettings) Instance?._Settings;

    public override Type SettingsType => typeof(DrawCallStatsSettings);

    public override void Load() {
        Instance = this;

        StatsHooks.Load();
        On.Monocle.Engine.RenderCore += OnRenderCore;
        On.Monocle.Engine.Draw += OnEngineDraw;
        On.Monocle.Engine.Update += OnEngineUpdate;
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= OnRenderCore;
        On.Monocle.Engine.Draw -= OnEngineDraw;
        On.Monocle.Engine.Update -= OnEngineUpdate;
        StatsHooks.Unload();
        Instance = null;
    }

    // 每帧渲染开始清零
    private static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        StatsHooks.BeginFrame();
        orig(self);
    }

    // 引擎一帧的 Draw 收尾：在 backbuffer 上定格统计并画面板，任何 Scene 都会走到这里
    private static void OnEngineDraw(On.Monocle.Engine.orig_Draw orig, Engine self, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(self, gameTime);
        StatsHooks.Snapshot();
        StatsPanel.Render();
    }

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine self, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(self, gameTime);
        StatsPanel.HandleHotkeys();
    }
}
