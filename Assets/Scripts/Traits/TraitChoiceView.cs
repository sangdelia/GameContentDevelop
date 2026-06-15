public readonly struct TraitChoiceView
{
    public readonly TraitData Trait;
    public readonly int CurrentLevel;
    public readonly int NextLevel;

    public TraitChoiceView(TraitData trait, int currentLevel)
    {
        Trait = trait;
        CurrentLevel = currentLevel;
        NextLevel = currentLevel + 1;
    }
}
