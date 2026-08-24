using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalUltra.ExerciseCatalogFactory.Providers.Text;

public static class MetadataProposalValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static MetadataProposal ParseAndValidate(string json)
    {
        MetadataProposal proposal;
        try
        {
            proposal = JsonSerializer.Deserialize<MetadataProposal>(json, JsonOptions)
                ?? throw new InvalidDataException("Resposta estruturada vazia.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Resposta não corresponde ao JSON Schema de metadados.", exception);
        }

        Require(proposal.CanonicalName, 200, "canonicalName");
        Require(proposal.PrimaryMuscleGroup, 100, "primaryMuscleGroup");
        Require(proposal.Equipment, 100, "equipment");
        Require(proposal.Instructions, 4000, "instructions");
        Require(proposal.VisualDescription, 2000, "visualDescription");
        if (proposal.Aliases is null || proposal.Ambiguities is null || proposal.Confidence is null)
            throw new InvalidDataException("Arrays e confidence são obrigatórios.");
        if (proposal.Aliases.Count > 20 || proposal.Aliases.Any(alias => string.IsNullOrWhiteSpace(alias) || alias.Length > 200))
            throw new InvalidDataException("aliases inválidos.");
        if (proposal.Ambiguities.Count > 10 || proposal.Ambiguities.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 500))
            throw new InvalidDataException("ambiguities inválidas.");
        if (!MetadataTaxonomy.MuscleGroups.Contains(proposal.PrimaryMuscleGroup, StringComparer.Ordinal))
            throw new InvalidDataException("primaryMuscleGroup fora da taxonomia permitida.");
        if (!MetadataTaxonomy.Equipment.Contains(proposal.Equipment, StringComparer.Ordinal))
            throw new InvalidDataException("equipment fora da taxonomia permitida.");
        if (proposal.Confidence.PrimaryMuscleGroup is not ("low" or "medium" or "high") ||
            proposal.Confidence.Equipment is not ("low" or "medium" or "high"))
            throw new InvalidDataException("confidence inválida.");
        return proposal;
    }

    private static void Require(string? value, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum)
            throw new InvalidDataException($"{field} ausente ou excede {maximum} caracteres.");
    }
}
