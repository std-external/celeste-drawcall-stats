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
        On.Monocle.Scene.Begin += OnSceneBegin;
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= OnRenderCore;
        On.Monocle.Scene.Begin -= OnSceneBegin;
        StatsHooks.Unload();
        Instance = null;
    }

    private static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        StatsHooks.BeginFrame();
        orig(self);
    }

    private static void OnSceneBegin(On.Monocle.Scene.orig_Begin orig, Scene self) {
        orig(self);
        self.Add(new StatsHud());
    }
}
