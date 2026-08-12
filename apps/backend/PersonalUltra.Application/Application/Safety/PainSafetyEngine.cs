namespace PersonalUltra.Application.Safety;

/// <summary>
/// Conservative, deterministic classification for a reported pain. It does not
/// diagnose, prescribe treatment, or change a workout; it only explains whether
/// an automatic action is permitted by the v0 safety policy.
/// </summary>
public sealed class PainSafetyEngine
{
    public PainSafetyDecision Evaluate(int intensity, string? context)
    {
        if (intensity is < 0 or > 10)
            return Red("PAIN_INTENSITY_INVALID", "A intensidade informada é inválida. Não automatize alterações; procure orientação profissional se necessário.");
        if (string.IsNullOrWhiteSpace(context))
            return Red("PAIN_CONTEXT_INCOMPLETE", "Faltam informações sobre a dor. Não automatize alterações até uma revisão adequada.");
        if (intensity >= 7)
            return Red("PAIN_HIGH_INTENSITY", "Dor intensa registrada. Não automatize alterações e procure orientação profissional.");
        if (intensity >= 4)
            return new PainSafetyDecision("Yellow", "PAIN_MODERATE_INTENSITY", "Dor moderada registrada. Uma revisão é necessária antes de qualquer alteração.", true);

        return new PainSafetyDecision("Green", "PAIN_LOW_INTENSITY", "Registro realizado. Observe a resposta nas próximas séries.", false);
    }

    private static PainSafetyDecision Red(string reasonCode, string message) => new("Red", reasonCode, message, true);
}

public sealed record PainSafetyDecision(string SafetyLevel, string ReasonCode, string Message, bool RequiresConfirmation);
