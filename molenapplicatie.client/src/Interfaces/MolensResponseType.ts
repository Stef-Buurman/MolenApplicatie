import { MolenData } from "./MolenData";

export interface MolensResponseType {
  activeMolensWithImage: number;
  remainderMolensWithImage: number;
  totalMolensWithImage: number;
  totalCountActiveMolens: number;
  totalCountRemainderMolens: number;
  totalCountDisappearedMolens: CountDisappearedMolens[];
  totalCountExistingMolens: number;
  totalCountMolens: number;
  molens: MolenData[];
}

export interface CountDisappearedMolens {
  provincie: string;
  count: number;
}
