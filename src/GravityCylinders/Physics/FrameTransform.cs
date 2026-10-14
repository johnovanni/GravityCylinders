namespace GravityCylinders.Physics;

/// <summary>How velocities and lengths look from an observer moving along x.</summary>
public abstract class FrameTransform
{
    public static readonly FrameTransform Galilean = new GalileanTransform();
    public static readonly FrameTransform Lorentz = new LorentzTransform();

    public abstract double VelocityInFrame(double beta, double frameBeta);

    /// <summary>Lorentz factor of a body moving at <paramref name="betaInFrame"/> (always 1 for Galilean frames).</summary>
    public abstract double Gamma(double betaInFrame);

    private sealed class GalileanTransform : FrameTransform
    {
        public override double VelocityInFrame(double beta, double frameBeta) => beta - frameBeta;
        public override double Gamma(double betaInFrame) => 1.0;
    }

    private sealed class LorentzTransform : FrameTransform
    {
        public override double VelocityInFrame(double beta, double frameBeta) => Relativity.InFrame(beta, frameBeta);
        public override double Gamma(double betaInFrame) => Phys.Gamma(betaInFrame);
    }
}
