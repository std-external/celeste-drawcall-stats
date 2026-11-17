using System.ComponentModel;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.DrawCallStats;

public sealed class DrawCallStatsSettings : EverestModuleSettings {
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DefaultButtonBinding((Buttons) 0, Keys.F8)]
    public ButtonBinding ToggleHud { get; set; }

    [DefaultButtonBinding((Buttons) 0, Keys.F9)]
    public ButtonBinding DumpLog { get; set; }

    [DefaultValue(false)]
    public bool AutoDumpLog { get; set; }

    [SettingRange(60, 3600)]
    [DefaultValue(600)]
    public int SampleFrames { get; set; } = 600;
}
