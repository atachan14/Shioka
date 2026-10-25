using System;
public sealed class TrainingTension
{
    public bool SuppressGains;public bool Fixed;
    public double Gauge { get; private set; }
    public double Level { get; private set; }
    public double LastCost { get; private set; }
    public float Bonus => (float)(Gauge >= 0 ? 2.0 - 100.0 / (Gauge + 100.0) : 100.0 / (Math.Abs(Gauge) + 100.0));
    // Gain-level growth is provisional and configured on the move set.
    public void Gain(double amount, float levelRate)
    {
        if(Fixed||SuppressGains)return;
        Gauge += amount * (1.0 + Level / (Level + 100.0));
        Level += Math.Abs(amount) * levelRate;
    }
    public void Spend(double amount, float levelRate)
    {
        if(Fixed){LastCost=0;return;}LastCost = Math.Max(0, amount); Gauge -= LastCost; Level += LastCost * levelRate;
    }
    public void SetTestGauge(double value){if(double.IsNaN(value)||double.IsInfinity(value))throw new ArgumentOutOfRangeException(nameof(value));if(!Fixed)Gauge=value;}
    public void SetTrainingGauge(double value){if(double.IsNaN(value)||double.IsInfinity(value))throw new ArgumentOutOfRangeException(nameof(value));Gauge=value;LastCost=0;}
    public void Reset() { if(Fixed)return;Gauge = Level = LastCost = 0; }
}
