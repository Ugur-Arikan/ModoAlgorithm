namespace Modo;

internal static class Alg
{
    // alg
    internal static Res<IEnumerable<NondomSoln<S>>> CreateNondominatedSet<S>(Modo<S> input, ParamsModo parameters)
    {
        var resPar = parameters.Validate();
        if (resPar.IsErr)
            return Err<IEnumerable<NondomSoln<S>>>(resPar.ToString(), "validate-params");

        (
            Rect boundingRect,
            Func<double, Rect, Opt<NondomSoln<S>>> solveForEpsilon,
            double[] disturbanceRadius,
            Opt<Func<Rect, double>> optGetRectPriority
        ) = input;
        var getRectPriority = optGetRectPriority.UnwrapOr(() => Rect.GetDefaultCalcVolume(boundingRect.Lower));

        var fxbar = new double[boundingRect.Dim];
        List<Rect> T = new();
        List<Rect> Tpr = new();

        var yN = new Dictionary<int, NondomSoln<S>>();
        var L = new List<(Rect Rect, double Prio)> { (boundingRect, 0) };

        int limSoln = parameters.MaxNbSolutions;
        double limTime = parameters.TimeLimitSec;
        double remainingTime = limTime;
        var timer = Stopwatch.StartNew();

        while (L.Count > 0 && remainingTime > 0 && yN.Count < limSoln)
        {
            var first = L.MaxBy(x => x.Prio);
            var Li = first.Rect;
            var ui = Li.Upper;
            var maybeSoln = solveForEpsilon(remainingTime, Li);
            if (maybeSoln.IsNone) // infeasible
                RemoveRect(L, new(boundingRect.Lower, ui), disturbanceRadius);
            else
            {
                var soln = maybeSoln.Unwrap();
                soln.CopyObjValsIntoFxBar(fxbar);
                int key = HashArray(fxbar);

                if (yN.ContainsKey(key)) // already found solution
                    RemoveRect(L, new(fxbar, ui), disturbanceRadius);
                else // new nondominated solution is found
                {
                    yN.Add(key, soln);
                    L = UpdateList(getRectPriority, T, Tpr, L, fxbar);
                    RemoveRect(L, new(fxbar, ui), disturbanceRadius);
                }
            }

            remainingTime = limTime - timer.ElapsedMilliseconds / 1000.0;
        }
        return yN.Values;
    }


    // helpers
    static List<(Rect Rect, double Prio)> UpdateList(
                                Func<Rect, double> GetRectPriority,
                                List<Rect> T, List<Rect> Tpr,
                                List<(Rect Rect, double Prio)> Lold,
                                double[] fx)
    {
        var L = new List<(Rect Rect, double Prio)>();
        foreach (var (Ri, _) in Lold)
        {
            T.Clear();
            T.Add(Ri);
            foreach (var (objInd, newObjVal) in Ri.GetIntersectingObjectiveIndex(fx))
            {
                Tpr = new List<Rect>();
                foreach (var Rt in T)
                {
                    var (lower, upper) = Rt.SplitRectangle(objInd, newObjVal);
                    Tpr.Add(lower);
                    Tpr.Add(upper);
                }
                T = Tpr;
            }
            L.AddRange(T.Select(x => (x, GetRectPriority(x))));
        }
        return L;
    }
    static void RemoveRect(List<(Rect Rect, double Prio)> L, Rect other, double[] disturbanceRadius)
    {
        for (int i = 0; i < L.Count; i++)
            if (L[i].Rect.IsSubsetOf(other))
            {
                var removedRect = L[i].Rect;
                L.RemoveAt(i);
                int decrement = 1;

                for (int i2 = 0; i2 < L.Count; i2++)
                {
                    // exclude potentially degenerate point
                    if (i != i2 && L[i2].Rect.Upper.SequenceEqual(removedRect.Lower))
                    {
                        for (int j = 0; j < L[i2].Rect.Upper.Length; j++)
                            L[i2].Rect.Upper[j] = L[i2].Rect.Upper[j] - disturbanceRadius[j];
                        for (int j = 0; j < L[i2].Rect.Upper.Length; j++)
                            if (L[i2].Rect.Upper[j] < L[i2].Rect.Lower[j])
                            {
                                if (i2 < i)
                                    decrement++;
                                L.RemoveAt(i2);
                                i2--;
                                break;
                            }
                    }
                }
                i -= decrement;
            }
    }
    static int HashArray(double[] arr)
        => ((IStructuralEquatable)arr).GetHashCode(EqualityComparer<double>.Default);
}
