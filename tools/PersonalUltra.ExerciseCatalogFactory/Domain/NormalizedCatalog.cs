namespace PersonalUltra.ExerciseCatalogFactory.Domain;

public sealed record NormalizedCatalog(
    int SchemaVersion,
    string PipelineVersion,
    string TaxonomyVersion,
    string TargetProfileVersion,
    string SourceSha256,
    IReadOnlyList<NormalizedExercise> Items,
    IReadOnlyList<LegacyExerciseIdentity> PreservedLegacyIdentities,
    IReadOnlyList<LegacyExerciseIdentity> MatchedLegacyIdentities,
    IReadOnlyList<LegacyExerciseIdentity> UnresolvedLegacyIdentities,
    IReadOnlyList<IntakeIssue> Issues,
    TaxonomyImpact TaxonomyImpact);

public sealed record NormalizedExercise(
    string ExternalKey,
    string CanonicalName,
    IReadOnlyList<string> Aliases,
    string Slug,
    string AssetName,
    Guid TargetId,
    bool PreservesLegacyIdentity,
    string? PrimaryMuscleGroup,
    string? Equipment,
    int SourceRow,
    string SourceHash,
    string State);

public sealed record LegacyExerciseIdentity(Guid Id, string Name, string Slug);

public sealed record IntakeIssue(
    string Code,
    string Severity,
    IReadOnlyList<string> ExternalKeys,
    string Message);

public sealed record TaxonomyImpact(
    IReadOnlyList<string> GroupsInInput,
    IReadOnlyList<string> EquipmentInInput,
    IReadOnlyList<string> GroupsOutsideCurrentMobileTaxonomy,
    IReadOnlyList<string> EquipmentOutsideProposedTaxonomy,
    int ItemsWithoutGroup,
    int ItemsWithoutEquipment);

public sealed record CatalogInputRow(CatalogInputItem Item, int Row);
