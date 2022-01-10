namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// This Provider holds the rules currently not documented by Microsoft.
/// </summary>
public class MicrosoftCompilerErrorsProviderUndocumented : AnalyzerProviderBase
{
    public MicrosoftCompilerErrorsProviderUndocumented(ILogger logger, bool logWithAnsiConsoleMarkup = false)
        : base(logger, logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Microsoft.CompilerErrors.Undocumented";

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected override async Task<AnalyzerProviderBaseRuleData> ReCollect(
        AnalyzerProviderBaseRuleData data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var list = new List<Tuple<string, string>>
        {
            Tuple.Create("CS1998", "Async method lacks 'await' operators and will run synchronously."),
            Tuple.Create("CS8034", "Unable to load Analyzer assembly."),
            Tuple.Create("CS8073", "The result of the expression is always the same since a value of this type is never equal to ‘null’."),
            Tuple.Create("CS8597", "Thrown value may be null."),
            Tuple.Create("CS8600", "Converting null literal or possible null value to non - nullable type."),
            Tuple.Create("CS8601", "Possible null reference assignment."),
            Tuple.Create("CS8602", "Dereference of a possibly null reference."),
            Tuple.Create("CS8603", "Possible null reference return."),
            Tuple.Create("CS8604", "Possible null reference argument."),
            Tuple.Create("CS8605", "Unboxing a possibly null value."),
            Tuple.Create("CS8606", "Possible null reference assignment to iteration variable"),
            Tuple.Create("CS8607", "A possible null value may not be passed to a target marked with the[DisallowNull] attribute"),
            Tuple.Create("CS8608", "Nullability of reference types in type doesn’t match overridden member."),
            Tuple.Create("CS8609", "Nullability of reference types in return type doesn’t match overridden member."),
            Tuple.Create("CS8610", "Nullability of reference types in type of parameter doesn't match overridden member."),
            Tuple.Create("CS8611", "Nullability of reference types in type of parameter doesn't match partial method declaration."),
            Tuple.Create("CS8612", "Nullability of reference types in type doesn’t match implicitly implemented member."),
            Tuple.Create("CS8613", "Nullability of reference types in return type doesn't match implicitly implemented member."),
            Tuple.Create("CS8614", "Nullability of reference types in type of parameter doesn’t match implicitly implemented member."),
            Tuple.Create("CS8615", "Nullability of reference types in type doesn’t match implemented member."),
            Tuple.Create("CS8616", "Nullability of reference types in return type doesn’t match implemented member."),
            Tuple.Create("CS8617", "Nullability of reference types in type of parameter doesn’t match implemented member."),
            Tuple.Create("CS8618", "Non-nullable field is uninitialized. Consider declaring as nullable."),
            Tuple.Create("CS8619", "Nullability of reference types in value doesn't match target type."),
            Tuple.Create("CS8620", "Argument cannot be used for parameter due to differences in the nullability of reference types."),
            Tuple.Create("CS8621", "Nullability of reference types in return type doesn't match the target delegate."),
            Tuple.Create("CS8622", "Nullability of reference types in type of parameter doesn't match the target delegate."),
            Tuple.Create("CS8624", "Argument cannot be used as an output for parameter due to differences in the nullability of reference types."),
            Tuple.Create("CS8625", "Cannot convert null literal to non-nullable reference type."),
            Tuple.Create("CS8626", "The ‘as’ operator may produce a null value for a type parameter."),
            Tuple.Create("CS8629", "Nullable value type may be null."),
            Tuple.Create("CS8631", "The type cannot be used as type parameter in the generic type or method.Nullability of type argument doesn’t match constraint type."),
            Tuple.Create("CS8632", "The annotation for nullable reference types should only be used in code within a '#nullable' annotations context."),
            Tuple.Create("CS8633", "Nullability in constraints for type parameter doesn’t match the constraints for type parameter in implicitly implemented interface method’."),
            Tuple.Create("CS8634", "The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint."),
            Tuple.Create("CS8638", "Conditional access may produce a null value for a type parameter."),
            Tuple.Create("CS8643", "Nullability of reference types in explicit interface specifier doesn’t match interface implemented by the type."),
            Tuple.Create("CS8644", "Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn’t match."),
            Tuple.Create("CS8645", "Interface is already listed in the interface list with different nullability of reference types."),
            Tuple.Create("CS8653", "A default expression introduces a null value for a type parameter."),
            Tuple.Create("CS8654", "A null literal introduces a null value for a type parameter."),
            Tuple.Create("CS8655", "The switch expression does not handle some null inputs."),
            Tuple.Create("CS8667", "Partial method declarations have inconsistent nullability in constraints for type parameter."),
            Tuple.Create("CS8670", "Object or collection initializer implicitly dereferences possibly null member."),
            Tuple.Create("CS8714", "The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint."),
            Tuple.Create("CS8762", "Parameter may not have a null value when exiting in some condition."),
        };

        foreach (var (code, title) in list)
        {
            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    string.Empty,
                    category: null,
                    description: null));
        }

        return data;
    }
}