namespace Modo;

public record struct NondomSoln<T>(double[] ObjVals, T SolnInfo)
{
    // helper
    internal void CopyObjValsIntoFxBar(double[] fxbar)
    {
        Debug.Assert(ObjVals.Length == fxbar.Length + 1);
        Array.Copy(ObjVals, fxbar, fxbar.Length);
    }
}
