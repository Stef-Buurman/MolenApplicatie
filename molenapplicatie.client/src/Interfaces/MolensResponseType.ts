export interface MolensResponseType<T> {
  activeMolensWithImage: number;
  remainderMolensWithImage: number;
  totalMolensWithImage: number;
  totalCountActiveMolens: number;
  totalCountRemainderMolens: number;
  totalCountDisappearedMolens: CountDisappearedMolens[];
  totalCountExistingMolens: number;
  totalCountMolens: number;
  molens: T[];
}

export interface CountDisappearedMolens {
  provincie: string;
  count: number;
}
