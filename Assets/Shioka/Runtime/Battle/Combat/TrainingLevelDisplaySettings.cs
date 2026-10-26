using UnityEngine;

// Presentation only. Breakpoints are fixed; colors/opacity can be tuned in the overlay Inspector.
[System.Serializable]
public sealed class TrainingLevelDisplaySettings
{
    public Color minus100 = new Color(.68f,.60f,.38f,.85f);
    public Color zero = new Color(.76f,.94f,.32f,50f/255f);
    public Color hundred = new Color(.52f,.42f,.96f,.38f);
    public Color twoHundred = new Color(.45f,.44f,.60f,.85f);
    [Range(.5f,1f)] public float hitboxAlpha = 1f;
    [Range(12,24)] public int labelFontSize = 14;
    public Color LevelColor(float level)
    {
        if(level<=-100)return minus100;
        if(level<0)return Color.Lerp(minus100,zero,(level+100)/100);
        if(level<100)return Color.Lerp(zero,hundred,level/100);
        if(level<200)return Color.Lerp(hundred,twoHundred,(level-100)/100);
        return twoHundred;
    }
    public Color TextColor(float level){var c=LevelColor(level);c.a=1;return c;}
    public Color HitboxColor(float level){var c=LevelColor(level);c.a=hitboxAlpha;return c;}
    public Color VerticalColor(float y,float mid,float high,float low)
    {
        if(y>=2f/3f)return Color.Lerp(LevelColor(high),LevelColor(mid),Mathf.SmoothStep(0,1,(y-2f/3f)*3));
        if(y>=1f/3f)return LevelColor(high);
        return Color.Lerp(LevelColor(low),LevelColor(high),Mathf.SmoothStep(0,1,y*3));
    }
}
