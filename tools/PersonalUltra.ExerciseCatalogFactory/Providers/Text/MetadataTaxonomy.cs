namespace PersonalUltra.ExerciseCatalogFactory.Providers.Text;

public static class MetadataTaxonomy
{
    public static readonly IReadOnlyList<string> MuscleGroups =
    [
        "Quadríceps", "Posteriores da coxa", "Glúteos", "Panturrilhas", "Peito", "Costas",
        "Ombros", "Bíceps", "Tríceps", "Core", "Corpo inteiro", "Cardio"
    ];

    public static readonly IReadOnlyList<string> Equipment =
    [
        "Barra", "Halteres", "Cabo", "Máquina", "Peso corporal", "Elástico", "Caneleira",
        "Kettlebell", "Trap bar", "Landmine", "Suspensão", "Bola suíça", "Sliders",
        "Rolo abdominal", "Trenó", "Corda naval", "Medicine ball", "Cardio"
    ];
}
