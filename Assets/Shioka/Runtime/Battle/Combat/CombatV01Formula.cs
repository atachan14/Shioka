using System;
public enum CombatV01Outcome { Guard, Block, Hit }
public struct CombatV01Result {
 public double A,D,H,B,attackTG,defenseTG,jgReduction,rawStun,surplus,damage,selfPush,attackerPush;
 public long jgAge;public int hitstopFrames,stunFrames;public bool fd;public CombatV01Outcome outcome;
}
// Pure contact math. HP remains fractional; only final durations are rounded.
public static class CombatV01Formula {
 static readonly GuardSystemValues defaultGuard=new GuardSystemValues();static readonly TGSystemValues defaultTG=new TGSystemValues();static readonly CombatFormulaValues defaultCombat=new CombatFormulaValues();
 static GuardSystemValues G(CombatV01Profile t)=>t!=null&&t.guard!=null&&t.guard.Valid?t.guard.values:defaultGuard;
 static TGSystemValues T(CombatV01Profile t)=>t!=null&&t.tg!=null&&t.tg.Valid?t.tg.values:defaultTG;
 static CombatFormulaValues C(CombatV01Profile t)=>t!=null&&t.formula!=null&&t.formula.Valid?t.formula.values:defaultCombat;
 public static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
 static double Nonnegative(double n){if(!Finite(n))throw new ArgumentOutOfRangeException(nameof(n),"Finite values required");return Math.Max(0,n);}
 public static double TGMultiplier(double tg,CombatV01Profile tuning=null){if(!Finite(tg))throw new ArgumentOutOfRangeException(nameof(tg));return tg>=0?1+tg/T(tuning).positiveDivisor:1/(1+Math.Abs(tg)/T(tuning).negativeDivisor);}
 public static double HitLevel(double a,double d,double attackTG,double defenseTG,CombatV01Profile tuning=null)=>Nonnegative(a)*TGMultiplier(attackTG,tuning)/TGMultiplier(defenseTG,tuning)/(1+Nonnegative(d)/C(tuning).defenseDivisor);
 public static int Frames(double duration){duration=Nonnegative(duration);if(duration>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(duration));return (int)Math.Ceiling(duration);}
 public static int JGReduction(long age,bool continuous,CombatV01Profile tuning=null){var g=G(tuning);int window=continuous?g.continuousWindow:g.normalWindow;return age<0||age>=window?0:Math.Max(0,g.maxReduction-(int)(age/(continuous?g.continuousDecayFrames:g.normalDecayFrames)));}
 public static CombatV01Result Resolve(double a,double d,double attackTG,double defenseTG,bool matchingGuard,bool fd,int jg,double damageMultiplier=1,double chipMultiplier=.1,double pushScale=.001,CombatV01Profile tuning=null){
  a=Nonnegative(a);d=Nonnegative(d);if(!Finite(attackTG)||!Finite(defenseTG))throw new ArgumentOutOfRangeException("TG");damageMultiplier=Nonnegative(damageMultiplier);chipMultiplier=Nonnegative(chipMultiplier);pushScale=Nonnegative(pushScale);if(chipMultiplier>1)throw new ArgumentOutOfRangeException(nameof(chipMultiplier));
  var r=new CombatV01Result{A=a,D=d,B=d,H=HitLevel(a,d,attackTG,defenseTG,tuning),attackTG=attackTG,defenseTG=defenseTG};r.outcome=matchingGuard?CombatV01Outcome.Guard:r.H<=r.B?CombatV01Outcome.Block:CombatV01Outcome.Hit;r.fd=matchingGuard&&fd;r.jgReduction=matchingGuard?Math.Max(0,Math.Min(G(tuning).maxReduction,jg)):0;r.rawStun=r.H/C(tuning).stunDivisor-r.jgReduction;r.surplus=Math.Max(0,-r.rawStun);r.stunFrames=Frames(Math.Max(0,r.rawStun));r.hitstopFrames=Frames(a*TGMultiplier(attackTG,tuning)/TGMultiplier(defenseTG,tuning)/C(tuning).hitstopDivisor);r.damage=r.fd?0:r.H*damageMultiplier*(r.outcome==CombatV01Outcome.Hit?1:chipMultiplier);double k=r.H*pushScale,j=r.jgReduction/Math.Max(1,G(tuning).maxReduction);
  r.selfPush=r.outcome==CombatV01Outcome.Hit?C(tuning).hitPushRatio*k:r.outcome==CombatV01Outcome.Block?k/C(tuning).blockPushDivisor:r.fd?k:k*(1-G(tuning).jgSelfPushReduction*j);r.attackerPush=r.fd?k*G(tuning).fdAttackerPush*(1+G(tuning).fdJGBonus*j):0;return r;
 }
 public static double PushStep(double distance,int completed,int total=10){if(completed<0||completed>=total||total<1)return 0;double a=(double)completed/total,b=(double)(completed+1)/total;return Nonnegative(distance)*((2*b-b*b)-(2*a-a*a));}
}
