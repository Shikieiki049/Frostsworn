namespace Frostsworn;

// Horizontal glass thermometer, drawn with native UI shapes so it stays sharp at any scale.
public static class FrostThermometer
{
    private static StyleBoxFlat Rounded(Color fill,Color edge,int radius,int border=1)=>new() {
        BgColor=fill,BorderColor=edge,BorderWidthLeft=border,BorderWidthRight=border,
        BorderWidthTop=border,BorderWidthBottom=border,CornerRadiusTopLeft=radius,
        CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius };

    public static ProgressBar Create(Control row)
    {
        var bar=new ProgressBar {Name="FrostswornFrostBar",Position=new Vector2(18,10),
            Size=new Vector2(Math.Max(46,row.Size.X-94),12),ShowPercentage=false,MouseFilter=Control.MouseFilterEnum.Ignore};
        bar.AddThemeFontSizeOverride("font_size",1);
        bar.AddThemeStyleboxOverride("background",Rounded(new Color("102333"),new Color("69899f"),6));
        bar.AddThemeStyleboxOverride("fill",Rounded(new Color("50768d"),Colors.Transparent,5,0));
        row.AddChild(bar);
        bar.Size=new Vector2(Math.Max(46,row.Size.X-94),12);
        var ticks=new Control {Name="ThermometerTicks",MouseFilter=Control.MouseFilterEnum.Ignore};bar.AddChild(ticks);
        for(int i=0;i<9;i++)ticks.AddChild(new ColorRect {Name="Tick"+i,MouseFilter=Control.MouseFilterEnum.Ignore});
        var bulb=new Panel {Name="ThermometerBulb",Position=new Vector2(0,4),Size=new Vector2(24,24),MouseFilter=Control.MouseFilterEnum.Ignore};
        bulb.AddThemeStyleboxOverride("panel",Rounded(new Color("102333"),new Color("69899f"),12,2));row.AddChild(bulb);
        var core=new Panel {Name="Core",Position=new Vector2(5,5),Size=new Vector2(14,14),MouseFilter=Control.MouseFilterEnum.Ignore};
        core.AddThemeStyleboxOverride("panel",Rounded(new Color("50768d"),Colors.Transparent,7,0));bulb.AddChild(core);
        var glint=new ColorRect {Position=new Vector2(5,4),Size=new Vector2(4,2),Color=new Color(1,1,1,0.65f),MouseFilter=Control.MouseFilterEnum.Ignore};core.AddChild(glint);
        var frost=new Node2D {Name="ThermometerFrost",Position=new Vector2(12,16)};row.AddChild(frost);
        // Three crossed crystal axes with short branches, appearing as the freezing point approaches.
        for(int i=0;i<3;i++)
        {
            var direction=Vector2.Right.Rotated(i*Mathf.Pi/3);
            frost.AddChild(new Line2D {Points=[-direction*9,direction*9],Width=1.3f,Antialiased=true});
            foreach(float sign in new[]{-1f,1f})
                foreach(float angle in new[]{-0.65f,0.65f})
                    frost.AddChild(new Line2D {Points=[direction*sign*6,direction*sign*6-direction.Rotated(angle)*sign*3],Width=1.1f,Antialiased=true});
        }
        Refresh(bar);return bar;
    }

    public static Color ColdColor(float ratio)
    {
        ratio=Math.Clamp(ratio,0,1);
        return ratio<0.65f ? new Color("50768d").Lerp(new Color("39baf5"),ratio/0.65f)
            : new Color("39baf5").Lerp(new Color("dcfbff"),(ratio-0.65f)/0.35f);
    }

    public static void Refresh(ProgressBar bar)
    {
        var ratio=(float)Math.Clamp(bar.MaxValue<=0?0:bar.Value/bar.MaxValue,0,1);
        var color=ColdColor(ratio);
        var fill=(StyleBoxFlat)bar.GetThemeStylebox("fill");fill.BgColor=color;
        var row=bar.GetParent<Control>();
        var bulb=row.GetNode<Panel>("ThermometerBulb");
        ((StyleBoxFlat)bulb.GetNode<Panel>("Core").GetThemeStylebox("panel")).BgColor=color;
        var shell=(StyleBoxFlat)bulb.GetThemeStylebox("panel");
        shell.BorderColor=new Color("69899f").Lerp(new Color("d9faff"),ratio);
        shell.ShadowColor=new Color(0.2f,0.75f,1f,Math.Max(0,ratio-0.5f)*0.7f);
        shell.ShadowSize=ratio>=0.75f?4:0;
        var ticks=bar.GetNode<Control>("ThermometerTicks");
        for(int i=0;i<9;i++)
        {
            var tick=ticks.GetChild<ColorRect>(i);float fraction=(i+1)/10f;
            tick.Position=new Vector2(3+(bar.Size.X-6)*fraction,2);
            tick.Size=new Vector2(1,i%2==0?3:5);
            tick.Color=fraction<=ratio?new Color(0.04f,0.18f,0.27f,0.65f):new Color(0.6f,0.8f,0.9f,0.45f);
        }
        var frost=row.GetNode<Node2D>("ThermometerFrost");
        frost.Visible=ratio>=0.75f;
        frost.Modulate=new Color(0.85f,0.98f,1f,Math.Clamp((ratio-0.65f)/0.35f,0,1));
    }
}
