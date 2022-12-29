namespace Modo;

public record struct ParamsModo(
    double TimeLimitSec = double.MaxValue,
    int MaxNbSolutions = int.MaxValue
    )
{
    public Res Validate()
    {
        return
            OkIf(TimeLimitSec >= 0.0)
            .OkIf(MaxNbSolutions >= 0.0);
    }
}
