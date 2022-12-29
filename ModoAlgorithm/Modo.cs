namespace Modo;

public class Modo<T>
{
    // data
    public readonly Rect BoundingRect;
    public readonly Func<double, Rect, Opt<NondomSoln<T>>> SolveForEpsilon;
    readonly double[] DisturbanceRadiusArr;
    public readonly Opt<Func<Rect, double>> GetRectPriority = default;


    // prop
    public ReadOnlySpan<double> DisturbanceRadius
        => DisturbanceRadiusArr;


    // ctor
    Modo(Rect boundingRect, Func<double, Rect, Opt<NondomSoln<T>>> solveForEpsilon, double[] disturbanceRadius, Opt<Func<Rect, double>> getRectPriority)
    {
        BoundingRect = boundingRect;
        SolveForEpsilon = solveForEpsilon;
        DisturbanceRadiusArr = disturbanceRadius;
        GetRectPriority = getRectPriority;
    }
    public static Res<Modo<T>> New(
        Rect boundingRect,
        Func<double, Rect, Opt<NondomSoln<T>>> solveForEpsilon,
        double[] disturbanceRadius,
        Opt<Func<Rect, double>> getRectPriority = default)
    {
        return OkIf(disturbanceRadius.Length >= boundingRect.Dim)
            .Map(() => new Modo<T>(boundingRect, solveForEpsilon, disturbanceRadius, getRectPriority));
    }
    
    
    internal void Deconstruct(out Rect boundingRect, out Func<double, Rect, Opt<NondomSoln<T>>> solveForEpsilon, out double[] disturbanceRadius, out Opt<Func<Rect, double>> optGetRectPriority)
    {
        boundingRect = BoundingRect;
        solveForEpsilon = SolveForEpsilon;
        disturbanceRadius = DisturbanceRadiusArr;
        optGetRectPriority = GetRectPriority;
    }


    // method
    public Res<IEnumerable<NondomSoln<T>>> CreateNondominatedSet(ParamsModo parameters)
        => Alg.CreateNondominatedSet(this, parameters);
}
