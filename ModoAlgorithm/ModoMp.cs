namespace Modo;

public static class ModoMp
{
    // ctor
    public static Res<Modo<SolnMp>> New<X>(Model model, X solver, HashSet<Var0> objectives, HashSet<IVar> varsToCache, double[] disturbanceRadius, Opt<Func<Rect, double>> getRectPriority = default, Opt<Rect> boundingRect = default)
        where X : ISolver
    {
        return
            OkIf(boundingRect.IsNone || boundingRect.Unwrap().Dim <= disturbanceRadius.Length)
            .OkIf(boundingRect.IsNone || boundingRect.Unwrap().Dim == objectives.Count - 1)
            .OkIf(objectives.Count > 1)
            .OkIf(disturbanceRadius.Length >= objectives.Count - 1)
            .Map(() =>
            {
                var objArr = objectives.ToArray();
                var varsToCacheExt = GetVarsToCache(objectives, varsToCache);
                var solveForEpsilon = GetSolveForEpsilon(model, solver, objArr, varsToCacheExt);
                var resBoundingRect = boundingRect.IsSome ? Ok(boundingRect.Unwrap()) : GetBoundingRect(model, solver, objArr.AsSpan()[..^1]);
                return resBoundingRect.Map(rect => Modo<SolnMp>.New(rect, solveForEpsilon, disturbanceRadius, getRectPriority)).Flatten();
            })
            .Flatten();
    }
    
    
    // cache
    static HashSet<IVar> GetVarsToCache(HashSet<Var0> objectives, HashSet<IVar> varsToCache)
    {
        var missing = objectives.Where(x => !varsToCache.Contains(x));
        return missing.Any() ? varsToCache.Union(missing).ToHashSet() : varsToCache;
    }
    
    
    // solve
    static Func<double, Rect, Opt<NondomSoln<SolnMp>>> GetSolveForEpsilon<X>(Model model, X solver, Var0[] objArr, HashSet<IVar> varsToCache)
        where X : ISolver
    {
        var bndKeys = Enumerable.Range(0, objArr.Length).Select(j => string.Format("__bb{0}__", j)).ToArray();
        Expr sumObj = objArr.Aggregate(Expr.Zero, (s, o) => s + o);
        Var0 dummyVar = Var0.Nonneg();
        Var0 mainObj = objArr[^1];

        return (timeLeft, rect) =>
        {
            // first stage: Pk(epsilon)
            int dim = objArr.Length - 1;
            Debug.Assert(rect.Upper.Length == dim);
            model.Obj = ObjDir.Min | objArr[dim];
            var upper = rect.Upper;

            for (int j = 0; j < dim; j++)
                model[bndKeys[j]] = objArr[j] <= (upper[j] - 1.0);
            model[bndKeys[dim]] = dummyVar >= 0.0;

            // todo: better error handling needed here!
            solver.Params.TimeLimitSec = timeLeft;
            using var soln1 = solver.Solve(model, mainObj);
            if (!soln1.IsFeasible)
                return None<NondomSoln<SolnMp>>();
            double zStar = soln1.GetVals(mainObj);


            // second stage: Qk(epsilon)
            model.Obj = ObjDir.Min | sumObj;
            model[bndKeys[dim]] = mainObj == zStar;

            using var soln2 = solver.Solve(model, varsToCache);
            if (!soln2.IsFeasible)
                return None<NondomSoln<SolnMp>>();

            // extract solution
            var objValsQ = objArr.Select(o => soln2.GetVals(o).Val).ToArray();
            return Some(new NondomSoln<SolnMp>(objValsQ, soln2));
        };
    }


    // bounding rect
    static Res<Rect> GetBoundingRect<X>(Model model, X solver, Span<Var0> subObjectives) where X : ISolver
    {
        var min = GetBoundingRectVertex(model, solver, subObjectives, ObjDir.Min);
        if (min.IsErr)
            return Err<Rect>(min.ToString());

        var max = GetBoundingRectVertex(model, solver, subObjectives, ObjDir.Max);
        if (max.IsErr)
            return Err<Rect>(max.ToString());

        return Ok(new Rect(min.Unwrap(), max.Unwrap()));
    }
    static Res<double[]> GetBoundingRectVertex<X>(Model model, X solver, Span<Var0> subObjectives, ObjDir dir) where X : ISolver
    {
        var vertex = new double[subObjectives.Length];
        for (int j = 0; j < subObjectives.Length; j++)
        {
            Var0 singleObj = subObjectives[j];
            model.Obj = dir | singleObj;
            using var soln = solver.Solve(model, singleObj);
            if (!soln.IsFeasible)
                return Err<double[]>(string.Format("Failed to compute bounding rectangle vertex for {0}-th objective: {1}", j, model.Obj));
            vertex[j] = soln.GetVals(singleObj);
        }
        return vertex;
    }
}
