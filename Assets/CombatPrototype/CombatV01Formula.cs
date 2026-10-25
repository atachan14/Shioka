using System;
public enum CombatV01Outcome { Guard, Block, Hit }
public struct CombatV01Result {
 public double A,D,H,B,attackTG,defenseTG,jgReduction,rawStun,surplus,damage,selfPush,attackerPush;
 public long jgAge;public int hitstopFrames,stunFrames;public bool fd;public CombatV01Outcome outcome;
}
// Pure contact math. HP remains fractional; only final durations are rounded.
public static class CombatV01Formula {
 public static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
 static double Nonnegative(double n){if(!Finite(n))throw new ArgumentOutOfRangeException(nameof(n),"Finite values required");return Math.Max(0,n);}
 public static double TGMultiplier(double tg){if(!Finite(tg))throw new ArgumentOutOfRangeException(nameof(tg));return tg>=0?1+tg/200:1/(1+Math.Abs(tg)/100);}
 public static double HitLevel(double a,double d,double attackTG,double defenseTG)=>Nonnegative(a)*TGMultiplier(attackTG)/TGMultiplier(defenseTG)/(1+Nonnegative(d)/100);
 public static int Frames(double duration){duration=Nonnegative(duration);if(duration>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(duration));return (int)Math.Ceiling(duration);}
 public static int JGReduction(long age,bool continuous){int window=continuous?20:30;return age<0||age>=window?0:10-(int)(age/(continuous?2:3));}
 public static CombatV01Result Resolve(double a,double d,double attackTG,double defenseTG,bool matchingGuard,bool fd,int jg,double damageMultiplier=1,double chipMultiplier=.1,double pushScale=.001){
  a=Nonnegative(a);d=Nonnegative(d);if(!Finite(attackTG)||!Finite(defenseTG))throw new ArgumentOutOfRangeException("TG");damageMultiplier=Nonnegative(damageMultiplier);chipMultiplier=Nonnegative(chipMultiplier);pushScale=Nonnegative(pushScale);if(chipMultiplier>1)throw new ArgumentOutOfRangeException(nameof(chipMultiplier));
  var r=new CombatV01Result{A=a,D=d,B=d,H=HitLevel(a,d,attackTG,defenseTG),attackTG=attackTG,defenseTG=defenseTG};r.outcome=matchingGuard?CombatV01Outcome.Guard:r.H<=r.B?CombatV01Outcome.Block:CombatV01Outcome.Hit;r.fd=matchingGuard&&fd;r.jgReduction=matchingGuard?Math.Max(0,Math.Min(10,jg)):0;r.rawStun=r.H/10-r.jgReduction;r.surplus=Math.Max(0,-r.rawStun);r.stunFrames=Frames(Math.Max(0,r.rawStun));r.hitstopFrames=Frames(a*TGMultiplier(attackTG)/TGMultiplier(defenseTG)/40);r.damage=r.fd?0:r.H*damageMultiplier*(r.outcome==CombatV01Outcome.Hit?1:chipMultiplier);double k=r.H*pushScale,j=r.jgReduction/10;
  r.selfPush=r.outcome==CombatV01Outcome.Hit?1.2*k:r.outcome==CombatV01Outcome.Block?k/3:r.fd?k:k*(1-.4*j);r.attackerPush=r.fd?k*.2*(1+1.5*j):0;return r;
 }
 public static double PushStep(double distance,int completed,int total=10){if(completed<0||completed>=total||total<1)return 0;double a=(double)completed/total,b=(double)(completed+1)/total;return Nonnegative(distance)*((2*b-b*b)-(2*a-a*a));}
}
