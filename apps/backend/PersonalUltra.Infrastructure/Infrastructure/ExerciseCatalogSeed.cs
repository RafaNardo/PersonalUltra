using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

/// <summary>
/// The V1 exercise catalog is system-owned. Keep this list deterministic and
/// keyed by slug so it can be safely applied every time the demo starts.
/// Images use immutable bucket keys. The original 28 identities remain stable,
/// but their donor illustrations were retired from the mobile bundle in v3.
/// </summary>
internal static class ExerciseCatalogSeed
{
    private static readonly IReadOnlyList<ExerciseSeed> LegacyExercises =
    [
        new("10000000-0000-0000-0000-000000000001", "Supino reto com barra", "supino-reto-com-barra", "Peito", "Barra", "supino-reto-com-barra", "Mantenha as escápulas retraídas e os pés apoiados no chão."),
        new("10000000-0000-0000-0000-000000000002", "Afundo com halteres", "afundo-com-halteres", "Quadríceps", "Halteres", "afundo_com_halteres", "Dê um passo firme e mantenha o joelho acompanhando o pé."),
        new("10000000-0000-0000-0000-000000000003", "Ponte de glúteo unilateral", "ponte-de-gluteo-unilateral", "Glúteos", "Peso corporal", "ponte_de_gluteo_unilateral", "Mantenha a pelve nivelada e suba com controle."),
        new("10000000-0000-0000-0000-000000000004", "Remada baixa", "remada-baixa", "Costas", "Cabo", "remada-baixa", "Inicie o movimento aproximando as escápulas e evite balançar o tronco."),
        new("10000000-0000-0000-0000-000000000005", "Puxada dorsal na máquina", "puxada-dorsal-na-maquina", "Costas", "Máquina", "puxada-dorsal-na-maquina", "Puxe em direção ao peito mantendo o tronco estável."),
        new("10000000-0000-0000-0000-000000000006", "Pull through no cabo", "pull-through-no-cabo", "Glúteos", "Cabo", "pull_through_no_cabo", "Leve o quadril para trás e finalize a extensão contraindo os glúteos."),
        new("10000000-0000-0000-0000-000000000007", "Desenvolvimento com halteres", "desenvolvimento-com-halteres", "Ombros", "Halteres", "desenvolvimento-com-halteres", "Pressione os halteres sem compensar com a lombar."),
        new("10000000-0000-0000-0000-000000000008", "Elevação lateral com halteres", "elevacao-lateral-com-halteres", "Ombros", "Halteres", "elevacao-lateral-com-halteres", "Eleve os braços até a linha dos ombros com controle."),
        new("10000000-0000-0000-0000-000000000009", "Tríceps na polia com corda", "triceps-na-polia-com-corda", "Tríceps", "Cabo", "triceps-na-polia-com-corda", "Mantenha os cotovelos próximos ao corpo durante toda a extensão."),
        new("10000000-0000-0000-0000-000000000010", "Rosca direta com barra", "rosca-direta-com-barra", "Bíceps", "Barra", "rosca-direta-com-barra", "Evite impulsionar o corpo e mantenha os cotovelos estáveis."),
        new("10000000-0000-0000-0000-000000000011", "Agachamento livre", "agachamento-livre", "Quadríceps", "Barra", "agachamento_livre", "Desça com controle mantendo os joelhos alinhados aos pés."),
        new("10000000-0000-0000-0000-000000000012", "Agachamento goblet", "agachamento-goblet", "Quadríceps", "Halter", "agachamento_goblet", "Segure o halter junto ao peito e mantenha o tronco firme."),
        new("10000000-0000-0000-0000-000000000013", "Agachamento sumô", "agachamento-sumo", "Quadríceps", "Barra", "agachamento_sumo", "Use uma base ampla e acompanhe a linha dos pés com os joelhos."),
        new("10000000-0000-0000-0000-000000000014", "Cadeira extensora", "cadeira-extensora", "Quadríceps", "Máquina", "cadeira_extensora", "Faça a extensão sem chutar a carga e retorne lentamente."),
        new("10000000-0000-0000-0000-000000000015", "Cadeira flexora", "cadeira-flexora", "Posteriores da coxa", "Máquina", "cadeira_flexora", "Mantenha o quadril apoiado e controle o retorno da flexão."),
        new("10000000-0000-0000-0000-000000000016", "Leg press 45°", "leg-press-45", "Quadríceps", "Máquina", "leg_press_45", "Não trave os joelhos e mantenha a lombar apoiada no encosto."),
        new("10000000-0000-0000-0000-000000000017", "Passada com halteres", "passada-com-halteres", "Quadríceps", "Halteres", "passada_com_halteres", "Dê um passo firme e mantenha o joelho acompanhando o pé."),
        new("10000000-0000-0000-0000-000000000018", "Step-up com halteres", "step-up-com-halteres", "Quadríceps", "Halteres", "step_up_com_halteres", "Suba usando a perna apoiada no banco, sem impulsionar com a de trás."),
        new("10000000-0000-0000-0000-000000000019", "Stiff com barra", "stiff-com-barra", "Posteriores da coxa", "Barra", "stiff_com_barra", "Leve o quadril para trás mantendo a coluna neutra."),
        new("10000000-0000-0000-0000-000000000020", "Levantamento terra romeno", "levantamento-terra-romeno", "Posteriores da coxa", "Barra", "levantamento-terra-romeno", "Mantenha a barra próxima às pernas durante a descida."),
        new("10000000-0000-0000-0000-000000000021", "Abdução com elástico", "abducao-com-elastico", "Glúteos", "Elástico", "abducao_com_elastico", "Faça a abertura sem girar o quadril e mantenha a tensão do elástico."),
        new("10000000-0000-0000-0000-000000000022", "Abdução de quadril na máquina", "abducao-de-quadril-na-maquina", "Glúteos", "Máquina", "abducao_de_quadril_na_maquina", "Abra os joelhos com controle e retorne sem soltar a carga."),
        new("10000000-0000-0000-0000-000000000023", "Coice com caneleira", "coice-com-caneleira", "Glúteos", "Caneleira", "coice_com_caneleira", "Mantenha o abdômen ativo e evite girar o quadril."),
        new("10000000-0000-0000-0000-000000000024", "Coice no cabo", "coice-no-cabo", "Glúteos", "Cabo", "coice_no_cabo", "Estenda o quadril sem arquear a lombar."),
        new("10000000-0000-0000-0000-000000000025", "Elevação pélvica com barra", "elevacao-pelvica-com-barra", "Glúteos", "Barra", "elevacao_pelvica_com_barra", "Finalize a extensão contraindo os glúteos sem hiperestender a lombar."),
        new("10000000-0000-0000-0000-000000000026", "Elevação pélvica unilateral com barra", "elevacao-pelvica-unilateral-com-barra", "Glúteos", "Barra", "elevacao_pelvica_unilateral_com_barra", "Mantenha a pelve nivelada durante toda a execução."),
        new("10000000-0000-0000-0000-000000000027", "Ponte de glúteos", "ponte-de-gluteos", "Glúteos", "Peso corporal", "ponte_de_gluteos", "Suba a pelve com controle e mantenha os joelhos alinhados."),
        new("10000000-0000-0000-0000-000000000028", "Frog pump", "frog-pump", "Glúteos", "Peso corporal", "frog_pump", "Mantenha as solas dos pés unidas e faça contrações controladas."),
    ];

    internal static readonly IReadOnlyList<ExerciseSeed> Exercises =
        [.. LegacyExercises, .. ExerciseCatalogSeedGenerated.Exercises];

    internal sealed record ExerciseSeed(
        string Id,
        string Name,
        string Slug,
        string PrimaryMuscleGroup,
        string? Equipment,
        string ImageRef,
        string? Instructions)
    {
        internal Exercise ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            Name = Name,
            Slug = Slug,
            PrimaryMuscleGroup = PrimaryMuscleGroup,
            Equipment = Equipment,
            ImageRef = $"media://exercise-catalog/delivery/v1/{Slug}.webp",
            Instructions = Instructions,
            IsActive = true,
        };
    }
}
