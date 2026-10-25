using UnityEngine;

public sealed partial class TrainingOverlay : MonoBehaviour
{
    public TrainingFighter player;
    public TrainingDummy dummy;
    private GameObject box,secondBox;
    private TrainingSecondPlayerControls secondPlayerControls;
    public bool SecondPlayerControlled { get; private set; }
    public void SetSecondPlayerControl(bool controlled)
    {
        if(secondPlayerControls==null)return;
        secondPlayerControls.Clear(); secondPlayerControls.enabled=controlled;
        dummy.Fighter.SetInputSource(controlled?(MonoBehaviour)secondPlayerControls:dummy);
        SecondPlayerControlled=controlled;
    }
    private sealed class HurtboxOutline
    {
        public TrainingHurtbox hurtbox;
        public LineRenderer line;
        public MeshRenderer fill;
        public UnityEngine.UI.Text[] labels;
    }
    private readonly System.Collections.Generic.List<HurtboxOutline> hurtboxes = new System.Collections.Generic.List<HurtboxOutline>();
    private Material hurtboxMaterial;
    private const int CapSegments = 24;
    public void CycleFacingView()
    {
        var style = (TrainingFighter.VisualFacingStyle)(((int)player.FacingStyle + 1) % 3);
        player.SetVisualFacingStyle(style);
        if (player.opponent != null) player.opponent.SetVisualFacingStyle(style);
    }
    private void Start()
    {
        var config=Resources.Load<TrainingInputConfig>("TrainingInputConfig");
        if(config!=null&&config.inputActions!=null){secondPlayerControls=dummy.gameObject.AddComponent<TrainingSecondPlayerControls>();secondPlayerControls.enabled=false;secondPlayerControls.Configure(config);SetSecondPlayerControl(false);}
        gameObject.AddComponent<TrainingStage>();
        var boundaries = gameObject.AddComponent<TrainingCameraBounds>();
        boundaries.view = Camera.main; boundaries.first = player; boundaries.second = player.opponent;
        player.CameraBounds = boundaries;
        if (player.opponent != null) player.opponent.CameraBounds = boundaries;
        CreateHud();
        box = new GameObject("Attack hitbox (debug)");
        box.name = "Attack hitbox (debug)";
        var outline = box.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.widthMultiplier = 0.015f;
        var corners = new[] { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) };
        var order = new[] {0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4};
        outline.positionCount = order.Length;
        for (int i = 0; i < order.Length; i++) outline.SetPosition(i, corners[order[i]]);
        var material = new Material(Shader.Find("Shioka/TrainingDebugOutline"));
        material.color = new Color(1f, 0.2f, 0.1f);
        box.GetComponent<Renderer>().material = material;
        box.transform.localScale = player.HitboxHalfSize * 2f;
        if(player.opponent!=null){secondBox=Instantiate(box);secondBox.name="2P Attack hitbox (debug)";secondBox.GetComponent<Renderer>().sharedMaterial=new Material(box.GetComponent<Renderer>().sharedMaterial);}
        // ReceiveAttack is reached by OverlapBox contacting this actual collider.
        // Draw one thin side meridian rather than a filled volume over the model.
        hurtboxMaterial = new Material(Shader.Find("Shioka/TrainingDebugOutline"));
        hurtboxMaterial.color = new Color(1f, 1f, 1f, 0.7f);
        hurtboxObserverFirst=player;hurtboxObserverSecond=player.opponent;hurtboxObserverFirst.HurtboxesChanged+=RefreshHurtboxOutlines;
        if(hurtboxObserverSecond!=null)hurtboxObserverSecond.HurtboxesChanged+=RefreshHurtboxOutlines;
        AddHurtbox(player);
        if (player.opponent != null) AddHurtbox(player.opponent);
        AddThrowReceiver(player);if(player.opponent!=null)AddThrowReceiver(player.opponent);EnsureTrainingSession();CreateOption();
    }
    private TrainingFighter hurtboxObserverFirst,hurtboxObserverSecond;
    private readonly System.Collections.Generic.List<ThrowOutline> throwOutlines=new System.Collections.Generic.List<ThrowOutline>();
    private sealed class ThrowOutline{public TrainingThrowHurtbox region;public LineRenderer line;}
    private void RefreshHurtboxOutlines(){foreach(var entry in hurtboxes){DestroyDefenseDisplay(entry);if(entry.line!=null)Destroy(entry.line.gameObject);}hurtboxes.Clear();if(player!=null)AddHurtbox(player);if(player!=null&&player.opponent!=null)AddHurtbox(player.opponent);}
    private void AddHurtbox(TrainingFighter fighter)
    {
        foreach(var part in fighter.Hurtboxes)
        {
            var line=new GameObject((fighter==player?"1P":"2P")+(part.Region.shape==TrainingHurtboxShape.Box?" hurtbox "+part.StableId+" Box (debug)":(part.Region.shape==TrainingHurtboxShape.BodyWhole?" body":part.Upper?" upper":" lower")+" hurtbox (debug)")).AddComponent<LineRenderer>();
            line.useWorldSpace=true;line.widthMultiplier=.009f;line.positionCount=16;
            line.sharedMaterial=hurtboxMaterial;
            line.startColor=line.endColor=part.Upper?Color.cyan:new Color(.5f,1f,.1f);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            var item=new HurtboxOutline{hurtbox=part,line=line};line.enabled=false;CreateDefenseDisplay(item);hurtboxes.Add(item);
        }
    }
    private void UpdateHurtbox(HurtboxOutline outline)
    {
        var part=outline.hurtbox;var collider=part==null?null:part.Collider;
        bool visible=collider!=null && collider.enabled && collider.gameObject.activeInHierarchy;
        ColorComparisonHurtbox(outline);outline.line.gameObject.SetActive(visible);UpdateDefenseDisplay(outline,visible);if(!visible)return;
        if(Camera.main!=null&&Camera.main.orthographic)outline.line.widthMultiplier=Mathf.Max(.009f,2*Camera.main.orthographicSize/Mathf.Max(1,Camera.main.pixelHeight));
        var corners=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        var order=new[]{0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4};
        for(int i=0;i<order.Length;i++)outline.line.SetPosition(i,collider.transform.TransformPoint((Vector3)collider.offset+Vector3.Scale(corners[order[i]],collider.size*.5f)));
    }
    private void UpdateAttackOutline(GameObject outline,TrainingFighter fighter)
    {
        if(outline==null)return;
        outline.SetActive(fighter!=null&&fighter.HitboxActive);
        if(fighter==null)return;
        if(Camera.main!=null&&Camera.main.orthographic)outline.GetComponent<LineRenderer>().widthMultiplier=Mathf.Max(.015f,2.6f*Camera.main.orthographicSize/Mathf.Max(1,Camera.main.pixelHeight));
        outline.transform.position=fighter.HitboxCenter;outline.transform.localScale=fighter.HitboxHalfSize*2;
        outline.GetComponent<Renderer>().sharedMaterial.color=fighter.CurrentAttackClass==TrainingAttackClass.Throw?new Color(1,.65f,0):levelDisplay.HitboxColor(fighter.attackLevel*(fighter.CombatV01==null?fighter.Tension.Bonus:1));
    }
    private void AddThrowReceiver(TrainingFighter fighter){var line=new GameObject((fighter==player?"1P":"2P")+" throw receiver (debug)").AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=16;line.widthMultiplier=.009f;line.sharedMaterial=hurtboxMaterial;line.startColor=line.endColor=new Color(.3f,.6f,1f);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;throwOutlines.Add(new ThrowOutline{region=fighter.ThrowHurtbox,line=line});}
    private void UpdateThrowReceiver(ThrowOutline item){var c=item.region==null?null:item.region.Collider;bool visible=c!=null&&c.enabled&&c.gameObject.activeInHierarchy;item.line.gameObject.SetActive(visible);if(!visible)return;if(Camera.main!=null&&Camera.main.orthographic)item.line.widthMultiplier=Mathf.Max(.009f,2*Camera.main.orthographicSize/Mathf.Max(1,Camera.main.pixelHeight));var corners=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};var order=new[]{0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4};for(int i=0;i<order.Length;i++)item.line.SetPosition(i,c.transform.TransformPoint((Vector3)c.offset+Vector3.Scale(corners[order[i]],c.size*.5f)));}
    private void LateUpdate()
    {
        TickHud();
        UpdateAttackOutline(box,player);UpdateAttackOutline(secondBox,player==null?null:player.opponent);
        foreach(var outline in hurtboxes)UpdateHurtbox(outline);foreach(var item in throwOutlines)UpdateThrowReceiver(item);ResolveDefenseLabelOverlaps();
    }
    public static Rect HealthRect(bool first,float screenWidth){float width=Mathf.Min(360,Mathf.Max(120,screenWidth*.5f-40));return new Rect(first?24:screenWidth-24-width,20,width,24);}
    public static Rect TensionRect(bool first,float screenWidth){var health=HealthRect(first,screenWidth);return new Rect(first?health.x+96:health.x,52,health.width-96,18);}
    public static Rect TensionNumberRect(bool first,float screenWidth){var health=HealthRect(first,screenWidth);return new Rect(first?health.x:health.xMax-96,48,96,26);}
    private void OnDestroy()
    {
        if(hurtboxObserverFirst!=null)hurtboxObserverFirst.HurtboxesChanged-=RefreshHurtboxOutlines;if(hurtboxObserverSecond!=null)hurtboxObserverSecond.HurtboxesChanged-=RefreshHurtboxOutlines;
        DestroyOption();
        DestroyHud();
        if (box != null) { Destroy(box.GetComponent<Renderer>().sharedMaterial); Destroy(box); }
        if(secondBox!=null){Destroy(secondBox.GetComponent<Renderer>().sharedMaterial);Destroy(secondBox);}
        foreach (var outline in hurtboxes) {DestroyDefenseDisplay(outline);if (outline.line != null) Destroy(outline.line.gameObject);}
        if(defenseQuad!=null)Destroy(defenseQuad);if(defenseFont!=null)Destroy(defenseFont);
        foreach(var item in throwOutlines)if(item.line!=null)Destroy(item.line.gameObject);
        if (hurtboxMaterial != null) Destroy(hurtboxMaterial);
    }
}
