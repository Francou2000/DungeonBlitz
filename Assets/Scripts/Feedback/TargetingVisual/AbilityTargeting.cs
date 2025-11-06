public static class AbilityTargeting
{
    public static bool NeedsAim(UnitAbility a)
        => a != null && (a.groundTarget || a.areaType == AreaType.Circle || a.areaType == AreaType.Line);

    public static bool NeedsUnit(UnitAbility a)
        => a != null && a.areaType == AreaType.Single && !a.selfOnly;

    public static bool IsSelf(UnitAbility a)
        => a != null && a.selfOnly;
}
