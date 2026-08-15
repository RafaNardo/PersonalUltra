using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Normalization;

public static class LegacyCatalog
{
    public static IReadOnlyList<LegacyExerciseIdentity> Identities { get; } =
    [
        Entry(1, "Supino reto com barra", "supino-reto-com-barra"),
        Entry(2, "Afundo com halteres", "afundo-com-halteres"),
        Entry(3, "Ponte de glúteo unilateral", "ponte-de-gluteo-unilateral"),
        Entry(4, "Remada baixa", "remada-baixa"),
        Entry(5, "Puxada dorsal na máquina", "puxada-dorsal-na-maquina"),
        Entry(6, "Pull through no cabo", "pull-through-no-cabo"),
        Entry(7, "Desenvolvimento com halteres", "desenvolvimento-com-halteres"),
        Entry(8, "Elevação lateral com halteres", "elevacao-lateral-com-halteres"),
        Entry(9, "Tríceps na polia com corda", "triceps-na-polia-com-corda"),
        Entry(10, "Rosca direta com barra", "rosca-direta-com-barra"),
        Entry(11, "Agachamento livre", "agachamento-livre"),
        Entry(12, "Agachamento goblet", "agachamento-goblet"),
        Entry(13, "Agachamento sumô", "agachamento-sumo"),
        Entry(14, "Cadeira extensora", "cadeira-extensora"),
        Entry(15, "Cadeira flexora", "cadeira-flexora"),
        Entry(16, "Leg press 45°", "leg-press-45"),
        Entry(17, "Passada com halteres", "passada-com-halteres"),
        Entry(18, "Step-up com halteres", "step-up-com-halteres"),
        Entry(19, "Stiff com barra", "stiff-com-barra"),
        Entry(20, "Levantamento terra romeno", "levantamento-terra-romeno"),
        Entry(21, "Abdução com elástico", "abducao-com-elastico"),
        Entry(22, "Abdução de quadril na máquina", "abducao-de-quadril-na-maquina"),
        Entry(23, "Coice com caneleira", "coice-com-caneleira"),
        Entry(24, "Coice no cabo", "coice-no-cabo"),
        Entry(25, "Elevação pélvica com barra", "elevacao-pelvica-com-barra"),
        Entry(26, "Elevação pélvica unilateral com barra", "elevacao-pelvica-unilateral-com-barra"),
        Entry(27, "Ponte de glúteos", "ponte-de-gluteos"),
        Entry(28, "Frog pump", "frog-pump")
    ];

    private static LegacyExerciseIdentity Entry(int suffix, string name, string slug) =>
        new(Guid.Parse($"10000000-0000-0000-0000-{suffix:000000000000}"), name, slug);
}
