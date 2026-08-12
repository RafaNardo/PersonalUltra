import type { ImageSourcePropType } from 'react-native';

const images: Record<string, ImageSourcePropType> = {
  'Supino reto com barra': require('../../assets/training/supino-reto-com-barra.png'),
  'Remada baixa': require('../../assets/training/remada-baixa.png'),
  'Puxada dorsal na máquina': require('../../assets/training/puxada-dorsal-na-maquina.png'),
  'Desenvolvimento com halteres': require('../../assets/training/desenvolvimento-com-halteres.png'),
  'Elevação lateral com halteres': require('../../assets/training/elevacao-lateral-com-halteres.png'),
  'Tríceps na polia com corda': require('../../assets/training/triceps-na-polia-com-corda.png'),
  'Rosca direta com barra': require('../../assets/training/rosca-direta-com-barra.png'),
  'Agachamento livre': require('../../assets/training/agachamento_livre.png'),
  'Levantamento terra romeno': require('../../assets/training/levantamento-terra-romeno.png'),
  'Abdução com elástico': require('../../assets/training/abducao_com_elastico.png'),
  'Abdução de quadril na máquina': require('../../assets/training/abducao_de_quadril_na_maquina.png'),
  'Afundo com halteres': require('../../assets/training/afundo_com_halteres.png'),
  'Agachamento goblet': require('../../assets/training/agachamento_goblet.png'),
  'Agachamento sumô': require('../../assets/training/agachamento_sumo.png'),
  'Cadeira extensora': require('../../assets/training/cadeira_extensora.png'),
  'Cadeira flexora': require('../../assets/training/cadeira_flexora.png'),
  'Coice com caneleira': require('../../assets/training/coice_com_caneleira.png'),
  'Coice no cabo': require('../../assets/training/coice_no_cabo.png'),
  'Elevação pélvica com barra': require('../../assets/training/elevacao_pelvica_com_barra.png'),
  'Elevação pélvica unilateral com barra': require('../../assets/training/elevacao_pelvica_unilateral_com_barra.png'),
  'Frog pump': require('../../assets/training/frog_pump.png'),
  'Leg press 45°': require('../../assets/training/leg_press_45.png'),
  'Passada com halteres': require('../../assets/training/passada_com_halteres.png'),
  'Ponte de glúteos': require('../../assets/training/ponte_de_gluteos.png'),
  'Ponte de glúteo unilateral': require('../../assets/training/ponte_de_gluteo_unilateral.png'),
  'Pull through no cabo': require('../../assets/training/pull_through_no_cabo.png'),
  'Step-up com halteres': require('../../assets/training/step_up_com_halteres.png'),
  'Stiff com barra': require('../../assets/training/stiff_com_barra.png'),
};

export function exerciseImage(name: string) {
  return images[name];
}
