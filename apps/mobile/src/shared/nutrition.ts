export function formatNutritionQuantity(quantity: number, unit: string) {
  if (unit === 'livre') return 'livre';
  const formatted = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(quantity);
  if (quantity === 1 || unit === 'g' || unit === 'ml') return `${formatted} ${unit}`;
  const plurals: Record<string, string> = { unidade: 'unidades', fatia: 'fatias', colher: 'colheres', dose: 'doses', porção: 'porções' };
  return `${formatted} ${plurals[unit] ?? unit}`;
}
