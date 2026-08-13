import type { ImageSourcePropType } from 'react-native';

// Metro resolves local images at build time, so every seeded ImageRef must use a
// static require instead of a dynamically constructed path.
const exerciseMediaByRef: Record<string, ImageSourcePropType> = {
  'assets/training/abducao_com_elastico.png': require('../../../assets/training/abducao_com_elastico.png'),
  'assets/training/abducao_de_quadril_na_maquina.png': require('../../../assets/training/abducao_de_quadril_na_maquina.png'),
  'assets/training/afundo_com_halteres.png': require('../../../assets/training/afundo_com_halteres.png'),
  'assets/training/agachamento_goblet.png': require('../../../assets/training/agachamento_goblet.png'),
  'assets/training/agachamento_livre.png': require('../../../assets/training/agachamento_livre.png'),
  'assets/training/agachamento_sumo.png': require('../../../assets/training/agachamento_sumo.png'),
  'assets/training/cadeira_extensora.png': require('../../../assets/training/cadeira_extensora.png'),
  'assets/training/cadeira_flexora.png': require('../../../assets/training/cadeira_flexora.png'),
  'assets/training/coice_com_caneleira.png': require('../../../assets/training/coice_com_caneleira.png'),
  'assets/training/coice_no_cabo.png': require('../../../assets/training/coice_no_cabo.png'),
  'assets/training/desenvolvimento-com-halteres.png': require('../../../assets/training/desenvolvimento-com-halteres.png'),
  'assets/training/elevacao-lateral-com-halteres.png': require('../../../assets/training/elevacao-lateral-com-halteres.png'),
  'assets/training/elevacao_pelvica_com_barra.png': require('../../../assets/training/elevacao_pelvica_com_barra.png'),
  'assets/training/elevacao_pelvica_unilateral_com_barra.png': require('../../../assets/training/elevacao_pelvica_unilateral_com_barra.png'),
  'assets/training/frog_pump.png': require('../../../assets/training/frog_pump.png'),
  'assets/training/leg_press_45.png': require('../../../assets/training/leg_press_45.png'),
  'assets/training/levantamento-terra-romeno.png': require('../../../assets/training/levantamento-terra-romeno.png'),
  'assets/training/passada_com_halteres.png': require('../../../assets/training/passada_com_halteres.png'),
  'assets/training/ponte_de_gluteo_unilateral.png': require('../../../assets/training/ponte_de_gluteo_unilateral.png'),
  'assets/training/ponte_de_gluteos.png': require('../../../assets/training/ponte_de_gluteos.png'),
  'assets/training/pull_through_no_cabo.png': require('../../../assets/training/pull_through_no_cabo.png'),
  'assets/training/puxada-dorsal-na-maquina.png': require('../../../assets/training/puxada-dorsal-na-maquina.png'),
  'assets/training/remada-baixa.png': require('../../../assets/training/remada-baixa.png'),
  'assets/training/rosca-direta-com-barra.png': require('../../../assets/training/rosca-direta-com-barra.png'),
  'assets/training/step_up_com_halteres.png': require('../../../assets/training/step_up_com_halteres.png'),
  'assets/training/stiff_com_barra.png': require('../../../assets/training/stiff_com_barra.png'),
  'assets/training/supino-reto-com-barra.png': require('../../../assets/training/supino-reto-com-barra.png'),
  'assets/training/triceps-na-polia-com-corda.png': require('../../../assets/training/triceps-na-polia-com-corda.png'),
};

export const SEEDED_EXERCISE_MEDIA_REFS = Object.freeze(Object.keys(exerciseMediaByRef));

export function exerciseMediaSource(imageRef?: string): ImageSourcePropType | undefined {
  if (!imageRef) return undefined;
  return exerciseMediaByRef[imageRef.trim().replace(/^\/+/, '')];
}
