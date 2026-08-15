using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Normalization;

public sealed partial class CatalogNormalizer
{
    public static readonly Guid PersonalUltraExerciseNamespace = Guid.Parse("9a724d9b-22d2-5aa4-8ea4-04aa535f4d81");
    public const string PipelineVersion = "normalization-v1";
    public const string TaxonomyVersion = "personal-ultra-catalog-v1";
    public const string TargetProfileVersion = "personal-ultra-legacy-28-v1";

    private static readonly IReadOnlySet<string> ProposedGroups = new HashSet<string>(
        ["Quadríceps", "Posteriores da coxa", "Glúteos", "Panturrilhas", "Peito", "Costas", "Ombros", "Bíceps", "Tríceps", "Core", "Corpo inteiro", "Cardio"],
        StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> CurrentMobileGroups = new HashSet<string>(
        ["Peito", "Costas", "Ombros", "Braços", "Pernas", "Glúteos"], StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> ProposedEquipment = new HashSet<string>(
        ["Barra", "Halteres", "Cabo", "Máquina", "Peso corporal", "Elástico", "Caneleira", "Kettlebell", "Trap bar", "Landmine", "Suspensão", "Bola suíça", "Sliders", "Rolo abdominal", "Trenó", "Corda naval", "Medicine ball", "Cardio"],
        StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, string> GroupAliases = AliasMap(
        ("quadriceps", "Quadríceps"), ("posteriores da coxa", "Posteriores da coxa"),
        ("gluteos", "Glúteos"), ("panturrilhas", "Panturrilhas"), ("peito", "Peito"),
        ("costas", "Costas"), ("ombros", "Ombros"), ("biceps", "Bíceps"),
        ("triceps", "Tríceps"), ("core", "Core"), ("corpo inteiro", "Corpo inteiro"), ("cardio", "Cardio"));

    private static readonly IReadOnlyDictionary<string, string> EquipmentAliases = AliasMap(
        ("barra", "Barra"), ("halter", "Halteres"), ("halteres", "Halteres"),
        ("cabo", "Cabo"), ("polia", "Cabo"), ("maquina", "Máquina"),
        ("peso corporal", "Peso corporal"), ("elastico", "Elástico"),
        ("caneleira", "Caneleira"), ("kettlebell", "Kettlebell"), ("trap bar", "Trap bar"),
        ("landmine", "Landmine"), ("suspensao", "Suspensão"), ("bola suica", "Bola suíça"),
        ("sliders", "Sliders"), ("rolo abdominal", "Rolo abdominal"), ("treno", "Trenó"),
        ("corda naval", "Corda naval"), ("medicine ball", "Medicine ball"), ("cardio", "Cardio"));

    private static readonly (string LegacySlug, string CandidateSlug, string Reason)[] KnownAmbiguities =
    [
        ("stiff-com-barra", "levantamento-terra-romeno-com-barra", "Provável alias/merge; exige decisão humana."),
        ("afundo-com-halteres", "afundo-estacionario-com-halteres", "Confirmar que o legado representa a variação estacionária."),
        ("passada-com-halteres", "passada-a-frente-com-halteres", "Confirmar que o legado representa passada à frente/dinâmica."),
        ("remada-baixa", "remada-baixa-no-cabo-com-triangulo", "Implemento e pegada do legado não estão confirmados."),
        ("puxada-dorsal-na-maquina", "puxada-dorsal-na-maquina-com-pegada-neutra", "Pegada do legado não está confirmada."),
        ("desenvolvimento-com-halteres", "desenvolvimento-sentado-com-halteres", "Posição sentada/em pé do legado não está confirmada."),
        ("desenvolvimento-com-halteres", "desenvolvimento-em-pe-com-halteres", "Posição sentada/em pé do legado não está confirmada."),
        ("rosca-direta-com-barra", "rosca-direta-com-barra-reta", "Tipo de barra do legado não está confirmado."),
        ("rosca-direta-com-barra", "rosca-direta-com-barra-ez", "Tipo de barra do legado não está confirmado."),
        ("abducao-com-elastico", "caminhada-lateral-com-mini-band", "Posição e implemento do legado não estão confirmados."),
        ("agachamento-livre", "agachamento-livre-com-barra", "O nome legado omite a barra; não vincular identidade sem confirmação."),
        ("agachamento-sumo", "agachamento-com-barra-em-base-ampla-sumo", "Variação legada deve ser preservada até decisão explícita."),
        ("levantamento-terra-romeno", "levantamento-terra-romeno-com-barra", "Nome legado omite o implemento; não mesclar automaticamente.")
    ];

    public NormalizedCatalog Normalize(IReadOnlyList<CatalogInputRow> rows, string sourceFile, string sourceSha256)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var issues = new List<IntakeIssue>();
        var legacyBySlug = LegacyCatalog.Identities.ToDictionary(identity => identity.Slug, StringComparer.Ordinal);
        var provisional = rows.Select(row => NormalizeRow(row, sourceFile, legacyBySlug)).ToArray();

        foreach (var group in provisional.GroupBy(item => item.ExternalKey, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            var replacements = group.ToDictionary(item => item.SourceRow,
                item => $"{item.ExternalKey}:candidate-{item.SourceHash[..8]}");
            provisional = provisional.Select(item => replacements.TryGetValue(item.SourceRow, out var replacement)
                ? item with { ExternalKey = replacement }
                : item).ToArray();
            issues.Add(Issue("external-key-collision", replacements.Values, $"externalKey derivada colidiu: {group.Key}. Candidatos receberam chaves determinísticas temporárias e exigem chave humana estável."));
        }
        foreach (var group in provisional.GroupBy(item => item.Slug, StringComparer.Ordinal).Where(group => group.Count() > 1))
            issues.Add(Issue("slug-collision", group.Select(x => x.ExternalKey), $"Slug colidiu sem sufixo automático: {group.Key}."));
        foreach (var group in provisional.GroupBy(item => Fold(item.CanonicalName), StringComparer.Ordinal).Where(group => group.Count() > 1))
            issues.Add(Issue("exact-duplicate", group.Select(x => x.ExternalKey), $"Nomes equivalentes após normalização: {group.First().CanonicalName}."));

        var bySlug = provisional.GroupBy(x => x.Slug, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
        foreach (var ambiguity in KnownAmbiguities)
        {
            if (!bySlug.TryGetValue(ambiguity.CandidateSlug, out var candidates)) continue;
            issues.Add(Issue("known-legacy-ambiguity", candidates.Select(x => x.ExternalKey),
                $"{ambiguity.LegacySlug} ↔ {ambiguity.CandidateSlug}: {ambiguity.Reason}"));
        }

        foreach (var item in provisional)
        {
            if (item.PrimaryMuscleGroup is not null && !ProposedGroups.Contains(item.PrimaryMuscleGroup))
                issues.Add(Issue("unknown-muscle-group", [item.ExternalKey], $"Grupo fora da taxonomia proposta: {item.PrimaryMuscleGroup}."));
            if (item.Equipment is not null && !ProposedEquipment.Contains(item.Equipment))
                issues.Add(Issue("unknown-equipment", [item.ExternalKey], $"Equipamento fora da taxonomia proposta: {item.Equipment}."));
        }

        var blockedKeys = issues.SelectMany(issue => issue.ExternalKeys).ToHashSet(StringComparer.Ordinal);
        var items = provisional.Select(item => item with { State = blockedKeys.Contains(item.ExternalKey) ? "needs_review" : "normalized" })
            .OrderBy(item => item.ExternalKey, StringComparer.Ordinal).ToArray();
        var groups = items.Select(item => item.PrimaryMuscleGroup).Where(value => value is not null).Cast<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var equipment = items.Select(item => item.Equipment).Where(value => value is not null).Cast<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var impact = new TaxonomyImpact(groups, equipment,
            groups.Where(group => !CurrentMobileGroups.Contains(group)).ToArray(),
            equipment.Where(value => !ProposedEquipment.Contains(value)).ToArray(),
            items.Count(item => item.PrimaryMuscleGroup is null), items.Count(item => item.Equipment is null));

        var matchedLegacySlugs = items.Where(item => item.PreservesLegacyIdentity).Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var matchedLegacy = LegacyCatalog.Identities.Where(identity => matchedLegacySlugs.Contains(identity.Slug)).ToArray();
        var unresolvedLegacy = LegacyCatalog.Identities.Where(identity => !matchedLegacySlugs.Contains(identity.Slug)).ToArray();

        return new NormalizedCatalog(1, PipelineVersion, TaxonomyVersion, TargetProfileVersion, sourceSha256,
            items, LegacyCatalog.Identities, matchedLegacy, unresolvedLegacy,
            issues.OrderBy(issue => issue.Code).ThenBy(issue => string.Join('|', issue.ExternalKeys)).ToArray(), impact);
    }

    public static string NormalizeName(string value) => Whitespace().Replace(value.Normalize(NormalizationForm.FormC).Trim(), " ");

    public static string Slugify(string value)
    {
        var decomposed = NormalizeName(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        var slug = Separators().Replace(builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(slug)) throw new InvalidDataException($"Nome não produz slug válido: {value}");
        return slug;
    }

    public static string? NormalizeGroup(string? value) => NormalizeTaxonomy(value, GroupAliases);
    public static string? NormalizeEquipment(string? value) => NormalizeTaxonomy(value, EquipmentAliases);

    private static NormalizedExercise NormalizeRow(CatalogInputRow row, string sourceFile,
        IReadOnlyDictionary<string, LegacyExerciseIdentity> legacyBySlug)
    {
        var name = NormalizeName(row.Item.Name);
        var slug = Slugify(name);
        var externalKey = string.IsNullOrWhiteSpace(row.Item.ExternalKey) ? $"exercise:{slug}" : Slugify(row.Item.ExternalKey);
        var identity = legacyBySlug.GetValueOrDefault(slug);
        var targetId = identity?.Id ?? UuidV5.Create(PersonalUltraExerciseNamespace, slug);
        var aliases = (row.Item.Aliases ?? []).Select(NormalizeName).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var sourceHash = Sha256(string.Join("\n", sourceFile, row.Row, externalKey, name,
            string.Join('|', aliases), row.Item.PrimaryMuscleGroup, row.Item.Equipment,
            row.Item.InstructionsHint, row.Item.VisualHint, string.Join('|', row.Item.LockedFields ?? [])));
        return new NormalizedExercise(externalKey, name, aliases, slug, slug, targetId, identity is not null,
            NormalizeGroup(row.Item.PrimaryMuscleGroup), NormalizeEquipment(row.Item.Equipment), row.Row, sourceHash, "normalized");
    }

    private static string? NormalizeTaxonomy(string? value, IReadOnlyDictionary<string, string> aliases)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = NormalizeName(value);
        return aliases.GetValueOrDefault(Fold(normalized), normalized);
    }

    private static IReadOnlyDictionary<string, string> AliasMap(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(entry => Fold(entry.Key), entry => entry.Value, StringComparer.Ordinal);

    private static string Fold(string value)
    {
        var decomposed = NormalizeName(value).Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static IntakeIssue Issue(string code, IEnumerable<string> keys, string message) =>
        new(code, "blocking", keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), message);

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex Separators();
}
