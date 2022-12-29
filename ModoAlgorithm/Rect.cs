namespace Modo;

public readonly struct Rect : IEquatable<Rect>
{
    // data
    internal readonly double[] Lower;
    internal readonly double[] Upper;


    // ctor
    internal Rect(double[] lowerVertex, double[] upperVertex)
    {
        Debug.Assert(lowerVertex.Length == upperVertex.Length, nameof(Rect));
        for (int i = 0; i < lowerVertex.Length; i++)
            Debug.Assert(lowerVertex[i] <= upperVertex[i]);
        Lower = lowerVertex;
        Upper = upperVertex;
    }


    // method
    public int Dim
        => Lower.Length;
    public IEnumerable<(int ObjInd, double NewObjVal)> GetIntersectingObjectiveIndex(double[] newNondominatedSoln)
    {
        Debug.Assert(newNondominatedSoln.Length == Dim, nameof(GetIntersectingObjectiveIndex));
        for (int j = 0; j < newNondominatedSoln.Length; j++)
        {
            double newObjVal = newNondominatedSoln[j];
            if (Lower[j] < newObjVal && newObjVal < Upper[j])
                yield return (j, newObjVal);
        }
    }
    public (Rect LowerArr, Rect UpperArr) SplitRectangle(int objInd, double newObjVal)
    {
        Debug.Assert(objInd < Dim, nameof(SplitRectangle));

        var (lowerLower, lowerUpper) = CloneVectors();
        lowerUpper[objInd] = newObjVal; // push upper bound vertex to newObjVal on objInd-th dim

        // misuse this rectangle as the upper
        var (upperLower, upperUpper) = CloneVectors();
        upperLower[objInd] = newObjVal; // push lower vertex to newObjVal on objInd-th dim

        return (new(lowerLower, lowerUpper), new(upperLower, upperUpper));
    }
    public bool IsSubsetOf(Rect other)
    {
        Debug.Assert(other.Lower.Length == Dim, nameof(IsSubsetOf));
        for (int j = 0; j < other.Lower.Length; j++)
            if (Lower[j] < other.Lower[j])
                return false; // lower[j] < other.LowerArr[j]; this is not a subset of the other
            else if (Upper[j] > other.Upper[j])
                return false; // upper[j] > other.UpperArr[j]; this is not a subset of the other
        return true;
    }


    // helper
    (double[] lower, double[] upper) CloneVectors()
    {
        double[] lower = new double[Lower.Length], upper = new double[Upper.Length];
        Array.Copy(Lower, lower, lower.Length);
        Array.Copy(Upper, upper, upper.Length);
        return (lower, upper);
    }


    // helper - static
    internal static Func<Rect, double> GetDefaultCalcVolume(double[] boundingRectLower)
        => rect => CalcVolume(boundingRectLower, rect.Upper);
    internal static double CalcVolume(double[] lower, double[] upper)
    {
        double vol = 1;
        for (int j = 0; j < lower.Length; j++)
            vol *= (upper[j] - lower[j]);
        Debug.Assert(vol >= 0);
        return vol;
    }


    // common
    public override string ToString()
        => string.Format("{0};{1}", string.Join(',', Lower), string.Join(',', Upper));
    public static bool operator ==(Rect left, Rect right)
        => left.Lower.Length == right.Lower.Length
        && left.Lower.SequenceEqual(right.Lower)
        && left.Upper.SequenceEqual(right.Upper);
    public static bool operator !=(Rect left, Rect right)
        => left.Lower.Length != right.Lower.Length
        || !left.Lower.SequenceEqual(right.Lower)
        || !left.Upper.SequenceEqual(right.Upper);
    public bool Equals(Rect other)
        => this == other;
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (obj is Rect rect1)
            return Equals(rect1);
        return false;
    }
    public override int GetHashCode()
        => HashCode.Combine(
            ((IStructuralEquatable)Lower).GetHashCode(EqualityComparer<double>.Default),
            ((IStructuralEquatable)Upper).GetHashCode(EqualityComparer<double>.Default));
}
