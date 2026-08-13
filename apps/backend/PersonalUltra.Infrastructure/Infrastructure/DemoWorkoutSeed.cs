using System.Security.Cryptography;
using System.Text;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

/// <summary>
/// The primary demo Student's four complete workouts. IDs are derived from
/// stable keys so a later seed run can add missing rows without replacing a
/// workout that a demo user has edited.
/// </summary>
internal static class DemoWorkoutSeed
{
    internal static readonly IReadOnlyList<WorkoutSeed> Workouts =
    [
        new("upper-a", "Upper A", "Peito, costas e braços.",
        [
            new("supino-reto-com-barra", 4, 8, 12, 90, "Escápulas retraídas."),
            new("remada-baixa", 4, 8, 12, 90, "Conduzir o cotovelo para trás."),
            new("desenvolvimento-com-halteres", 3, 10, 12, 75, "Evitar compensar com a lombar."),
            new("puxada-dorsal-na-maquina", 3, 10, 12, 75, "Manter o tronco estável."),
            new("triceps-na-polia-com-corda", 3, 10, 15, 60, "Cotovelos próximos ao corpo."),
            new("rosca-direta-com-barra", 3, 10, 12, 60, "Sem impulsionar o corpo."),
        ]),
        new("lower-a", "Lower A", "Quadríceps, posteriores e glúteos.",
        [
            new("agachamento-livre", 4, 8, 10, 120, "Joelhos acompanham a linha dos pés."),
            new("leg-press-45", 4, 10, 12, 90, "Manter a lombar apoiada."),
            new("cadeira-extensora", 3, 12, 15, 60, "Controlar a volta da carga."),
            new("cadeira-flexora", 3, 10, 12, 60, "Manter o quadril apoiado."),
            new("stiff-com-barra", 3, 8, 12, 90, "Levar o quadril para trás."),
            new("elevacao-pelvica-com-barra", 4, 8, 12, 90, "Contrair os glúteos no topo."),
        ]),
        new("upper-b", "Upper B", "Variação de tronco e braços.",
        [
            new("supino-reto-com-barra", 3, 10, 12, 75, "Descer a barra com controle."),
            new("puxada-dorsal-na-maquina", 4, 8, 12, 90, "Puxar em direção ao peito."),
            new("remada-baixa", 3, 10, 12, 75, "Manter a coluna neutra."),
            new("elevacao-lateral-com-halteres", 3, 12, 15, 60, "Elevar até a linha dos ombros."),
            new("triceps-na-polia-com-corda", 3, 12, 15, 60, "Estender sem mover os cotovelos."),
            new("rosca-direta-com-barra", 3, 8, 12, 60, "Punhos alinhados."),
        ]),
        new("lower-b", "Lower B", "Glúteos e cadeia posterior.",
        [
            new("levantamento-terra-romeno", 4, 8, 10, 120, "Barra próxima às pernas."),
            new("passada-com-halteres", 3, 10, 12, 90, "Passo firme e controlado."),
            new("agachamento-sumo", 4, 8, 12, 90, "Base ampla e joelhos alinhados."),
            new("abducao-com-elastico", 3, 15, 20, 45, "Manter a tensão do elástico."),
            new("coice-no-cabo", 3, 12, 15, 60, "Não arquear a lombar."),
            new("ponte-de-gluteos", 4, 12, 15, 60, "Subir a pelve com controle."),
        ]),
    ];

    internal static Guid IdFor(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"personal-ultra/demo/{key}"));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        return new Guid(bytes);
    }

    internal sealed record WorkoutSeed(string Key, string Name, string Notes, IReadOnlyList<ExerciseSeed> Exercises);
    internal sealed record ExerciseSeed(string Slug, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
}
